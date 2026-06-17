using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Pedidos.AsignarCocinero;

public sealed record AsignarCocineroCommand(
    Guid       PedidoId,
    Guid       OtId,
    Guid       CocineroLegajoId,
    RolUsuario Rol);
