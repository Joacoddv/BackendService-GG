using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Pedidos.MarcarOrdenTrabajoLista;

public sealed class MarcarOrdenTrabajoListaHandler
{
    private readonly IPedidoRepository _pedidos;
    private readonly IUnitOfWork       _uow;

    public MarcarOrdenTrabajoListaHandler(IPedidoRepository pedidos, IUnitOfWork uow)
    {
        _pedidos = pedidos;
        _uow     = uow;
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

        // TODO PR2: await _kitchenNotifier.NotifyOtChangedAsync(...)
    }
}
