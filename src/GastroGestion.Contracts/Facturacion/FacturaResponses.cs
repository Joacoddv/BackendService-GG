using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Facturacion;

public sealed record FacturaResponse(
    Guid Id,
    TipoComprobante TipoComprobante,
    EstadoFactura Estado,
    Guid ClienteId,
    DateTime FechaAlta,
    decimal SubTotal,
    decimal TotalIVA,
    decimal Total,
    decimal TotalPagado,
    bool EstaPagada,
    string? CAE,
    DateOnly? VencimientoCAE,
    IReadOnlyList<FacturaLineaResponse> Lineas,
    IReadOnlyList<PagoResponse> Pagos);

public sealed record FacturaLineaResponse(
    Guid Id,
    Guid LineaPedidoId,
    int Cantidad,
    decimal PrecioUnitario,
    string Moneda,
    decimal IvaTasa);

public sealed record PagoResponse(
    Guid Id,
    decimal Monto,
    MetodoPago MetodoPago,
    DateTime FechaPago);
