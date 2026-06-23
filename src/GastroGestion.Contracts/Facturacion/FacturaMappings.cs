using GastroGestion.Application.Facturacion.AnularFactura;
using GastroGestion.Application.Facturacion.CrearFactura;
using GastroGestion.Application.Facturacion.RegistrarPago;
using GastroGestion.Domain.Facturacion;

namespace GastroGestion.Contracts.Facturacion;

public static class FacturaMappings
{
    public static CrearFacturaCommand ToCommand(this CrearFacturaRequest request)
        => new(request.ClienteId, request.PedidoIds.ToList().AsReadOnly(), request.Tipo);

    public static RegistrarPagoCommand ToCommand(this RegistrarPagoRequest request, Guid facturaId)
        => new(facturaId, request.Monto, request.MetodoPago);

    public static AnularFacturaCommand ToCommand(this AnularFacturaRequest request, Guid facturaId)
        => new(facturaId, request.Motivo);

    public static FacturaResumenResponse ToResumenResponse(this Factura factura)
        => new(
            factura.Id,
            factura.TipoComprobante,
            factura.Estado,
            factura.ClienteId,
            factura.FechaAlta,
            factura.Total.Monto,
            factura.TotalPagado.Monto,
            factura.EstaPagada);

    public static FacturaResponse ToResponse(this Factura factura)
        => new(
            factura.Id,
            factura.TipoComprobante,
            factura.Estado,
            factura.ClienteId,
            factura.FechaAlta,
            factura.SubTotal.Monto,
            factura.TotalIVA.Monto,
            factura.Total.Monto,
            factura.TotalPagado.Monto,
            factura.EstaPagada,
            factura.CAE,
            factura.VencimientoCAE,
            factura.Lineas.Select(l => new FacturaLineaResponse(
                l.Id,
                l.LineaPedidoId,
                l.Cantidad,
                l.PrecioUnitario.Monto,
                l.PrecioUnitario.Moneda.ToString(),
                l.IVA.Tasa)).ToList().AsReadOnly(),
            factura.Pagos.Select(p => new PagoResponse(
                p.Id,
                p.Monto.Monto,
                p.MetodoPago,
                p.FechaPago)).ToList().AsReadOnly(),
            factura.MotivoAnulacion,
            factura.FechaAnulacion);
}
