using GastroGestion.Api.Filters;
using GastroGestion.Application.Usuarios.BuscarUsuarios;
using GastroGestion.Application.Usuarios.CrearUsuario;
using GastroGestion.Application.Usuarios.DesactivarUsuario;
using GastroGestion.Application.Usuarios.EditarUsuario;
using GastroGestion.Application.Usuarios.GetCocineros;
using GastroGestion.Application.Usuarios.GetUsuarioById;
using GastroGestion.Contracts.Usuarios;
using GastroGestion.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GastroGestion.Api.Endpoints;

/// <summary>
/// Endpoint group for /usuarios: cocineros listing (CCC-A01) + the Admin-only
/// user management CRUD (registration, list, edit, soft-delete).
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

        // POST /usuarios — register a new user (Admin only)
        group.MapPost("/", async (
            [FromBody] CrearUsuarioRequest request,
            HttpContext http,
            CrearUsuarioHandler handler,
            CancellationToken ct) =>
        {
            if (RequireAdmin(http) is { } denied) return denied;

            var id = await handler.Handle(request.ToCommand(), ct);
            return Results.Created($"/usuarios/{id}", id);
        })
        .WithValidation<CrearUsuarioRequest>();

        // GET /usuarios/{id} — get by id (Admin only)
        group.MapGet("/{id:guid}", async (
            Guid id,
            HttpContext http,
            GetUsuarioByIdHandler handler,
            CancellationToken ct) =>
        {
            if (RequireAdmin(http) is { } denied) return denied;

            var usuario = await handler.Handle(new GetUsuarioByIdQuery(id), ct);
            return usuario is null
                ? Results.NotFound()
                : Results.Ok(usuario.ToResponse());
        });

        // GET /usuarios?nombre=&rol=&incluirInactivos= — search (Admin only)
        group.MapGet("/", async (
            HttpRequest request,
            HttpContext http,
            BuscarUsuariosHandler handler,
            CancellationToken ct) =>
        {
            if (RequireAdmin(http) is { } denied) return denied;

            var nombre = request.Query["nombre"].FirstOrDefault();

            RolUsuario? rolFiltro = null;
            var rolRaw = request.Query["rol"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(rolRaw))
            {
                if (!Enum.TryParse<RolUsuario>(rolRaw, ignoreCase: true, out var parsed))
                    return Results.Problem(title: $"Invalid rol '{rolRaw}'.", statusCode: StatusCodes.Status400BadRequest);
                rolFiltro = parsed;
            }

            var incluirInactivos = string.Equals(
                request.Query["incluirInactivos"].FirstOrDefault(), "true",
                StringComparison.OrdinalIgnoreCase);

            var list = await handler.Handle(new BuscarUsuariosQuery(nombre, rolFiltro, incluirInactivos), ct);
            return Results.Ok(list.Select(u => u.ToResponse()).ToList());
        });

        // PUT /usuarios/{id} — edit name + role (Admin only)
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] EditarUsuarioRequest request,
            HttpContext http,
            EditarUsuarioHandler handler,
            CancellationToken ct) =>
        {
            if (RequireAdmin(http) is { } denied) return denied;

            var usuario = await handler.Handle(request.ToCommand(id), ct);
            return Results.Ok(usuario.ToResponse());
        })
        .WithValidation<EditarUsuarioRequest>();

        // DELETE /usuarios/{id} — soft-delete (Admin only)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            HttpContext http,
            DesactivarUsuarioHandler handler,
            CancellationToken ct) =>
        {
            if (RequireAdmin(http) is { } denied) return denied;

            await handler.Handle(new DesactivarUsuarioCommand(id), ct);
            return Results.NoContent();
        });

        return app;
    }

    /// <summary>
    /// Returns a 403 ProblemDetails result when the caller is not an Administrador,
    /// or <c>null</c> when access is granted.
    /// </summary>
    private static IResult? RequireAdmin(HttpContext http)
    {
        var rolClaim = http.User.FindFirst(ClaimTypes.Role)?.Value;
        if (rolClaim is null || !Enum.TryParse<RolUsuario>(rolClaim, out var rol))
            return Results.Problem(
                title: "Invalid or missing role claim.",
                statusCode: StatusCodes.Status403Forbidden);

        if (rol is not RolUsuario.Administrador)
            return Results.Problem(
                title: "Access denied. Required role: Administrador.",
                statusCode: StatusCodes.Status403Forbidden);

        return null;
    }
}
