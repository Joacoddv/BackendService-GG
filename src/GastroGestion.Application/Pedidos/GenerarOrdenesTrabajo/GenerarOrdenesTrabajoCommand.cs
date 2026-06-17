using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Pedidos.GenerarOrdenesTrabajo;

public sealed record GenerarOrdenesTrabajoCommand(Guid PedidoId, RolUsuario Rol);
