using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Pedidos;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Application.Pedidos.CrearPedido;

public sealed class CrearPedidoHandler
{
    private readonly IPedidoRepository _pedidos;
    private readonly IMesaRepository   _mesas;
    private readonly IUnitOfWork       _uow;

    public CrearPedidoHandler(
        IPedidoRepository pedidos,
        IMesaRepository mesas,
        IUnitOfWork uow)
    {
        _pedidos = pedidos;
        _mesas   = mesas;
        _uow     = uow;
    }

    public async Task<Guid> Handle(CrearPedidoCommand cmd, CancellationToken ct = default)
    {
        DireccionEntrega? direccion = cmd.DireccionEntrega is null ? null : new DireccionEntrega(
            cmd.DireccionEntrega.Calle,
            cmd.DireccionEntrega.Numero,
            cmd.DireccionEntrega.Ciudad,
            cmd.DireccionEntrega.Provincia,
            cmd.DireccionEntrega.CodigoPostal,
            cmd.DireccionEntrega.Piso,
            cmd.DireccionEntrega.Departamento,
            cmd.DireccionEntrega.Zona);

        var pedido = Pedido.Crear(cmd.Tipo, cmd.MesaId, cmd.ClienteId, direccion, cmd.CreadoEnUtc);

        if (cmd.Tipo == TipoPedido.Salon)
        {
            var mesa = await _mesas.GetByIdAsync(cmd.MesaId!.Value, ct)
                ?? throw new NotFoundException($"Mesa {cmd.MesaId} not found.");
            mesa.AsignarPedido(pedido.Id);
        }

        await _pedidos.AddAsync(pedido, ct);
        await _uow.SaveChangesAsync(ct);

        return pedido.Id;
    }
}
