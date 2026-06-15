namespace GastroGestion.Application.Pedidos.AgregarLinea;

public sealed record AgregarLineaCommand(
    Guid PedidoId,
    Guid PlatoId,
    int Cantidad,
    string? Observaciones);
