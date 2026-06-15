using GastroGestion.Api.Filters;
using GastroGestion.Application.Pedidos.AgregarLinea;
using GastroGestion.Application.Pedidos.ConfirmarPrecioLinea;
using GastroGestion.Application.Pedidos.CrearPedido;
using GastroGestion.Application.Pedidos.GetPedidoById;
using GastroGestion.Application.Pedidos.TransicionarEstadoPedido;
using GastroGestion.Contracts.Pedidos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastroGestion.Api.Endpoints;

public static class PedidoEndpoints
{
    public static WebApplication MapPedidoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/pedidos").WithTags("Pedidos");

        // POST /pedidos — create a new order
        group.MapPost("/", [AllowAnonymous] async (
            [FromBody] CrearPedidoRequest request,
            CrearPedidoHandler handler,
            CancellationToken ct) =>
        {
            var id = await handler.Handle(request.ToCommand(), ct);
            return Results.Created($"/pedidos/{id}", id);
        })
        .WithValidation<CrearPedidoRequest>();

        // POST /pedidos/{id}/lineas — add a line to an order
        group.MapPost("/{id:guid}/lineas", [AllowAnonymous] async (
            Guid id,
            [FromBody] AgregarLineaRequest request,
            AgregarLineaHandler handler,
            CancellationToken ct) =>
        {
            var lineaId = await handler.Handle(request.ToCommand(id), ct);
            return Results.Created($"/pedidos/{id}/lineas/{lineaId}", lineaId);
        })
        .WithValidation<AgregarLineaRequest>();

        // POST /pedidos/{id}/lineas/{lineaId}/confirmar-precio — confirm price snapshot (W-01 live path)
        group.MapPost("/{id:guid}/lineas/{lineaId:guid}/confirmar-precio", [AllowAnonymous] async (
            Guid id,
            Guid lineaId,
            ConfirmarPrecioLineaHandler handler,
            CancellationToken ct) =>
        {
            await handler.Handle(new ConfirmarPrecioLineaCommand(id, lineaId), ct);
            return Results.NoContent();
        });

        // POST /pedidos/{id}/transicion — transition order state
        group.MapPost("/{id:guid}/transicion", [AllowAnonymous] async (
            Guid id,
            [FromBody] TransicionarEstadoRequest request,
            TransicionarEstadoPedidoHandler handler,
            GetPedidoByIdHandler getHandler,
            CancellationToken ct) =>
        {
            // PHASE-5: replace body-supplied Rol with JWT claim (User.FindFirst(ClaimTypes.Role))
            await handler.Handle(request.ToCommand(id), ct);

            var pedido = await getHandler.Handle(new GetPedidoByIdQuery(id), ct);
            return Results.Ok(pedido!.ToResponse());
        });

        // GET /pedidos/{id} — get order by id
        group.MapGet("/{id:guid}", [AllowAnonymous] async (
            Guid id,
            GetPedidoByIdHandler handler,
            CancellationToken ct) =>
        {
            var pedido = await handler.Handle(new GetPedidoByIdQuery(id), ct);
            return pedido is null
                ? Results.NotFound()
                : Results.Ok(pedido.ToResponse());
        });

        return app;
    }
}
