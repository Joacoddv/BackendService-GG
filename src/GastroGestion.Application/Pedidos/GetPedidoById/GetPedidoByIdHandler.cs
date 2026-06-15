using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Pedidos;

namespace GastroGestion.Application.Pedidos.GetPedidoById;

public sealed class GetPedidoByIdHandler
{
    private readonly IPedidoRepository _pedidos;

    public GetPedidoByIdHandler(IPedidoRepository pedidos) => _pedidos = pedidos;

    public async Task<Pedido?> Handle(GetPedidoByIdQuery query, CancellationToken ct = default)
        => await _pedidos.GetByIdAsync(query.Id, ct);
}
