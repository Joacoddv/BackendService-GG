namespace GastroGestion.Application.Pedidos.GenerarOrdenTrabajoLinea;

/// <summary>Command for POST /pedidos/{id}/lineas/{lineaId}/orden-trabajo — generate one line's OT.</summary>
public sealed record GenerarOrdenTrabajoLineaCommand(Guid PedidoId, Guid LineaId);
