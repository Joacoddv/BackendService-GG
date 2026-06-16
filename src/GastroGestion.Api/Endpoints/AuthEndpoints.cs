using GastroGestion.Api.Filters;
using GastroGestion.Application.Auth.Login;
using GastroGestion.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        return app;
    }
}
