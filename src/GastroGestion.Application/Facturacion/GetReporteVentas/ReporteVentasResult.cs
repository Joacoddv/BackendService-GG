using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Facturacion.GetReporteVentas;

public sealed record ReporteTipoItem(TipoComprobante Tipo, int Cantidad, decimal Total);
public sealed record ReporteMetodoItem(MetodoPago Metodo, decimal Total);
public sealed record ReporteVentasResult(
    int CantidadFacturas,
    decimal TotalFacturado,
    decimal TotalCobrado,
    IReadOnlyList<ReporteTipoItem> PorTipo,
    IReadOnlyList<ReporteMetodoItem> PorMetodoPago);
