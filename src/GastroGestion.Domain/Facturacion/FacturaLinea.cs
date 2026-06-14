using GastroGestion.Domain.Common;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Facturacion;

/// <summary>
/// An invoice line owned by a <see cref="Factura"/>. Captures a 1:1 snapshot
/// of a <c>LineaPedido</c> at billing time — price is never reread from the catalogue.
/// <para>
/// Totals are always computed; never stored.
/// </para>
/// </summary>
public class FacturaLinea : Entity
{
    /// <summary>Cross-boundary reference to the originating LineaPedido.</summary>
    public Guid LineaPedidoId { get; private set; }

    /// <summary>Unit price snapshot at billing time.</summary>
    public Dinero PrecioUnitario { get; private set; }

    /// <summary>IVA rate applicable to this line.</summary>
    public PorcentajeIVA IVA { get; private set; }

    /// <summary>Quantity billed for this line (must be &gt; 0).</summary>
    public int Cantidad { get; private set; }

    // ── Computed totals (never stored) ────────────────────────────────────────

    /// <summary>Net subtotal: PrecioUnitario × Cantidad.</summary>
    public Dinero Subtotal => PrecioUnitario.Multiplicar(Cantidad);

    /// <summary>Gross subtotal with IVA: Subtotal + IVA amount.</summary>
    public Dinero SubtotalConIVA => Subtotal.ConIVA(IVA);

    // EF Core parameterless ctor.
#pragma warning disable CS8618
    protected FacturaLinea() { }
#pragma warning restore CS8618

    public FacturaLinea(Guid id, Guid lineaPedidoId, Dinero precioUnitario, PorcentajeIVA iva, int cantidad)
        : base(id)
    {
        if (lineaPedidoId == Guid.Empty)
            throw new DomainException("FacturaLinea.LineaPedidoId cannot be empty.");
        if (precioUnitario is null)
            throw new DomainException("FacturaLinea.PrecioUnitario cannot be null.");
        if (iva is null)
            throw new DomainException("FacturaLinea.IVA cannot be null.");
        if (cantidad <= 0)
            throw new DomainException($"FacturaLinea.Cantidad must be greater than zero. Received: {cantidad}.");

        LineaPedidoId = lineaPedidoId;
        PrecioUnitario = precioUnitario;
        IVA            = iva;
        Cantidad       = cantidad;
    }
}
