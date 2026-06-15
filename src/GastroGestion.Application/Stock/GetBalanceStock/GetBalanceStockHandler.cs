using GastroGestion.Application.Abstractions.Persistence;

namespace GastroGestion.Application.Stock.GetBalanceStock;

public sealed class GetBalanceStockHandler
{
    private readonly IMovimientoStockRepository _stock;

    public GetBalanceStockHandler(IMovimientoStockRepository stock) => _stock = stock;

    public async Task<decimal> Handle(GetBalanceStockQuery query, CancellationToken ct = default)
        => await _stock.CalcularBalanceAsync(query.IngredienteId, ct);
}
