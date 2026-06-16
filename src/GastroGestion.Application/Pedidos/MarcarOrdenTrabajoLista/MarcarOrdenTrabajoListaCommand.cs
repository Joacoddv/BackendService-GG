using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Pedidos.MarcarOrdenTrabajoLista;

public sealed record MarcarOrdenTrabajoListaCommand(Guid PedidoId, Guid OtId, RolUsuario Rol);
