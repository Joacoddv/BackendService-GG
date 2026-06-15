using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;

namespace GastroGestion.Application.Pedidos.AgregarLinea;

public sealed class AgregarLineaHandler
{
    private readonly IPedidoRepository _pedidos;
    private readonly IUnitOfWork       _uow;

    public AgregarLineaHandler(IPedidoRepository pedidos, IUnitOfWork uow)
    {
        _pedidos = pedidos;
        _uow     = uow;
    }

    public async Task<Guid> Handle(AgregarLineaCommand cmd, CancellationToken ct = default)
    {
        var pedido = await _pedidos.GetByIdAsync(cmd.PedidoId, ct)
            ?? throw new NotFoundException($"Pedido {cmd.PedidoId} not found.");

        var linea = pedido.AgregarLinea(cmd.PlatoId, cmd.Cantidad, cmd.Observaciones);

        await _uow.SaveChangesAsync(ct);

        return linea.Id;
    }
}
