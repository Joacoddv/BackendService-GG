using GastroGestion.Api.Filters;
using GastroGestion.Application.Ingredientes.ActualizarStockMinimo;
using GastroGestion.Application.Ingredientes.BuscarIngredientes;
using GastroGestion.Application.Ingredientes.CrearIngrediente;
using GastroGestion.Application.Ingredientes.DesactivarIngrediente;
using GastroGestion.Application.Ingredientes.EditarIngrediente;
using GastroGestion.Application.Ingredientes.GetIngredienteById;
using GastroGestion.Contracts.Ingredientes;
using Microsoft.AspNetCore.Mvc;

namespace GastroGestion.Api.Endpoints;

public static class IngredienteEndpoints
{
    public static WebApplication MapIngredienteEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/ingredientes").WithTags("Ingredientes").RequireAuthorization();

        // POST /ingredientes — create
        group.MapPost("/", async (
            [FromBody] CrearIngredienteRequest request,
            CrearIngredienteHandler handler,
            CancellationToken ct) =>
        {
            var id = await handler.Handle(request.ToCommand(), ct);
            return Results.Created($"/ingredientes/{id}", id);
        })
        .WithValidation<CrearIngredienteRequest>()
        .WithBitacora("Create ingredient");

        // GET /ingredientes/{id} — get by id
        group.MapGet("/{id:guid}", async (
            Guid id,
            GetIngredienteByIdHandler handler,
            CancellationToken ct) =>
        {
            var ingrediente = await handler.Handle(new GetIngredienteByIdQuery(id), ct);
            return ingrediente is null
                ? Results.NotFound()
                : Results.Ok(ingrediente.ToResponse());
        });

        // GET /ingredientes?nombre=&incluirInactivos= — search
        group.MapGet("/", async (
            HttpRequest request,
            BuscarIngredientesHandler handler,
            CancellationToken ct) =>
        {
            var nombre           = request.Query["nombre"].FirstOrDefault();
            var incluirInactivos = string.Equals(
                request.Query["incluirInactivos"].FirstOrDefault(), "true",
                StringComparison.OrdinalIgnoreCase);

            var list = await handler.Handle(new BuscarIngredientesQuery(nombre, incluirInactivos), ct);
            return Results.Ok(list.Select(i => i.ToResponse()).ToList());
        });

        // PUT /ingredientes/{id} — edit Nombre (Admin only, CCC-C01)
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] EditarIngredienteRequest request,
            EditarIngredienteHandler handler,
            CancellationToken ct) =>
        {
            var ingrediente = await handler.Handle(request.ToCommand(id), ct);
            return Results.Ok(ingrediente.ToResponse());
        })
        .WithValidation<EditarIngredienteRequest>()
        .RequireAuthorization("SoloAdministrador")
        .WithBitacora("Update ingredient");

        // PUT /ingredientes/{id}/stock-minimo — set reorder threshold (Admin only)
        group.MapPut("/{id:guid}/stock-minimo", async (
            Guid id,
            [FromBody] ActualizarStockMinimoRequest request,
            ActualizarStockMinimoHandler handler,
            CancellationToken ct) =>
        {
            await handler.Handle(request.ToCommand(id), ct);
            return Results.NoContent();
        })
        .WithValidation<ActualizarStockMinimoRequest>()
        .RequireAuthorization("SoloAdministrador")
        .WithBitacora("Update ingredient minimum stock");

        // DELETE /ingredientes/{id} — soft-delete (Admin only, CCC-C02)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            DesactivarIngredienteHandler handler,
            CancellationToken ct) =>
        {
            await handler.Handle(new DesactivarIngredienteCommand(id), ct);
            return Results.NoContent();
        })
        .RequireAuthorization("SoloAdministrador")
        .WithBitacora("Deactivate ingredient");

        return app;
    }
}
