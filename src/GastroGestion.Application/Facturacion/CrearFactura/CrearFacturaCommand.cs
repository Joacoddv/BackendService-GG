namespace GastroGestion.Application.Facturacion.CrearFactura;

/// <summary>
/// Command to create a new Factura from one or more confirmed Pedidos.
/// All Pedidos must belong to the same ClienteId (REQ-13-G).
/// </summary>
public sealed record CrearFacturaCommand(
    Guid ClienteId,
    IReadOnlyList<Guid> PedidoIds,
    TipoComprobanteSolicitado Tipo);
