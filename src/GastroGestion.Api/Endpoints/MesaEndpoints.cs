using GastroGestion.Api.Filters;
using GastroGestion.Application.Mesas.CrearMesa;
using GastroGestion.Application.Mesas.GetAllMesas;
using GastroGestion.Application.Mesas.GetMesaById;
using GastroGestion.Contracts.Mesas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastroGestion.Api.Endpoints;

public static class MesaEndpoints
{
    public static WebApplication MapMesaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/mesas").WithTags("Mesas");

        group.MapPost("/", [AllowAnonymous] async (
            [FromBody] CrearMesaRequest request,
            CrearMesaHandler handler,
            CancellationToken ct) =>
        {
            var id = await handler.Handle(request.ToCommand(), ct);
            return Results.Created($"/mesas/{id}", id);
        })
        .WithValidation<CrearMesaRequest>();

        group.MapGet("/{id:guid}", [AllowAnonymous] async (
            Guid id,
            GetMesaByIdHandler handler,
            CancellationToken ct) =>
        {
            var mesa = await handler.Handle(new GetMesaByIdQuery(id), ct);
            return mesa is null
                ? Results.NotFound()
                : Results.Ok(mesa.ToResponse());
        });

        group.MapGet("/", [AllowAnonymous] async (
            GetAllMesasHandler handler,
            CancellationToken ct) =>
        {
            var list = await handler.Handle(new GetAllMesasQuery(), ct);
            return Results.Ok(list.Select(m => m.ToResponse()).ToList());
        });

        return app;
    }
}
