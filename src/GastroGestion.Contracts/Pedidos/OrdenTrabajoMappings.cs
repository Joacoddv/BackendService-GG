using GastroGestion.Application.Pedidos.AsignarCocinero;
using GastroGestion.Application.Pedidos.GetOrdenesByEstado;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Pedidos;

namespace GastroGestion.Contracts.Pedidos;

public static class OrdenTrabajoMappings
{
    public static OrdenTrabajoBoardResponse ToResponse(this OrdenTrabajoBoardItem item)
        => new(item.OtId, item.PedidoId, item.PedidoTipo, item.PlatoId,
               item.LineaPedidoId, item.Estado, item.CocineroAsignadoLegajoId);

    public static OrdenTrabajoResponse ToResponse(this OrdenTrabajo ot, Guid pedidoId)
        => new(ot.Id, pedidoId, ot.PlatoId, ot.LineaPedidoId, ot.Estado,
               ot.CocineroAsignado?.Valor);

    public static AsignarCocineroCommand ToCommand(
        this AsignarCocineroRequest r, Guid pedidoId, Guid otId, RolUsuario rol)
        => new(pedidoId, otId, r.CocineroLegajoId, rol);
}
