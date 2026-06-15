namespace GastroGestion.Application.Pedidos.ConfirmarPrecioLinea;

public sealed record ConfirmarPrecioLineaCommand(Guid PedidoId, Guid LineaId);
