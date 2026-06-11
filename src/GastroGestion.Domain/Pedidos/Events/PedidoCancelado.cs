using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;

namespace GastroGestion.Domain.Pedidos.Events;

/// <summary>
/// Raised when a <see cref="Pedido"/> is fully cancelled.
/// Downstream handlers cancel all owned OTs and emit stock-restoration events
/// for eligible OTs (state Creada). OTs in Preparandose or Lista are consumed —
/// no stock restoration occurs for them.
/// </summary>
public sealed record PedidoCancelado(
    Guid PedidoId,
    EstadoPedido EstadoAnterior,
    RolUsuario RolQueCancel,
    DateTime OccurredOnUtc) : IDomainEvent;
