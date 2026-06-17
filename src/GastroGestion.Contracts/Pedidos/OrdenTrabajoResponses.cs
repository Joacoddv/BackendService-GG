using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Pedidos;

namespace GastroGestion.Contracts.Pedidos;

/// <summary>Single OT mutation response — returned after asignar-cocinero and marcar-lista.</summary>
public sealed record OrdenTrabajoResponse(
    Guid     Id,
    Guid     PedidoId,
    Guid     PlatoId,
    Guid     LineaPedidoId,
    EstadoOT Estado,
    Guid?    CocineroAsignadoLegajoId);

/// <summary>Flat board item — returned by GET /ordenes-trabajo.</summary>
public sealed record OrdenTrabajoBoardResponse(
    Guid       OtId,
    Guid       PedidoId,
    TipoPedido PedidoTipo,
    Guid       PlatoId,
    Guid       LineaPedidoId,
    EstadoOT   Estado,
    Guid?      CocineroAsignadoLegajoId);
