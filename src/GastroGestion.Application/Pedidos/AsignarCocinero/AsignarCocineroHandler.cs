using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Application.Pedidos.AsignarCocinero;

public sealed class AsignarCocineroHandler
{
    private readonly IPedidoRepository _pedidos;
    private readonly IUnitOfWork       _uow;

    public AsignarCocineroHandler(IPedidoRepository pedidos, IUnitOfWork uow)
    {
        _pedidos = pedidos;
        _uow     = uow;
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

        // TODO PR2: await _kitchenNotifier.NotifyOtChangedAsync(...)
    }
}
