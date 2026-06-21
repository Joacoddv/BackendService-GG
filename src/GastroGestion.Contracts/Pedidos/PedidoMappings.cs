using GastroGestion.Application.Pedidos.ActualizarLinea;
using GastroGestion.Application.Pedidos.AgregarLinea;
using GastroGestion.Application.Pedidos.CrearPedido;
using GastroGestion.Application.Pedidos.TransicionarEstadoPedido;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Pedidos;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Contracts.Pedidos;

public static class PedidoMappings
{
    public static CrearPedidoCommand ToCommand(this CrearPedidoRequest request)
        => new(
            request.Tipo,
            request.MesaId,
            request.ClienteId,
            request.DireccionEntrega is null ? null : new DireccionEntregaInput(
                request.DireccionEntrega.Calle,
                request.DireccionEntrega.Numero,
                request.DireccionEntrega.Ciudad,
                request.DireccionEntrega.Provincia,
                request.DireccionEntrega.CodigoPostal,
                request.DireccionEntrega.Piso,
                request.DireccionEntrega.Departamento),
            DateTime.UtcNow);

    public static AgregarLineaCommand ToCommand(this AgregarLineaRequest request, Guid pedidoId)
        => new(pedidoId, request.PlatoId, request.Cantidad, request.Observaciones);

    public static ActualizarLineaCommand ToCommand(this ActualizarLineaRequest request, Guid pedidoId, Guid lineaId)
        => new(pedidoId, lineaId, request.Cantidad, request.Observaciones);

    public static TransicionarEstadoPedidoCommand ToCommand(this TransicionarEstadoRequest request, Guid pedidoId, RolUsuario rol)
        => new(pedidoId, request.EstadoNuevo, rol);

    public static PedidoResponse ToResponse(this Pedido pedido)
        => new(
            pedido.Id,
            pedido.Tipo,
            pedido.Estado,
            pedido.MesaId,
            pedido.ClienteId,
            pedido.DireccionEntrega is null ? null : new DireccionEntregaResponse(
                pedido.DireccionEntrega.Calle,
                pedido.DireccionEntrega.Numero,
                pedido.DireccionEntrega.Ciudad,
                pedido.DireccionEntrega.Provincia,
                pedido.DireccionEntrega.CodigoPostal,
                pedido.DireccionEntrega.Piso,
                pedido.DireccionEntrega.Departamento),
            pedido.CreadoEnUtc,
            pedido.Lineas.Select(l => l.ToResponse()).ToList().AsReadOnly());

    public static LineaPedidoResponse ToResponse(this LineaPedido linea)
        => new(
            linea.Id,
            linea.PlatoId,
            linea.Cantidad,
            linea.Observaciones,
            linea.PrecioUnitario?.Monto,
            linea.PrecioUnitario?.Moneda.ToString(),
            linea.IVA?.Tasa,
            linea.SubtotalLinea?.Monto,
            linea.TotalLinea?.Monto);
}
