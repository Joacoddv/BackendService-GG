using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Facturacion;
using GastroGestion.Domain.Pedidos;

namespace GastroGestion.Application.Facturacion.CrearFactura;

/// <summary>
/// Handles <see cref="CrearFacturaCommand"/>.
/// Enforces REQ-13-G: all Pedidos in a single Factura must belong to the same ClienteId.
/// This is the Phase-2 deferred validation that cannot live inside the domain because
/// the domain factory cannot load aggregates.
/// </summary>
public sealed class CrearFacturaHandler
{
    private readonly IPedidoRepository  _pedidos;
    private readonly IFacturaRepository _facturas;
    private readonly IUnitOfWork        _uow;

    public CrearFacturaHandler(
        IPedidoRepository pedidos,
        IFacturaRepository facturas,
        IUnitOfWork uow)
    {
        _pedidos  = pedidos;
        _facturas = facturas;
        _uow      = uow;
    }

    /// <summary>
    /// Creates a new Factura from the specified Pedidos.
    /// </summary>
    /// <returns>The Id of the newly created Factura.</returns>
    /// <exception cref="ConflictException">
    /// Thrown when PedidoIds is empty, a Pedido is not found, or Pedidos belong to
    /// different clients (REQ-13-G).
    /// </exception>
    public async Task<Guid> Handle(CrearFacturaCommand cmd, CancellationToken ct = default)
    {
        if (cmd.PedidoIds is null || cmd.PedidoIds.Count == 0)
            throw new ConflictException("At least one Pedido is required to create a Factura.");

        var pedidos = await _pedidos.GetByIdsAsync(cmd.PedidoIds, ct);

        // REQ-13-G: all requested Pedidos must exist.
        if (pedidos.Count != cmd.PedidoIds.Count)
            throw new ConflictException("One or more Pedidos were not found.");

        // REQ-13-G: all Pedidos must belong to the same ClienteId.
        if (pedidos.Any(p => p.ClienteId != cmd.ClienteId))
            throw new ConflictException(
                "All Pedidos in a Factura must belong to the same ClienteId (REQ-13-G).");

        var lineas = BuildLineasFromPedidos(pedidos);

        var factura = cmd.Tipo switch
        {
            TipoComprobanteSolicitado.TicketInterno      => Factura.CrearTicket(cmd.ClienteId, cmd.PedidoIds.ToList(), lineas),
            TipoComprobanteSolicitado.FacturaConIVA       => Factura.CrearFacturaConIVA(cmd.ClienteId, cmd.PedidoIds.ToList(), lineas),
            TipoComprobanteSolicitado.FacturaElectronica  => Factura.CrearFacturaElectronica(cmd.ClienteId, cmd.PedidoIds.ToList(), lineas),
            _ => throw new ConflictException($"Unsupported comprobante type: {cmd.Tipo}.")
        };

        await _facturas.AddAsync(factura, ct);
        await _uow.SaveChangesAsync(ct); // commits + dispatches FacturaNecesitaCAE for electronic type

        return factura.Id;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static List<FacturaLinea> BuildLineasFromPedidos(IReadOnlyList<Pedido> pedidos)
    {
        var lineas = new List<FacturaLinea>();

        foreach (var pedido in pedidos)
        {
            foreach (var linea in pedido.Lineas)
            {
                // Only bill lines that have a confirmed price snapshot.
                if (linea.PrecioUnitario is null || linea.IVA is null)
                    continue;

                lineas.Add(new FacturaLinea(
                    Guid.NewGuid(),
                    linea.Id,
                    linea.PrecioUnitario,
                    linea.IVA,
                    linea.Cantidad));
            }
        }

        if (lineas.Count == 0)
            throw new ConflictException(
                "No confirmed LineaPedido found. All Pedido lines must have a confirmed price before billing.");

        return lineas;
    }
}
