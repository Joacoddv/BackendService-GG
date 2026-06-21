namespace GastroGestion.Application.Pedidos.ActualizarLinea;

/// <summary>Command for PUT /pedidos/{id}/lineas/{lineaId} — edit an existing line's quantity/notes.</summary>
public sealed record ActualizarLineaCommand(
    Guid PedidoId,
    Guid LineaId,
    int Cantidad,
    string? Observaciones);
