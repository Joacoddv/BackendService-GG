using GastroGestion.Api.Filters;
using GastroGestion.Application.Pedidos.AsignarCocinero;
using GastroGestion.Application.Pedidos.GenerarOrdenesTrabajo;
using GastroGestion.Application.Pedidos.GetOrdenesByEstado;
using GastroGestion.Application.Pedidos.GetPedidoById;
using GastroGestion.Application.Pedidos.MarcarOrdenTrabajoLista;
using GastroGestion.Contracts.Pedidos;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Pedidos;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GastroGestion.Api.Endpoints;

public static class OrdenTrabajoEndpoints
{
    public static WebApplication MapOrdenTrabajoEndpoints(this WebApplication app)
    {
        // ── Nested mutation group: /pedidos/{pedidoId}/ordenes-trabajo/... ──────
        var nested = app
            .MapGroup("/pedidos/{pedidoId:guid}/ordenes-trabajo")
            .WithTags("OrdenesTrabajo")
            .RequireAuthorization();

        // POST /pedidos/{pedidoId}/ordenes-trabajo — generate work orders (Mozo + Admin)
        nested.MapPost("/", async (
            Guid pedidoId,
            HttpContext http,
            GenerarOrdenesTrabajoHandler handler,
            CancellationToken ct) =>
        {
            var rolClaim = http.User.FindFirst(ClaimTypes.Role)?.Value;
            if (rolClaim is null || !Enum.TryParse<RolUsuario>(rolClaim, out var rol))
                return Results.Problem(
                    title: "Invalid or missing role claim.",
                    statusCode: StatusCodes.Status403Forbidden);

            await handler.Handle(new GenerarOrdenesTrabajoCommand(pedidoId, rol), ct);
            return Results.NoContent();
        });

        // POST /pedidos/{pedidoId}/ordenes-trabajo/{otId}/asignar-cocinero — Cocinero + Admin
        nested.MapPost("/{otId:guid}/asignar-cocinero", async (
            Guid pedidoId,
            Guid otId,
            [FromBody] AsignarCocineroRequest request,
            HttpContext http,
            AsignarCocineroHandler handler,
            GetPedidoByIdHandler getHandler,
            CancellationToken ct) =>
        {
            var rolClaim = http.User.FindFirst(ClaimTypes.Role)?.Value;
            if (rolClaim is null || !Enum.TryParse<RolUsuario>(rolClaim, out var rol))
                return Results.Problem(
                    title: "Invalid or missing role claim.",
                    statusCode: StatusCodes.Status403Forbidden);

            await handler.Handle(request.ToCommand(pedidoId, otId, rol), ct);

            // Reload aggregate post-commit to project updated OT state (OW-11 design decision)
            var pedido = await getHandler.Handle(new GetPedidoByIdQuery(pedidoId), ct);
            var ot = pedido!.OrdenesTrabajo.First(o => o.Id == otId);
            return Results.Ok(ot.ToResponse(pedidoId));
        })
        .WithValidation<AsignarCocineroRequest>();

        // POST /pedidos/{pedidoId}/ordenes-trabajo/{otId}/marcar-lista — Cocinero + Admin
        nested.MapPost("/{otId:guid}/marcar-lista", async (
            Guid pedidoId,
            Guid otId,
            HttpContext http,
            MarcarOrdenTrabajoListaHandler handler,
            GetPedidoByIdHandler getHandler,
            CancellationToken ct) =>
        {
            var rolClaim = http.User.FindFirst(ClaimTypes.Role)?.Value;
            if (rolClaim is null || !Enum.TryParse<RolUsuario>(rolClaim, out var rol))
                return Results.Problem(
                    title: "Invalid or missing role claim.",
                    statusCode: StatusCodes.Status403Forbidden);

            await handler.Handle(new MarcarOrdenTrabajoListaCommand(pedidoId, otId, rol), ct);

            // Reload aggregate post-commit to project updated OT state (OW-11 design decision)
            var pedido = await getHandler.Handle(new GetPedidoByIdQuery(pedidoId), ct);
            var ot = pedido!.OrdenesTrabajo.First(o => o.Id == otId);
            return Results.Ok(ot.ToResponse(pedidoId));
        });

        // ── Board read group: GET /ordenes-trabajo ────────────────────────────
        var board = app
            .MapGroup("/ordenes-trabajo")
            .WithTags("OrdenesTrabajo")
            .RequireAuthorization();

        // GET /ordenes-trabajo?estado={EstadoOT?} — any authenticated role
        board.MapGet("/", async (
            [FromQuery] string? estado,
            GetOrdenesByEstadoHandler handler,
            CancellationToken ct) =>
        {
            EstadoOT? estadoFilter = null;
            if (estado is not null)
            {
                if (!Enum.TryParse<EstadoOT>(estado, ignoreCase: true, out var parsed))
                    return Results.Problem(
                        title: "Invalid estado value.",
                        detail: $"'{estado}' is not a valid EstadoOT. Valid values: {string.Join(", ", Enum.GetNames<EstadoOT>())}.",
                        statusCode: StatusCodes.Status400BadRequest);
                estadoFilter = parsed;
            }

            var items = await handler.Handle(new GetOrdenesByEstadoQuery(estadoFilter), ct);
            return Results.Ok(items.Select(i => i.ToResponse()).ToList());
        });

        return app;
    }
}
