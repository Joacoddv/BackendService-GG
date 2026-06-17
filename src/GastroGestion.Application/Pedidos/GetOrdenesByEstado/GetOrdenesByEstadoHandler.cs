using GastroGestion.Application.Abstractions.Persistence;

namespace GastroGestion.Application.Pedidos.GetOrdenesByEstado;

public sealed class GetOrdenesByEstadoHandler
{
    private readonly IPedidoRepository _pedidos;

    public GetOrdenesByEstadoHandler(IPedidoRepository pedidos) => _pedidos = pedidos;

    public async Task<IReadOnlyList<OrdenTrabajoBoardItem>> Handle(
        GetOrdenesByEstadoQuery query, CancellationToken ct = default)
        => await _pedidos.GetAllOrdenesTrabajoAsync(query.Estado, ct);
}
