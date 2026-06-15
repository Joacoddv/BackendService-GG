using GastroGestion.Api.Filters;
using GastroGestion.Application.Ingredientes.CrearIngrediente;
using GastroGestion.Application.Ingredientes.GetAllIngredientes;
using GastroGestion.Application.Ingredientes.GetIngredienteById;
using GastroGestion.Contracts.Ingredientes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastroGestion.Api.Endpoints;

public static class IngredienteEndpoints
{
    public static WebApplication MapIngredienteEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/ingredientes").WithTags("Ingredientes");

        group.MapPost("/", [AllowAnonymous] async (
            [FromBody] CrearIngredienteRequest request,
            CrearIngredienteHandler handler,
            CancellationToken ct) =>
        {
            var id = await handler.Handle(request.ToCommand(), ct);
            return Results.Created($"/ingredientes/{id}", id);
        })
        .WithValidation<CrearIngredienteRequest>();

        group.MapGet("/{id:guid}", [AllowAnonymous] async (
            Guid id,
            GetIngredienteByIdHandler handler,
            CancellationToken ct) =>
        {
            var ingrediente = await handler.Handle(new GetIngredienteByIdQuery(id), ct);
            return ingrediente is null
                ? Results.NotFound()
                : Results.Ok(ingrediente.ToResponse());
        });

        group.MapGet("/", [AllowAnonymous] async (
            GetAllIngredientesHandler handler,
            CancellationToken ct) =>
        {
            var list = await handler.Handle(new GetAllIngredientesQuery(), ct);
            return Results.Ok(list.Select(i => i.ToResponse()).ToList());
        });

        return app;
    }
}
