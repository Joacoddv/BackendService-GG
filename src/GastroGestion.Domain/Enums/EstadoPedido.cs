namespace GastroGestion.Domain.Enums;

/// <summary>
/// Unified state set for both Salon and counter/delivery orders.
/// Valid transitions per order type are governed by <see cref="GastroGestion.Domain.Pedidos.PedidoTransicionRegistry"/>.
/// </summary>
/// <remarks>
/// Salon path:    Abierto → Cerrado | Cancelado
/// Counter path:  Creado → Modificado → Preparandose → ListoParaEntregar → Entregado | Cancelado
/// </remarks>
public enum EstadoPedido
{
    Abierto           = 0,
    Creado            = 1,
    Modificado        = 2,
    Preparandose      = 3,
    ListoParaEntregar = 4,
    Entregado         = 5,
    Cerrado           = 6,
    Cancelado         = 7
}
