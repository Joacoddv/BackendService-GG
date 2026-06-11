namespace GastroGestion.Domain.Enums;

/// <summary>
/// TPH discriminator for <see cref="GastroGestion.Domain.Facturacion.Factura"/>.
/// The EF Core TPH mapping is configured in phase 3 infrastructure;
/// no discriminator attribute lives in the domain.
/// </summary>
public enum TipoComprobante
{
    TicketInterno      = 0,
    FacturaConIVA      = 1,
    FacturaElectronica = 2
}
