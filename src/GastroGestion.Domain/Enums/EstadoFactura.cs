namespace GastroGestion.Domain.Enums;

/// <summary>
/// Lifecycle state for a <see cref="GastroGestion.Domain.Facturacion.Factura"/>.
/// Terminal states are <see cref="Pagada"/>, <see cref="Cancelada"/>, and <see cref="Anulada"/>.
/// </summary>
public enum EstadoFactura
{
    Creada    = 0,
    Pagada    = 1,
    Cancelada = 2,
    Anulada   = 3
}
