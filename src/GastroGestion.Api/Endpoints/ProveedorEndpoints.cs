using GastroGestion.Api.Filters;
using GastroGestion.Application.Proveedores.BuscarProveedores;
using GastroGestion.Application.Proveedores.CrearProveedor;
using GastroGestion.Application.Proveedores.DesactivarProveedor;
using GastroGestion.Application.Proveedores.EditarProveedor;
using GastroGestion.Application.Proveedores.GetProveedorById;
using GastroGestion.Contracts.Proveedores;
using GastroGestion.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GastroGestion.Api.Endpoints;

public static class ProveedorEndpoints
{
    public static WebApplication MapProveedorEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/proveedores").WithTags("Proveedores").RequireAuthorization();

        // POST /proveedores — create
        group.MapPost("/", async (
            [FromBody] CrearProveedorRequest request,
            CrearProveedorHandler handler,
            CancellationToken ct) =>
        {
            var id = await handler.Handle(request.ToCommand(), ct);
            return Results.Created($"/proveedores/{id}", id);
        })
        .WithValidation<CrearProveedorRequest>();

        // GET /proveedores/{id} — get by id
        group.MapGet("/{id:guid}", async (
            Guid id,
            GetProveedorByIdHandler handler,
            CancellationToken ct) =>
        {
            var proveedor = await handler.Handle(new GetProveedorByIdQuery(id), ct);
            return proveedor is null ? Results.NotFound() : Results.Ok(proveedor.ToResponse());
        });

        // GET /proveedores?nombre=&incluirInactivos= — search (hides inactive by default)
        group.MapGet("/", async (
            HttpRequest request,
            BuscarProveedoresHandler handler,
            CancellationToken ct) =>
        {
            var nombre = request.Query["nombre"].FirstOrDefault();
            var incluirInactivos = string.Equals(
                request.Query["incluirInactivos"].FirstOrDefault(), "true",
                StringComparison.OrdinalIgnoreCase);

            var list = await handler.Handle(new BuscarProveedoresQuery(nombre, incluirInactivos), ct);
            return Results.Ok(list.Select(p => p.ToResponse()).ToList());
        });

        // PUT /proveedores/{id} — edit (Admin only)
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] EditarProveedorRequest request,
            HttpContext http,
            EditarProveedorHandler handler,
            CancellationToken ct) =>
        {
            if (RequireAdmin(http) is { } denied) return denied;

            var proveedor = await handler.Handle(request.ToCommand(id), ct);
            return Results.Ok(proveedor.ToResponse());
        })
        .WithValidation<EditarProveedorRequest>();

        // DELETE /proveedores/{id} — soft-delete (Admin only)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            HttpContext http,
            DesactivarProveedorHandler handler,
            CancellationToken ct) =>
        {
            if (RequireAdmin(http) is { } denied) return denied;

            await handler.Handle(new DesactivarProveedorCommand(id), ct);
            return Results.NoContent();
        });

        return app;
    }

    private static IResult? RequireAdmin(HttpContext http)
    {
        var rolClaim = http.User.FindFirst(ClaimTypes.Role)?.Value;
        if (rolClaim is null || !Enum.TryParse<RolUsuario>(rolClaim, out var rol))
            return Results.Problem(title: "Invalid or missing role claim.", statusCode: StatusCodes.Status403Forbidden);

        if (rol is not RolUsuario.Administrador)
            return Results.Problem(title: "Access denied. Required role: Administrador.", statusCode: StatusCodes.Status403Forbidden);

        return null;
    }
}
