using GastroGestion.Api.Filters;
using GastroGestion.Application.Abstractions;
using GastroGestion.Application.Auth.CerrarSesion;
using GastroGestion.Application.Auth.CerrarSesionGlobal;
using GastroGestion.Application.Auth.Login;
using GastroGestion.Application.Auth.RefrescarToken;
using GastroGestion.Contracts.Auth;
using GastroGestion.Domain.Bitacora;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GastroGestion.Api.Endpoints;

/// <summary>
/// Auth endpoints. The group has NO .RequireAuthorization() — login must be reachable
/// without a token (AUTH-05.1, AUTH-06.3). [AllowAnonymous] on the POST is explicit for clarity.
/// </summary>
public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/login", [AllowAnonymous] async (
            [FromBody] LoginRequest request,
            LoginHandler handler,
            IBitacoraWriter bitacora,
            CancellationToken ct) =>
        {
            try
            {
                var result = await handler.Handle(request.ToCommand(), ct);

                // Audit: successful login — record the canonical email from the authenticated
                // user, not the raw request value (casing/whitespace may differ).
                var entry = BitacoraEntry.Registrar(
                    result.UsuarioId,
                    result.Email,
                    result.Rol,
                    "Login",
                    detalle: null,
                    resultadoHttp: StatusCodes.Status200OK,
                    fechaUtc: DateTime.UtcNow);
                await bitacora.RegistrarAsync(entry, ct);

                return Results.Ok(result.ToResponse());
            }
            catch
            {
                // Audit: failed login — record the attempted email (all we have for an
                // unauthenticated request); do not reveal the failure reason in the detail.
                var failEntry = BitacoraEntry.RegistrarAnonimo(
                    email: request.Email,
                    accion: "Login failed",
                    detalle: null,
                    resultadoHttp: StatusCodes.Status401Unauthorized,
                    fechaUtc: DateTime.UtcNow);
                await bitacora.RegistrarAsync(failEntry, ct);

                throw; // re-throw so the exception handler maps it to the right status code
            }
        })
        .WithValidation<LoginRequest>();

        // POST /auth/refresh — exchange a valid refresh token for a new token pair (rotation)
        group.MapPost("/refresh", [AllowAnonymous] async (
            [FromBody] RefrescarTokenRequest request,
            RefrescarTokenHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(request.ToCommand(), ct);
            return Results.Ok(result.ToResponse());
        })
        .WithValidation<RefrescarTokenRequest>();

        // POST /auth/logout — revoke refresh token. Anonymous: possession of the token authorises revocation.
        group.MapPost("/logout", [AllowAnonymous] async (
            [FromBody] CerrarSesionRequest request,
            CerrarSesionHandler handler,
            CancellationToken ct) =>
        {
            await handler.Handle(request.ToCommand(), ct);
            return Results.NoContent();
        })
        .WithValidation<CerrarSesionRequest>();

        // POST /auth/logout-all — revoke every active session of the authenticated user
        group.MapPost("/logout-all", async (
            HttpContext http,
            CerrarSesionGlobalHandler handler,
            CancellationToken ct) =>
        {
            var sub = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(sub, out var usuarioId))
                return Results.Problem(
                    title: "Invalid or missing user identifier claim.",
                    statusCode: StatusCodes.Status403Forbidden);

            await handler.Handle(new CerrarSesionGlobalCommand(usuarioId), ct);
            return Results.NoContent();
        })
        .RequireAuthorization();

        return app;
    }
}
