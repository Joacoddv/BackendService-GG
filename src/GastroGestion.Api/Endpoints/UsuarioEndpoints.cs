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

namespace GastroGestion.Api.Endpoints;

/// <summary>
/// Endpoint group for /usuarios: cocineros listing (CCC-A01) + Admin-only user management CRUD.
/// Role enforcement is handled by named authorization policies defined in Program.cs.
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
            GetCocinerosHandler handler,
            CancellationToken ct) =>
        {
            var cocineros = await handler.Handle(new GetCocinerosQuery(), ct);
            return Results.Ok(cocineros.Select(u => u.ToCocineroResponse()).ToList());
        })
        .RequireAuthorization("CocineroOAdministrador");

        // POST /usuarios — register a new user (Admin only)
        group.MapPost("/", async (
            [FromBody] CrearUsuarioRequest request,
            CrearUsuarioHandler handler,
            CancellationToken ct) =>
        {
            var id = await handler.Handle(request.ToCommand(), ct);
            return Results.Created($"/usuarios/{id}", id);
        })
        .WithValidation<CrearUsuarioRequest>()
        .RequireAuthorization("SoloAdministrador")
        .WithBitacora("Create user");

        // GET /usuarios/{id} — get by id (Admin only)
        group.MapGet("/{id:guid}", async (
            Guid id,
            GetUsuarioByIdHandler handler,
            CancellationToken ct) =>
        {
            var usuario = await handler.Handle(new GetUsuarioByIdQuery(id), ct);
            return usuario is null
                ? Results.NotFound()
                : Results.Ok(usuario.ToResponse());
        })
        .RequireAuthorization("SoloAdministrador");

        // GET /usuarios?nombre=&rol=&incluirInactivos= — search (Admin only)
        group.MapGet("/", async (
            HttpRequest request,
            BuscarUsuariosHandler handler,
            CancellationToken ct) =>
        {
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
        })
        .RequireAuthorization("SoloAdministrador");

        // PUT /usuarios/{id} — edit name + role (Admin only)
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] EditarUsuarioRequest request,
            EditarUsuarioHandler handler,
            CancellationToken ct) =>
        {
            var usuario = await handler.Handle(request.ToCommand(id), ct);
            return Results.Ok(usuario.ToResponse());
        })
        .WithValidation<EditarUsuarioRequest>()
        .RequireAuthorization("SoloAdministrador")
        .WithBitacora("Update user");

        // DELETE /usuarios/{id} — soft-delete (Admin only)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            DesactivarUsuarioHandler handler,
            CancellationToken ct) =>
        {
            await handler.Handle(new DesactivarUsuarioCommand(id), ct);
            return Results.NoContent();
        })
        .RequireAuthorization("SoloAdministrador")
        .WithBitacora("Deactivate user");

        return app;
    }
}
