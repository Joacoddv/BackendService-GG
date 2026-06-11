using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;

namespace GastroGestion.Domain.Pedidos.Events;

/// <summary>
/// Raised when a <see cref="Pedido"/> transitions to a new state via
/// <see cref="Pedido.TransicionarEstado"/>. Infra layer dispatches after persistence.
/// </summary>
public sealed record PedidoEstadoCambiado(
    Guid PedidoId,
    EstadoPedido EstadoAnterior,
    EstadoPedido EstadoNuevo,
    RolUsuario RolQueTransiciono,
    DateTime OccurredOnUtc) : IDomainEvent;
