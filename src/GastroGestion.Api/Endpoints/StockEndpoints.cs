using GastroGestion.Api.Filters;
using GastroGestion.Application.Stock.GetBalanceStock;
using GastroGestion.Application.Stock.GetBalancesStock;
using GastroGestion.Application.Stock.GetMovimientosStock;
using GastroGestion.Application.Stock.GetProducibles;
using GastroGestion.Application.Stock.RegistrarMovimientoStock;
using GastroGestion.Contracts.Stock;
using Microsoft.AspNetCore.Mvc;

namespace GastroGestion.Api.Endpoints;

public static class StockEndpoints
{
    public static WebApplication MapStockEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/stock").WithTags("Stock").RequireAuthorization();

        // POST /stock/movimientos — register a stock movement
        group.MapPost("/movimientos", async (
            [FromBody] RegistrarMovimientoStockRequest request,
            RegistrarMovimientoStockHandler handler,
            CancellationToken ct) =>
        {
            var id = await handler.Handle(request.ToCommand(), ct);
            return Results.Created($"/stock/movimientos/{id}", id);
        })
        .WithValidation<RegistrarMovimientoStockRequest>();

        // GET /stock/balances — current balance for every ingredient (name + unit), ordered by name
        group.MapGet("/balances", async (
            GetBalancesStockHandler handler,
            CancellationToken ct) =>
        {
            var balances = await handler.Handle(new GetBalancesStockQuery(), ct);
            return Results.Ok(balances.Select(b => b.ToResponse()).ToList());
        });

        // GET /stock/movimientos/{ingredienteId} — ledger history for an ingredient, newest first
        group.MapGet("/movimientos/{ingredienteId:guid}", async (
            Guid ingredienteId,
            GetMovimientosStockHandler handler,
            CancellationToken ct) =>
        {
            var movimientos = await handler.Handle(new GetMovimientosStockQuery(ingredienteId), ct);
            return Results.Ok(movimientos.Select(m => m.ToResponse()).ToList());
        });

        // GET /stock/balance/{ingredienteId} — get current stock balance
        // NOTE: Never returns 404 — zero-balance is valid (Scenario 17-D)
        group.MapGet("/balance/{ingredienteId:guid}", async (
            Guid ingredienteId,
            GetBalanceStockHandler handler,
            CancellationToken ct) =>
        {
            var balance = await handler.Handle(new GetBalanceStockQuery(ingredienteId), ct);
            return Results.Ok(new BalanceStockResponse(ingredienteId, balance));
        });

        // GET /stock/producibles — maximum producible quantity per active dish, ordered by name
        group.MapGet("/producibles", async (
            GetProduciblesHandler handler,
            CancellationToken ct) =>
        {
            var results = await handler.Handle(new GetProduciblesQuery(), ct);
            return Results.Ok(results.Select(r => r.ToResponse()).ToList());
        });

        return app;
    }
}
