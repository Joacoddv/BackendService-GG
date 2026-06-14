using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Facturacion;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Services;

/// <summary>
/// Domain service contract for calculating invoice totals with IVA breakdown.
/// Implementation lives in the Application layer (phase 4) because it may require
/// configuration access for rounding rules.
/// <para>
/// Per-line IVA rule: <c>PrecioUnitario × Cantidad × Alicuota.Tasa</c>.<br/>
/// <see cref="TipoComprobante.TicketInterno"/> forces IVA = Cero on all lines (design §5e).
/// </para>
/// </summary>
public interface ICalculadorFactura
{
    /// <summary>
    /// Calculates the full invoice breakdown including IVA grouped by aliquot.
    /// </summary>
    /// <param name="lineas">Invoice lines to calculate from.</param>
    /// <param name="tipo">Comprobante type — TicketInterno forces zero IVA.</param>
    /// <returns>A <see cref="ResultadoFactura"/> with subtotals and IVA breakdown.</returns>
    ResultadoFactura Calcular(IReadOnlyList<FacturaLinea> lineas, TipoComprobante tipo);
}

/// <summary>
/// Result of a <see cref="ICalculadorFactura.Calcular"/> call.
/// All amounts are computed and immutable.
/// </summary>
/// <param name="SubTotal">Net amount before IVA.</param>
/// <param name="DesgloseIVA">Per-aliquot IVA breakdown.</param>
/// <param name="TotalIVA">Total IVA across all aliquots.</param>
/// <param name="Total">Grand total: SubTotal + TotalIVA.</param>
public sealed record ResultadoFactura(
    Dinero SubTotal,
    IReadOnlyList<DesglosIVA> DesgloseIVA,
    Dinero TotalIVA,
    Dinero Total);

/// <summary>
/// IVA breakdown for a single aliquot group within a <see cref="ResultadoFactura"/>.
/// </summary>
/// <param name="Alicuota">The IVA rate for this group.</param>
/// <param name="BaseImponible">Taxable base (net subtotal for this aliquot).</param>
/// <param name="MontoIVA">IVA amount for this group: BaseImponible × Alicuota.Tasa.</param>
public sealed record DesglosIVA(
    PorcentajeIVA Alicuota,
    Dinero BaseImponible,
    Dinero MontoIVA);
