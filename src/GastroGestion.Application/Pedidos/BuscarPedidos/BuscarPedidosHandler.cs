using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Pedidos;

namespace GastroGestion.Application.Pedidos.BuscarPedidos;

/// <summary>Lists orders (optionally filtered by state/type), newest first.</summary>
public sealed class BuscarPedidosHandler
{
    private readonly IPedidoRepository _pedidos;

    public BuscarPedidosHandler(IPedidoRepository pedidos) => _pedidos = pedidos;

    public async Task<IReadOnlyList<Pedido>> Handle(BuscarPedidosQuery query, CancellationToken ct = default)
        => await _pedidos.SearchAsync(query.Estado, query.Tipo, ct);
}
