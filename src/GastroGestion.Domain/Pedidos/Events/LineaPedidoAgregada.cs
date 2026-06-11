using GastroGestion.Domain.Common;

namespace GastroGestion.Domain.Pedidos.Events;

/// <summary>
/// Raised when a <see cref="LineaPedido"/> is added to a <see cref="Pedido"/> via
/// <see cref="Pedido.AgregarLinea"/>. Downstream handlers may use this for
/// real-time kitchen display updates or analytics.
/// REQ-15.
/// </summary>
public sealed record LineaPedidoAgregada(
    Guid PedidoId,
    Guid LineaPedidoId,
    Guid PlatoId,
    int Cantidad,
    DateTime OccurredOnUtc) : IDomainEvent;
