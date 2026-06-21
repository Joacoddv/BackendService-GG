using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;

namespace GastroGestion.Application.Pedidos.ActualizarLinea;

/// <summary>
/// Updates an existing line's quantity and observations. The aggregate enforces the edit-lock
/// rules (order not terminal; the line's OT still Creada) and recomputes the line totals from
/// the already-confirmed unit price.
/// </summary>
public sealed class ActualizarLineaHandler
{
    private readonly IPedidoRepository _pedidos;
    private readonly IUnitOfWork       _uow;

    public ActualizarLineaHandler(IPedidoRepository pedidos, IUnitOfWork uow)
    {
        _pedidos = pedidos;
        _uow     = uow;
    }

    public async Task Handle(ActualizarLineaCommand cmd, CancellationToken ct = default)
    {
        var pedido = await _pedidos.GetByIdAsync(cmd.PedidoId, ct)
            ?? throw new NotFoundException($"Pedido {cmd.PedidoId} not found.");

        pedido.ActualizarLinea(cmd.LineaId, cmd.Cantidad, cmd.Observaciones);

        await _uow.SaveChangesAsync(ct);
    }
}
