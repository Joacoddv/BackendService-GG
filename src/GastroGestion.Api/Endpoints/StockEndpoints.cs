using GastroGestion.Api.Filters;
using GastroGestion.Application.Stock.GetBalanceStock;
using GastroGestion.Application.Stock.RegistrarMovimientoStock;
using GastroGestion.Contracts.Stock;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastroGestion.Api.Endpoints;

public static class StockEndpoints
{
    public static WebApplication MapStockEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/stock").WithTags("Stock");

        // POST /stock/movimientos — register a stock movement
        group.MapPost("/movimientos", [AllowAnonymous] async (
            [FromBody] RegistrarMovimientoStockRequest request,
            RegistrarMovimientoStockHandler handler,
            CancellationToken ct) =>
        {
            var id = await handler.Handle(request.ToCommand(), ct);
            return Results.Created($"/stock/movimientos/{id}", id);
        })
        .WithValidation<RegistrarMovimientoStockRequest>();

        // GET /stock/balance/{ingredienteId} — get current stock balance
        // NOTE: Never returns 404 — zero-balance is valid (Scenario 17-D)
        group.MapGet("/balance/{ingredienteId:guid}", [AllowAnonymous] async (
            Guid ingredienteId,
            GetBalanceStockHandler handler,
            CancellationToken ct) =>
        {
            var balance = await handler.Handle(new GetBalanceStockQuery(ingredienteId), ct);
            return Results.Ok(new BalanceStockResponse(ingredienteId, balance));
        });

        return app;
    }
}
