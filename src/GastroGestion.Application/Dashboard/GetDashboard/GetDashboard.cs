using GastroGestion.Application.Abstractions.Persistence;

namespace GastroGestion.Application.Dashboard.GetDashboard;

public sealed record GetDashboardQuery;

public sealed record EstadoCount(string Estado, int Cantidad);
public sealed record PlatoRanking(string Plato, int Cantidad);
public sealed record AlertaStock(string Ingrediente, decimal Balance, decimal StockMinimo);

public sealed record DashboardResult(
    int TotalPedidos,
    decimal MontoTotalPedidos,
    IReadOnlyList<EstadoCount> PedidosPorEstado,
    IReadOnlyList<PlatoRanking> TopPlatos,
    IReadOnlyList<AlertaStock> AlertasStock);

/// <summary>
/// Aggregates a handful of operational metrics for the dashboard from the existing repositories.
/// Read-only; computed in-memory over the current data (no dedicated reporting store).
/// </summary>
public sealed class GetDashboardHandler
{
    private readonly IPedidoRepository          _pedidos;
    private readonly IPlatoRepository           _platos;
    private readonly IIngredienteRepository     _ingredientes;
    private readonly IMovimientoStockRepository _stock;

    public GetDashboardHandler(
        IPedidoRepository pedidos,
        IPlatoRepository platos,
        IIngredienteRepository ingredientes,
        IMovimientoStockRepository stock)
    {
        _pedidos      = pedidos;
        _platos       = platos;
        _ingredientes = ingredientes;
        _stock        = stock;
    }

    public async Task<DashboardResult> Handle(GetDashboardQuery query, CancellationToken ct = default)
    {
        var pedidos = await _pedidos.SearchAsync(null, null, ct);
        var lineas  = pedidos.SelectMany(p => p.Lineas).ToList();

        var montoTotal = lineas.Sum(l => l.TotalLinea?.Monto ?? 0m);

        var porEstado = pedidos
            .GroupBy(p => p.Estado)
            .Select(g => new EstadoCount(g.Key.ToString(), g.Count()))
            .OrderByDescending(x => x.Cantidad)
            .ToList();

        var platoNombres = (await _platos.GetAllAsync(ct)).ToDictionary(p => p.Id, p => p.Nombre);
        var topPlatos = lineas
            .GroupBy(l => l.PlatoId)
            .Select(g => new PlatoRanking(
                platoNombres.TryGetValue(g.Key, out var n) ? n : "(plato)",
                g.Sum(l => l.Cantidad)))
            .OrderByDescending(x => x.Cantidad)
            .Take(5)
            .ToList();

        var ingredientes = await _ingredientes.GetAllAsync(ct);
        var balances     = await _stock.CalcularBalancesAsync(ct);
        var alertas = new List<AlertaStock>();
        foreach (var ing in ingredientes)
        {
            var balance = balances.GetValueOrDefault(ing.Id, 0m);
            if (balance <= ing.StockMinimo)
                alertas.Add(new AlertaStock(ing.Nombre, balance, ing.StockMinimo));
        }
        alertas = alertas.OrderBy(a => a.Balance).ToList();

        return new DashboardResult(pedidos.Count, montoTotal, porEstado, topPlatos, alertas);
    }
}
