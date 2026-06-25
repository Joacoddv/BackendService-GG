using GastroGestion.Api.Filters;
using GastroGestion.Application.Proveedores.BuscarProveedores;
using GastroGestion.Application.Proveedores.CrearProveedor;
using GastroGestion.Application.Proveedores.DesactivarProveedor;
using GastroGestion.Application.Proveedores.EditarProveedor;
using GastroGestion.Application.Proveedores.GetProveedorById;
using GastroGestion.Contracts.Proveedores;
using Microsoft.AspNetCore.Mvc;

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
        .WithValidation<CrearProveedorRequest>()
        .WithBitacora("Create supplier");

        // GET /proveedores/{id} — get by id
        group.MapGet("/{id:guid}", async (
            Guid id,
            GetProveedorByIdHandler handler,
            CancellationToken ct) =>
        {
            var proveedor = await handler.Handle(new GetProveedorByIdQuery(id), ct);
            return proveedor is null ? Results.NotFound() : Results.Ok(proveedor.ToResponse());
        });

        // GET /proveedores?nombre=&incluirInactivos=
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
            EditarProveedorHandler handler,
            CancellationToken ct) =>
        {
            var proveedor = await handler.Handle(request.ToCommand(id), ct);
            return Results.Ok(proveedor.ToResponse());
        })
        .WithValidation<EditarProveedorRequest>()
        .RequireAuthorization("SoloAdministrador")
        .WithBitacora("Update supplier");

        // DELETE /proveedores/{id} — soft-delete (Admin only)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            DesactivarProveedorHandler handler,
            CancellationToken ct) =>
        {
            await handler.Handle(new DesactivarProveedorCommand(id), ct);
            return Results.NoContent();
        })
        .RequireAuthorization("SoloAdministrador")
        .WithBitacora("Deactivate supplier");

        return app;
    }
}
