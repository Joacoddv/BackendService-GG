using GastroGestion.Application.Abstractions;
using GastroGestion.Domain.Bitacora;

namespace GastroGestion.Api.Filters;

/// <summary>
/// Metadata record attached to endpoints that participate in audit logging.
/// Carries the semantic action label written to the Bitacora table.
/// </summary>
public sealed record BitacoraActionMetadata(string Accion);

/// <summary>
/// Endpoint filter that writes a <see cref="BitacoraEntry"/> after the handler executes.
/// Reads the action label from <see cref="BitacoraActionMetadata"/> endpoint metadata.
/// Swallows all exceptions — audit logging must never break the primary response.
/// </summary>
public sealed class BitacoraFilter : IEndpointFilter
{
    private readonly ILogger<BitacoraFilter> _logger;

    public BitacoraFilter(ILogger<BitacoraFilter> logger) => _logger = logger;

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        object? result = null;
        // Default to 500: if the handler throws before producing a result the request
        // failed server-side. The central exception handler may later remap a thrown
        // DomainException/etc. to a 4xx, but the audited outcome here records that the
        // action did not complete successfully.
        var statusCode = StatusCodes.Status500InternalServerError;

        try
        {
            result = await next(context);
            statusCode = result is IStatusCodeHttpResult sc
                ? sc.StatusCode ?? StatusCodes.Status200OK
                : StatusCodes.Status200OK;
        }
        finally
        {
            // Audit runs even when the handler threw, so failed mutating actions are still
            // recorded. The audit write itself must never break the request — swallow its errors.
            try
            {
                var services = context.HttpContext.RequestServices;
                var metadata = context.HttpContext.GetEndpoint()
                    ?.Metadata.GetMetadata<BitacoraActionMetadata>();

                var accion = metadata?.Accion ?? "Unknown";

                var currentUser = services.GetRequiredService<ICurrentUser>();
                var writer      = services.GetRequiredService<IBitacoraWriter>();

                var httpRequest = context.HttpContext.Request;
                var detalle     = $"{httpRequest.Method} {httpRequest.Path}";

                var nowUtc = DateTime.UtcNow;

                var entry = currentUser.IsAuthenticated
                    ? BitacoraEntry.Registrar(
                        currentUser.UsuarioId,
                        currentUser.Email,
                        currentUser.Rol,
                        accion,
                        detalle: detalle,
                        resultadoHttp: statusCode,
                        fechaUtc: nowUtc)
                    : BitacoraEntry.RegistrarAnonimo(
                        email:         currentUser.Email,
                        accion:        accion,
                        detalle:       detalle,
                        resultadoHttp: statusCode,
                        fechaUtc:      nowUtc);

                await writer.RegistrarAsync(entry);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "BitacoraFilter: unexpected error while writing audit entry.");
            }
        }

        // If next(context) threw, the exception propagates after the finally block runs.
        return result;
    }
}
