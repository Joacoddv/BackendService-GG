using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Services;

namespace GastroGestion.Application.Pedidos.ConfirmarPrecioLinea;

/// <summary>
/// Confirms the effective price snapshot for a single LineaPedido.
/// This handler exercises the W-01 async price service path on the live HTTP stack.
/// No .GetAwaiter().GetResult() or .Result is used — fully async throughout.
/// </summary>
public sealed class ConfirmarPrecioLineaHandler
{
    private readonly IPedidoRepository        _pedidos;
    private readonly IEfectivoPrecioService   _precios;
    private readonly IUnitOfWork              _uow;

    public ConfirmarPrecioLineaHandler(
        IPedidoRepository pedidos,
        IEfectivoPrecioService precios,
        IUnitOfWork uow)
    {
        _pedidos = pedidos;
        _precios = precios;
        _uow     = uow;
    }

    public async Task Handle(ConfirmarPrecioLineaCommand cmd, CancellationToken ct = default)
    {
        var pedido = await _pedidos.GetByIdAsync(cmd.PedidoId, ct)
            ?? throw new NotFoundException($"Pedido {cmd.PedidoId} not found.");

        var linea = pedido.Lineas.FirstOrDefault(l => l.Id == cmd.LineaId)
            ?? throw new NotFoundException($"LineaPedido {cmd.LineaId} not found.");

        // W-01: genuinely async, no blocking call — proves the fix works end-to-end on the HTTP stack
        var (precio, iva) = await _precios.ResolverPrecioEfectivoAsync(
            linea.PlatoId, DateOnly.FromDateTime(pedido.CreadoEnUtc), ct);

        linea.ConfirmarPrecio(precio, iva);

        await _uow.SaveChangesAsync(ct);
    }
}
