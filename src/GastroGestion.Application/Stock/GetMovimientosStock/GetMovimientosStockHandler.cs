using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Stock;

namespace GastroGestion.Application.Stock.GetMovimientosStock;

/// <summary>Returns an ingredient's stock-ledger movements, newest first.</summary>
public sealed class GetMovimientosStockHandler
{
    private readonly IMovimientoStockRepository _stock;

    public GetMovimientosStockHandler(IMovimientoStockRepository stock) => _stock = stock;

    public async Task<IReadOnlyList<MovimientoStock>> Handle(GetMovimientosStockQuery query, CancellationToken ct = default)
        => await _stock.GetByIngredienteAsync(query.IngredienteId, ct);
}
