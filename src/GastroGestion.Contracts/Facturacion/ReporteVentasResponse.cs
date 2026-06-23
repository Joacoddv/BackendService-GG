using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Facturacion;

public sealed record ReporteMetodoResponse(MetodoPago Metodo, decimal Total);
public sealed record ReporteTipoResponse(TipoComprobante Tipo, int Cantidad, decimal Total);
public sealed record ReporteVentasResponse(
    DateTime? Desde,
    DateTime? Hasta,
    int CantidadFacturas,
    decimal TotalFacturado,
    decimal TotalCobrado,
    IReadOnlyList<ReporteTipoResponse> PorTipo,
    IReadOnlyList<ReporteMetodoResponse> PorMetodoPago);
