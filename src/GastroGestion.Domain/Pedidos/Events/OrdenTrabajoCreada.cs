using GastroGestion.Domain.Common;

namespace GastroGestion.Domain.Pedidos.Events;

/// <summary>
/// Raised when a batch of <see cref="OrdenTrabajo"/> records is generated for a
/// <see cref="Pedido"/>. The infra layer uses this to trigger stock-consumption moves.
/// </summary>
public sealed record OrdenTrabajoCreada(
    Guid PedidoId,
    Guid OrdenTrabajoId,
    Guid PlatoId,
    DateTime OccurredOnUtc) : IDomainEvent;
