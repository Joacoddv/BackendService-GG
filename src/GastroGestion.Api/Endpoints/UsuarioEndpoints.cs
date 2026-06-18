using GastroGestion.Application.Usuarios.GetCocineros;
using GastroGestion.Contracts.Usuarios;
using GastroGestion.Domain.Enums;
using System.Security.Claims;

namespace GastroGestion.Api.Endpoints;

/// <summary>
/// Endpoint group for /usuarios (CCC-A01).
/// </summary>
public static class UsuarioEndpoints
{
    public static WebApplication MapUsuarioEndpoints(this WebApplication app)
    {
        var group = app
            .MapGroup("/usuarios")
            .WithTags("Usuarios")
            .RequireAuthorization();

        // GET /usuarios/cocineros — Cocinero | Administrador only (CCC-A01)
        group.MapGet("/cocineros", async (
            HttpContext http,
            GetCocinerosHandler handler,
            CancellationToken ct) =>
        {
            var rolClaim = http.User.FindFirst(ClaimTypes.Role)?.Value;
            if (rolClaim is null || !Enum.TryParse<RolUsuario>(rolClaim, out var rol))
                return Results.Problem(
                    title: "Invalid or missing role claim.",
                    statusCode: StatusCodes.Status403Forbidden);

            if (rol is not (RolUsuario.Cocinero or RolUsuario.Administrador))
                return Results.Problem(
                    title: "Access denied. Required role: Cocinero or Administrador.",
                    statusCode: StatusCodes.Status403Forbidden);

            var cocineros = await handler.Handle(new GetCocinerosQuery(), ct);
            return Results.Ok(cocineros.Select(u => u.ToCocineroResponse()).ToList());
        });

        return app;
    }
}
