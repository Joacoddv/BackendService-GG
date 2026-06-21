using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;

namespace GastroGestion.Application.Pedidos.QuitarLinea;

/// <summary>
/// Removes a line from an order. The aggregate enforces the edit-lock rules and, if the line had a
/// Creada OT, cancels it and raises the stock-restoration event.
/// </summary>
public sealed class QuitarLineaHandler
{
    private readonly IPedidoRepository _pedidos;
    private readonly IUnitOfWork       _uow;

    public QuitarLineaHandler(IPedidoRepository pedidos, IUnitOfWork uow)
    {
        _pedidos = pedidos;
        _uow     = uow;
    }

    public async Task Handle(QuitarLineaCommand cmd, CancellationToken ct = default)
    {
        var pedido = await _pedidos.GetByIdAsync(cmd.PedidoId, ct)
            ?? throw new NotFoundException($"Pedido {cmd.PedidoId} not found.");

        pedido.QuitarLinea(cmd.LineaId);

        await _uow.SaveChangesAsync(ct);
    }
}
