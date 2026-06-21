namespace GastroGestion.Application.Pedidos.QuitarLinea;

/// <summary>Command for DELETE /pedidos/{id}/lineas/{lineaId} — remove a line from the order.</summary>
public sealed record QuitarLineaCommand(Guid PedidoId, Guid LineaId);
