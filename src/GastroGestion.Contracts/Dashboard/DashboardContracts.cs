using GastroGestion.Application.Dashboard.GetDashboard;

namespace GastroGestion.Contracts.Dashboard;

public sealed record EstadoCountResponse(string Estado, int Cantidad);
public sealed record PlatoRankingResponse(string Plato, int Cantidad);
public sealed record AlertaStockResponse(string Ingrediente, decimal Balance, decimal StockMinimo);

public sealed record DashboardResponse(
    int TotalPedidos,
    decimal MontoTotalPedidos,
    IReadOnlyList<EstadoCountResponse> PedidosPorEstado,
    IReadOnlyList<PlatoRankingResponse> TopPlatos,
    IReadOnlyList<AlertaStockResponse> AlertasStock);

public static class DashboardMappings
{
    public static DashboardResponse ToResponse(this DashboardResult r)
        => new(
            r.TotalPedidos,
            r.MontoTotalPedidos,
            r.PedidosPorEstado.Select(e => new EstadoCountResponse(e.Estado, e.Cantidad)).ToList(),
            r.TopPlatos.Select(p => new PlatoRankingResponse(p.Plato, p.Cantidad)).ToList(),
            r.AlertasStock.Select(a => new AlertaStockResponse(a.Ingrediente, a.Balance, a.StockMinimo)).ToList());
}
