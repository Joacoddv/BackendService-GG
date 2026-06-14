namespace GastroGestion.Application.Facturacion.CrearFactura;

/// <summary>
/// Application-layer input mirror of <c>TipoComprobante</c>.
/// Decouples the API/consumer from the domain enum; maps 1:1 in this phase.
/// </summary>
public enum TipoComprobanteSolicitado
{
    TicketInterno,
    FacturaConIVA,
    FacturaElectronica
}
