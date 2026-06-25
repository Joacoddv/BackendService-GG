using GastroGestion.Application.Bitacora.GetBitacora;
using GastroGestion.Contracts.Bitacora;

namespace GastroGestion.Api.Endpoints;

/// <summary>
/// Read-only audit log endpoint. Accessible only to Administrador.
/// </summary>
public static class BitacoraEndpoints
{
    public static WebApplication MapBitacoraEndpoints(this WebApplication app)
    {
        var group = app
            .MapGroup("/bitacora")
            .WithTags("Bitacora")
            .RequireAuthorization("SoloAdministrador");

        // GET /bitacora?desde=&hasta=&usuarioId=&page=&pageSize=
        group.MapGet("/", async (
            [Microsoft.AspNetCore.Mvc.FromQuery] DateTime? desde,
            [Microsoft.AspNetCore.Mvc.FromQuery] DateTime? hasta,
            [Microsoft.AspNetCore.Mvc.FromQuery] Guid?     usuarioId,
            [Microsoft.AspNetCore.Mvc.FromQuery] int       page     = 1,
            [Microsoft.AspNetCore.Mvc.FromQuery] int       pageSize = 50,
            GetBitacoraHandler handler = default!,
            CancellationToken ct = default) =>
        {
            var result = await handler.Handle(
                new GetBitacoraQuery(desde, hasta, usuarioId, page, pageSize), ct);
            return Results.Ok(result.ToPageResponse());
        });

        return app;
    }
}
