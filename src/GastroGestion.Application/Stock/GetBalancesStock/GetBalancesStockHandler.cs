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

        var result = new List<IngredienteBalanceResult>(ingredientes.Count);
        foreach (var ing in ingredientes)
        {
            var balance = await _stock.CalcularBalanceAsync(ing.Id, ct);
            result.Add(new IngredienteBalanceResult(ing.Id, ing.Nombre, ing.UnidadBase, ing.Activo, balance));
        }

        return result
            .OrderBy(r => r.Nombre)
            .ToList();
    }
}
