namespace GastroGestion.Api.Filters;

/// <summary>
/// Extension methods for wiring the <see cref="BitacoraFilter"/> to Minimal API endpoints.
/// </summary>
public static class BitacoraExtensions
{
    /// <summary>
    /// Attaches <see cref="BitacoraActionMetadata"/> to the endpoint and adds
    /// <see cref="BitacoraFilter"/> so every response writes an audit log entry.
    /// </summary>
    /// <param name="builder">The route builder returned by MapGet/MapPost/etc.</param>
    /// <param name="accion">Short English label for the action (e.g. "Create client").</param>
    public static RouteHandlerBuilder WithBitacora(this RouteHandlerBuilder builder, string accion)
        => builder
            .WithMetadata(new BitacoraActionMetadata(accion))
            .AddEndpointFilter<BitacoraFilter>();
}
