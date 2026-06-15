using GastroGestion.Api.Filters;
using GastroGestion.Application.Clientes.CrearCliente;
using GastroGestion.Application.Clientes.GetAllClientes;
using GastroGestion.Application.Clientes.GetClienteById;
using GastroGestion.Contracts.Clientes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastroGestion.Api.Endpoints;

public static class ClienteEndpoints
{
    public static WebApplication MapClienteEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/clientes").WithTags("Clientes");

        group.MapPost("/", [AllowAnonymous] async (
            [FromBody] CrearClienteRequest request,
            CrearClienteHandler handler,
            CancellationToken ct) =>
        {
            var id = await handler.Handle(request.ToCommand(), ct);
            return Results.Created($"/clientes/{id}", id);
        })
        .WithValidation<CrearClienteRequest>();

        group.MapGet("/{id:guid}", [AllowAnonymous] async (
            Guid id,
            GetClienteByIdHandler handler,
            CancellationToken ct) =>
        {
            var cliente = await handler.Handle(new GetClienteByIdQuery(id), ct);
            return cliente is null
                ? Results.NotFound()
                : Results.Ok(cliente.ToResponse());
        });

        group.MapGet("/", [AllowAnonymous] async (
            GetAllClientesHandler handler,
            CancellationToken ct) =>
        {
            var list = await handler.Handle(new GetAllClientesQuery(), ct);
            return Results.Ok(list.Select(c => c.ToResponse()).ToList());
        });

        return app;
    }
}
