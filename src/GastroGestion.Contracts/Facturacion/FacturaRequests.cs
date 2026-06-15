using GastroGestion.Application.Facturacion.CrearFactura;
using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Facturacion;

public sealed record CrearFacturaRequest(
    Guid ClienteId,
    Guid[] PedidoIds,
    TipoComprobanteSolicitado Tipo);

public sealed record RegistrarPagoRequest(
    decimal Monto,
    MetodoPago MetodoPago);
