using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Pedidos.BuscarPedidos;

/// <summary>Query for GET /pedidos — optional filters by state and type.</summary>
public sealed record BuscarPedidosQuery(EstadoPedido? Estado, TipoPedido? Tipo);
