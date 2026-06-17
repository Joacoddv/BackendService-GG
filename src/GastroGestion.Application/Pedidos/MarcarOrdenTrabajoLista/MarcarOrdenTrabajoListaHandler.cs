using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Abstractions.Realtime;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Application.Pedidos.GetOrdenesByEstado;
using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Pedidos.MarcarOrdenTrabajoLista;

public sealed class MarcarOrdenTrabajoListaHandler
{
    private readonly IPedidoRepository  _pedidos;
    private readonly IUnitOfWork        _uow;
    private readonly IKitchenNotifier   _kitchenNotifier;

    public MarcarOrdenTrabajoListaHandler(
        IPedidoRepository pedidos,
        IUnitOfWork uow,
        IKitchenNotifier kitchenNotifier)
    {
        _pedidos          = pedidos;
        _uow              = uow;
        _kitchenNotifier  = kitchenNotifier;
    }

    public async Task Handle(MarcarOrdenTrabajoListaCommand cmd, CancellationToken ct = default)
    {
        var pedido = await _pedidos.GetByIdAsync(cmd.PedidoId, ct)
            ?? throw new NotFoundException($"Pedido {cmd.PedidoId} not found.");

        // Role gate — only Cocinero and Administrador may mark an OT ready (OT-03-C)
        if (cmd.Rol is not (RolUsuario.Cocinero or RolUsuario.Administrador))
            throw new ForbiddenException(
                $"Role {cmd.Rol} is not allowed to mark an OT as ready. Required: Cocinero or Administrador.");

        // Delegate to aggregate root; domain auto-advances non-Salon Pedido if all OTs are Lista
        pedido.MarcarOrdenTrabajoLista(cmd.OtId, cmd.Rol);

        await _uow.SaveChangesAsync(ct);

        // Post-commit realtime push (OT-05, ADR-003)
        var ot = pedido.OrdenesTrabajo.First(o => o.Id == cmd.OtId);
        var boardItem = new OrdenTrabajoBoardItem(
            ot.Id,
            pedido.Id,
            pedido.Tipo,
            ot.PlatoId,
            ot.LineaPedidoId,
            ot.Estado,
            ot.CocineroAsignado?.Valor);
        await _kitchenNotifier.NotifyOtChangedAsync(boardItem, ct);
    }
}
