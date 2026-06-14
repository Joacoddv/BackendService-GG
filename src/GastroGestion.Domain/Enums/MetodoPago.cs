namespace GastroGestion.Domain.Enums;

/// <summary>
/// Payment method used when registering a <see cref="GastroGestion.Domain.Facturacion.Pago"/>
/// against a <see cref="GastroGestion.Domain.Facturacion.Factura"/>.
/// </summary>
public enum MetodoPago
{
    Efectivo        = 0,
    TarjetaDebito   = 1,
    TarjetaCredito  = 2,
    Transferencia   = 3,
    ContraEntrega   = 4
}
