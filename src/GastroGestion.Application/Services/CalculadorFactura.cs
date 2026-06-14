using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Facturacion;
using GastroGestion.Domain.Services;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Application.Services;

/// <summary>
/// Pure computation implementation of <see cref="ICalculadorFactura"/>.
/// Groups invoice lines by IVA aliquot, computes per-group IVA amounts,
/// and returns a <see cref="ResultadoFactura"/> with the full breakdown.
/// No repository dependencies.
/// </summary>
internal sealed class CalculadorFactura : ICalculadorFactura
{
    /// <inheritdoc />
    public ResultadoFactura Calcular(IReadOnlyList<FacturaLinea> lineas, TipoComprobante tipo)
    {
        ArgumentNullException.ThrowIfNull(lineas);

        // TicketInterno forces all lines to zero IVA (matches Factura.CrearTicket invariant).
        var lineasEfectivas = tipo == TipoComprobante.TicketInterno
            ? lineas.Select(l => new FacturaLinea(l.Id, l.LineaPedidoId, l.PrecioUnitario, PorcentajeIVA.Cero, l.Cantidad)).ToList()
            : lineas.ToList();

        var subTotal = lineasEfectivas.Aggregate(new Dinero(0m), (acc, l) => acc.Sumar(l.Subtotal));

        var desgloseIVA = lineasEfectivas
            .GroupBy(l => l.IVA.Alicuota)
            .Select(g =>
            {
                var alicuota   = new PorcentajeIVA(g.Key);
                var baseImponible = g.Aggregate(new Dinero(0m), (acc, l) => acc.Sumar(l.Subtotal));
                var montoIVA   = g.Aggregate(new Dinero(0m), (acc, l) => acc.Sumar(l.SubtotalConIVA.Restar(l.Subtotal)));
                return new DesglosIVA(alicuota, baseImponible, montoIVA);
            })
            .ToList();

        var totalIVA = desgloseIVA.Aggregate(new Dinero(0m), (acc, d) => acc.Sumar(d.MontoIVA));
        var total    = subTotal.Sumar(totalIVA);

        return new ResultadoFactura(subTotal, desgloseIVA.AsReadOnly(), totalIVA, total);
    }
}
