using GastroGestion.Application.Abstractions.Persistence;

namespace GastroGestion.Application.Stock.GetBalancesStock;

/// <summary>
/// Returns the current ledger balance for every ingredient (name + unit included), ordered by name.
/// </summary>
public sealed class GetBalancesStockHandler
{
    private readonly IIngredienteRepository     _ingredientes;
    private readonly IMovimientoStockRepository _stock;

    public GetBalancesStockHandler(IIngredienteRepository ingredientes, IMovimientoStockRepository stock)
    {
        _ingredientes = ingredientes;
        _stock        = stock;
    }

    public async Task<IReadOnlyList<IngredienteBalanceResult>> Handle(
        GetBalancesStockQuery query, CancellationToken ct = default)
    {
        var ingredientes = await _ingredientes.GetAllAsync(ct);
        var balances     = await _stock.CalcularBalancesAsync(ct);

        var result = new List<IngredienteBalanceResult>(ingredientes.Count);
        foreach (var ing in ingredientes)
        {
            var balance = balances.GetValueOrDefault(ing.Id, 0m);
            // Low-stock when at/below the reorder threshold. With the default threshold of 0 this
            // only fires when the ingredient is actually out of stock.
            var enAlerta = balance <= ing.StockMinimo;
            result.Add(new IngredienteBalanceResult(
                ing.Id, ing.Nombre, ing.UnidadBase, ing.Activo, balance, ing.StockMinimo, enAlerta));
        }

        return result
            .OrderBy(r => r.Nombre)
            .ToList();
    }
}
