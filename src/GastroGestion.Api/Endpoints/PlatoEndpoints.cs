using GastroGestion.Api.Filters;
using GastroGestion.Application.Platos.CrearPlato;
using GastroGestion.Application.Platos.GetAllPlatos;
using GastroGestion.Application.Platos.GetPlatoById;
using GastroGestion.Contracts.Platos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastroGestion.Api.Endpoints;

public static class PlatoEndpoints
{
    public static WebApplication MapPlatoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/platos").WithTags("Platos");

        group.MapPost("/", [AllowAnonymous] async (
            [FromBody] CrearPlatoRequest request,
            CrearPlatoHandler handler,
            CancellationToken ct) =>
        {
            var id = await handler.Handle(request.ToCommand(), ct);
            return Results.Created($"/platos/{id}", id);
        })
        .WithValidation<CrearPlatoRequest>();

        group.MapGet("/{id:guid}", [AllowAnonymous] async (
            Guid id,
            GetPlatoByIdHandler handler,
            CancellationToken ct) =>
        {
            var plato = await handler.Handle(new GetPlatoByIdQuery(id), ct);
            return plato is null
                ? Results.NotFound()
                : Results.Ok(plato.ToResponse());
        });

        group.MapGet("/", [AllowAnonymous] async (
            GetAllPlatosHandler handler,
            CancellationToken ct) =>
        {
            var list = await handler.Handle(new GetAllPlatosQuery(), ct);
            return Results.Ok(list.Select(p => p.ToResponse()).ToList());
        });

        return app;
    }
}
