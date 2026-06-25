using GastroGestion.Application.Abstractions.Persistence;

namespace GastroGestion.Application.Stock.GetProducibles;

/// <summary>
/// Returns the maximum producible quantity for every active dish.
/// Fetches all on-hand ingredient balances in a single grouped query (no N+1),
/// then applies the pure domain formula on each active Plato.
/// </summary>
public sealed class GetProduciblesHandler
{
    private readonly IPlatoRepository _platoRepo;
    private readonly IMovimientoStockRepository _stockRepo;

    public GetProduciblesHandler(
        IPlatoRepository platoRepo,
        IMovimientoStockRepository stockRepo)
    {
        _platoRepo = platoRepo;
        _stockRepo = stockRepo;
    }

    public async Task<IReadOnlyList<PlatoProducibleResult>> Handle(
        GetProduciblesQuery query,
        CancellationToken ct = default)
    {
        var platos   = await _platoRepo.GetAllAsync(ct);
        var balances = await _stockRepo.CalcularBalancesAsync(ct);

        return platos
            .Where(p => p.Activo)
            .Select(p => new PlatoProducibleResult(
                p.Id,
                p.Nombre,
                p.CalcularMaxProducible(balances)))
            .OrderBy(r => r.Nombre)
            .ToList();
    }
}
