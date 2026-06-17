using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Abstractions.Realtime;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Application.Pedidos.GetOrdenesByEstado;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Application.Pedidos.AsignarCocinero;

public sealed class AsignarCocineroHandler
{
    private readonly IPedidoRepository  _pedidos;
    private readonly IUnitOfWork        _uow;
    private readonly IKitchenNotifier   _kitchenNotifier;

    public AsignarCocineroHandler(
        IPedidoRepository pedidos,
        IUnitOfWork uow,
        IKitchenNotifier kitchenNotifier)
    {
        _pedidos          = pedidos;
        _uow              = uow;
        _kitchenNotifier  = kitchenNotifier;
    }

    public async Task Handle(AsignarCocineroCommand cmd, CancellationToken ct = default)
    {
        var pedido = await _pedidos.GetByIdAsync(cmd.PedidoId, ct)
            ?? throw new NotFoundException($"Pedido {cmd.PedidoId} not found.");

        // Role gate — only Cocinero and Administrador may assign a cook (OT-02-B)
        if (cmd.Rol is not (RolUsuario.Cocinero or RolUsuario.Administrador))
            throw new ForbiddenException(
                $"Role {cmd.Rol} is not allowed to assign a cook. Required: Cocinero or Administrador.");

        // Delegate to aggregate root; domain validates OT state and throws DomainException on violation
        pedido.AsignarCocineroAOT(cmd.OtId, new LegajoId(cmd.CocineroLegajoId), cmd.Rol);

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
