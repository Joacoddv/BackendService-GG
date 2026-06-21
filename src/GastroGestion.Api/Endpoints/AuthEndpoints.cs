using GastroGestion.Api.Filters;
using GastroGestion.Application.Auth.CerrarSesion;
using GastroGestion.Application.Auth.CerrarSesionGlobal;
using GastroGestion.Application.Auth.Login;
using GastroGestion.Application.Auth.RefrescarToken;
using GastroGestion.Contracts.Auth;
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
            CancellationToken ct) =>
        {
            var result = await handler.Handle(request.ToCommand(), ct);
            return Results.Ok(result.ToResponse());
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

        // POST /auth/logout — revoke the presented refresh token (single session). Anonymous like
        // /refresh: the access token may already be expired at logout time, and possession of the
        // refresh token is what authorises its revocation. Always 204 (idempotent, non-revealing).
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
        // ("log out everywhere"). Requires a valid access token; the user id comes from its claims.
        group.MapPost("/logout-all", async (
            HttpContext http,
            CerrarSesionGlobalHandler handler,
            CancellationToken ct) =>
        {
            // sub is mapped to NameIdentifier by the JWT bearer middleware (default inbound mapping).
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
