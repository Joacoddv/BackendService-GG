using GastroGestion.Api.Filters;
using GastroGestion.Application.Pedidos.ActualizarLinea;
using GastroGestion.Application.Pedidos.AgregarLinea;
using GastroGestion.Application.Pedidos.BuscarPedidos;
using GastroGestion.Application.Pedidos.GenerarOrdenTrabajoLinea;
using GastroGestion.Application.Pedidos.QuitarLinea;
using GastroGestion.Application.Pedidos.ConfirmarPrecioLinea;
using GastroGestion.Application.Pedidos.CrearPedido;
using GastroGestion.Application.Pedidos.GetPedidoById;
using GastroGestion.Application.Pedidos.TransicionarEstadoPedido;
using GastroGestion.Contracts.Pedidos;
using GastroGestion.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GastroGestion.Api.Endpoints;

public static class PedidoEndpoints
{
    public static WebApplication MapPedidoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/pedidos").WithTags("Pedidos").RequireAuthorization();

        // GET /pedidos?estado=&tipo= — list orders (newest first), optional filters
        group.MapGet("/", async (
            HttpRequest request,
            BuscarPedidosHandler handler,
            CancellationToken ct) =>
        {
            EstadoPedido? estado = null;
            var estadoRaw = request.Query["estado"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(estadoRaw))
            {
                if (!Enum.TryParse<EstadoPedido>(estadoRaw, ignoreCase: true, out var parsed))
                    return Results.Problem(title: $"Invalid estado '{estadoRaw}'.", statusCode: StatusCodes.Status400BadRequest);
                estado = parsed;
            }

            TipoPedido? tipo = null;
            var tipoRaw = request.Query["tipo"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(tipoRaw))
            {
                if (!Enum.TryParse<TipoPedido>(tipoRaw, ignoreCase: true, out var parsed))
                    return Results.Problem(title: $"Invalid tipo '{tipoRaw}'.", statusCode: StatusCodes.Status400BadRequest);
                tipo = parsed;
            }

            var pedidos = await handler.Handle(new BuscarPedidosQuery(estado, tipo), ct);
            return Results.Ok(pedidos.Select(p => p.ToResponse()).ToList());
        });

        // POST /pedidos — create a new order
        group.MapPost("/", async (
            [FromBody] CrearPedidoRequest request,
            CrearPedidoHandler handler,
            CancellationToken ct) =>
        {
            var id = await handler.Handle(request.ToCommand(), ct);
            return Results.Created($"/pedidos/{id}", id);
        })
        .WithValidation<CrearPedidoRequest>();

        // POST /pedidos/{id}/lineas — add a line to an order
        group.MapPost("/{id:guid}/lineas", async (
            Guid id,
            [FromBody] AgregarLineaRequest request,
            AgregarLineaHandler handler,
            CancellationToken ct) =>
        {
            var lineaId = await handler.Handle(request.ToCommand(id), ct);
            return Results.Created($"/pedidos/{id}/lineas/{lineaId}", lineaId);
        })
        .WithValidation<AgregarLineaRequest>();

        // PUT /pedidos/{id}/lineas/{lineaId} — edit an existing line's quantity/notes; returns the order
        group.MapPut("/{id:guid}/lineas/{lineaId:guid}", async (
            Guid id,
            Guid lineaId,
            [FromBody] ActualizarLineaRequest request,
            ActualizarLineaHandler handler,
            GetPedidoByIdHandler getHandler,
            CancellationToken ct) =>
        {
            await handler.Handle(request.ToCommand(id, lineaId), ct);

            var pedido = await getHandler.Handle(new GetPedidoByIdQuery(id), ct);
            return Results.Ok(pedido!.ToResponse());
        })
        .WithValidation<ActualizarLineaRequest>();

        // DELETE /pedidos/{id}/lineas/{lineaId} — remove a line; returns the updated order
        group.MapDelete("/{id:guid}/lineas/{lineaId:guid}", async (
            Guid id,
            Guid lineaId,
            QuitarLineaHandler handler,
            GetPedidoByIdHandler getHandler,
            CancellationToken ct) =>
        {
            await handler.Handle(new QuitarLineaCommand(id, lineaId), ct);

            var pedido = await getHandler.Handle(new GetPedidoByIdQuery(id), ct);
            return Results.Ok(pedido!.ToResponse());
        });

        // POST /pedidos/{id}/lineas/{lineaId}/orden-trabajo — generate the kitchen OT for one
        // (newly added, already priced) line; returns the updated order
        group.MapPost("/{id:guid}/lineas/{lineaId:guid}/orden-trabajo", async (
            Guid id,
            Guid lineaId,
            GenerarOrdenTrabajoLineaHandler handler,
            GetPedidoByIdHandler getHandler,
            CancellationToken ct) =>
        {
            await handler.Handle(new GenerarOrdenTrabajoLineaCommand(id, lineaId), ct);

            var pedido = await getHandler.Handle(new GetPedidoByIdQuery(id), ct);
            return Results.Ok(pedido!.ToResponse());
        });

        // POST /pedidos/{id}/lineas/{lineaId}/confirmar-precio — confirm price snapshot (W-01 live path)
        group.MapPost("/{id:guid}/lineas/{lineaId:guid}/confirmar-precio", async (
            Guid id,
            Guid lineaId,
            ConfirmarPrecioLineaHandler handler,
            CancellationToken ct) =>
        {
            await handler.Handle(new ConfirmarPrecioLineaCommand(id, lineaId), ct);
            return Results.NoContent();
        });

        // POST /pedidos/{id}/transicion — transition order state; role comes from JWT claim
        group.MapPost("/{id:guid}/transicion", async (
            Guid id,
            [FromBody] TransicionarEstadoRequest request,
            HttpContext http,
            TransicionarEstadoPedidoHandler handler,
            GetPedidoByIdHandler getHandler,
            CancellationToken ct) =>
        {
            var rolClaim = http.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (rolClaim is null || !Enum.TryParse<RolUsuario>(rolClaim, out var rol))
                return Results.Problem(
                    title: "Invalid or missing role claim.",
                    statusCode: StatusCodes.Status403Forbidden);

            await handler.Handle(request.ToCommand(id, rol), ct);

            var pedido = await getHandler.Handle(new GetPedidoByIdQuery(id), ct);
            return Results.Ok(pedido!.ToResponse());
        });

        // GET /pedidos/{id} — get order by id
        group.MapGet("/{id:guid}", async (
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
