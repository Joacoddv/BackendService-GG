using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Facturacion.RegistrarPago;

public sealed record RegistrarPagoCommand(
    Guid FacturaId,
    decimal Monto,
    MetodoPago MetodoPago);
