using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Pedidos.TransicionarEstadoPedido;

public sealed record TransicionarEstadoPedidoCommand(
    Guid PedidoId,
    EstadoPedido EstadoNuevo,
    RolUsuario Rol);
