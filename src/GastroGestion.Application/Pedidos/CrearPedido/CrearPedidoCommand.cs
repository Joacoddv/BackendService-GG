using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Pedidos.CrearPedido;

public sealed record CrearPedidoCommand(
    TipoPedido Tipo,
    Guid? MesaId,
    Guid? ClienteId,
    DireccionEntregaInput? DireccionEntrega,
    DateTime CreadoEnUtc);
