using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;

namespace GastroGestion.Domain.Pedidos.Events;

/// <summary>
/// Raised when a new <see cref="Pedido"/> is created via <see cref="Pedido.Crear"/>.
/// Downstream handlers may use this for auditing, analytics, or saga initiation.
/// REQ-15.
/// </summary>
public sealed record PedidoCreado(
    Guid PedidoId,
    TipoPedido Tipo,
    Guid? ClienteId,
    DateTime OccurredOnUtc) : IDomainEvent;
