using GastroGestion.Domain.Common;

namespace GastroGestion.Domain.Pedidos.Events;

/// <summary>
/// Raised when an <see cref="OrdenTrabajo"/> is generated for a <see cref="Pedido"/>.
/// Carries the recipe snapshot so the stock handler can RESERVE the ingredients without
/// reloading the aggregate.
/// </summary>
public sealed record OrdenTrabajoCreada(
    Guid PedidoId,
    Guid OrdenTrabajoId,
    Guid PlatoId,
    IReadOnlyList<LineaRecetaSnapshot> RecetaSnapshot,
    DateTime OccurredOnUtc) : IDomainEvent;
