namespace GastroGestion.Domain.Enums;

/// <summary>
/// Classifies a <see cref="GastroGestion.Domain.Pedidos.Pedido"/> by its service channel.
/// TakeAway uses the same state machine as Mostrador (counter service).
/// </summary>
public enum TipoPedido
{
    Salon    = 0,
    TakeAway = 1,
    Delivery = 2
}
