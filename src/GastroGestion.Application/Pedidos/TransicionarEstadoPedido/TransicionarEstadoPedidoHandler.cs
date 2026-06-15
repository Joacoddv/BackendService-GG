using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;

namespace GastroGestion.Application.Pedidos.TransicionarEstadoPedido;

public sealed class TransicionarEstadoPedidoHandler
{
    private readonly IPedidoRepository _pedidos;
    private readonly IUnitOfWork       _uow;

    public TransicionarEstadoPedidoHandler(IPedidoRepository pedidos, IUnitOfWork uow)
    {
        _pedidos = pedidos;
        _uow     = uow;
    }

    public async Task Handle(TransicionarEstadoPedidoCommand cmd, CancellationToken ct = default)
    {
        var pedido = await _pedidos.GetByIdAsync(cmd.PedidoId, ct)
            ?? throw new NotFoundException($"Pedido {cmd.PedidoId} not found.");

        // PHASE-5: replace body-supplied Rol with JWT claim
        pedido.TransicionarEstado(cmd.EstadoNuevo, cmd.Rol);

        await _uow.SaveChangesAsync(ct);
    }
}
