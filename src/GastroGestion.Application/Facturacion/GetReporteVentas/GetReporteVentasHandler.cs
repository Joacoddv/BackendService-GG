using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Facturacion.GetReporteVentas;

public sealed class GetReporteVentasHandler
{
    private readonly IFacturaRepository _facturas;

    public GetReporteVentasHandler(IFacturaRepository facturas) => _facturas = facturas;

    public async Task<ReporteVentasResult> Handle(GetReporteVentasQuery query, CancellationToken ct = default)
    {
        var facturas = (await _facturas.ListByFechaAltaAsync(query.Desde, query.Hasta, ct))
            .Where(f => f.Estado != EstadoFactura.Cancelada && f.Estado != EstadoFactura.Anulada)
            .ToList();

        var porTipo = facturas
            .GroupBy(f => f.TipoComprobante)
            .Select(g => new ReporteTipoItem(g.Key, g.Count(), g.Sum(f => f.Total.Monto)))
            .ToList();

        var porMetodo = facturas
            .SelectMany(f => f.Pagos)
            .GroupBy(p => p.MetodoPago)
            .Select(g => new ReporteMetodoItem(g.Key, g.Sum(p => p.Monto.Monto)))
            .ToList();

        return new ReporteVentasResult(
            facturas.Count,
            facturas.Sum(f => f.Total.Monto),
            facturas.Sum(f => f.TotalPagado.Monto),
            porTipo,
            porMetodo);
    }
}
