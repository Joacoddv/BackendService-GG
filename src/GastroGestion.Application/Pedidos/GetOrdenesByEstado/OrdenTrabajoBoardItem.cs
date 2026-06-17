using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Pedidos;

namespace GastroGestion.Application.Pedidos.GetOrdenesByEstado;

/// <summary>
/// Flat read-model projection of a single <see cref="OrdenTrabajo"/> for the kitchen board.
/// Never materializes a full <see cref="GastroGestion.Domain.Pedidos.Pedido"/> aggregate (ADR-002).
/// </summary>
public sealed record OrdenTrabajoBoardItem(
    Guid       OtId,
    Guid       PedidoId,
    TipoPedido PedidoTipo,
    Guid       PlatoId,
    Guid       LineaPedidoId,
    EstadoOT   Estado,
    Guid?      CocineroAsignadoLegajoId);
