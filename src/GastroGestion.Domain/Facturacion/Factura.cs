using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Facturacion.Events;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Facturacion;

/// <summary>
/// Aggregate root for a billing document (Comprobante). Implements the TPH
/// (Table-Per-Hierarchy) polymorphic pattern via <see cref="TipoComprobante"/>
/// discriminator; EF Core TPH mapping is configured in phase 3 (no attributes here).
/// <para>
/// <b>Three factory methods enforce type invariants:</b><br/>
/// - <see cref="CrearTicket"/>: internal ticket, IVA forced to zero on all lines.<br/>
/// - <see cref="CrearFacturaConIVA"/>: standard VAT invoice, no CAE.<br/>
/// - <see cref="CrearFacturaElectronica"/>: AFIP e-invoice, raises <see cref="FacturaNecesitaCAE"/>
///   event; CAE assigned later via <see cref="AsignarCae"/>.<br/>
/// </para>
/// <para>
/// <b>Payment:</b> multi-payment via <see cref="RegistrarPago"/>. Becomes
/// <see cref="EstadoFactura.Pagada"/> automatically when total paid ≥ Total.
/// </para>
/// <para>
/// <b>Cancellation:</b> only from <see cref="EstadoFactura.Creada"/>; throws from
/// Pagada or Cancelada.
/// </para>
/// </summary>
public class Factura : AggregateRoot
{
    private readonly List<FacturaLinea> _lineas = [];
    private readonly List<Pago>         _pagos  = [];
    private readonly List<Guid>         _pedidosFacturados = [];

    // ── Identity and classification ───────────────────────────────────────────

    /// <summary>TPH discriminator — domain-only; no EF attribute (design §5a).</summary>
    public TipoComprobante TipoComprobante { get; private set; }

    /// <summary>Current lifecycle state of the invoice.</summary>
    public EstadoFactura Estado { get; private set; }

    /// <summary>Cross-boundary reference to the billing client.</summary>
    public Guid ClienteId { get; private set; }

    /// <summary>UTC timestamp when the invoice was created.</summary>
    public DateTime FechaAlta { get; private set; }

    /// <summary>
    /// AFIP/ARCA Electronic Authorization Code. Only valid for
    /// <see cref="TipoComprobante.FacturaElectronica"/>; null until assigned.
    /// </summary>
    public string? CAE { get; private set; }

    /// <summary>
    /// Expiry date of the assigned CAE. Only set alongside <see cref="CAE"/>.
    /// </summary>
    public DateOnly? VencimientoCAE { get; private set; }

    // ── Collections ───────────────────────────────────────────────────────────

    public IReadOnlyList<FacturaLinea> Lineas            => _lineas.AsReadOnly();
    public IReadOnlyList<Pago>         Pagos             => _pagos.AsReadOnly();
    public IReadOnlyList<Guid>         PedidosFacturados => _pedidosFacturados.AsReadOnly();

    // ── Computed totals (never stored) ────────────────────────────────────────

    /// <summary>Net subtotal: sum of each line's Subtotal.</summary>
    public Dinero SubTotal =>
        _lineas.Aggregate(new Dinero(0m), (acc, l) => acc.Sumar(l.Subtotal));

    /// <summary>Total IVA amount: sum of (SubtotalConIVA - Subtotal) per line.</summary>
    public Dinero TotalIVA =>
        _lineas.Aggregate(new Dinero(0m), (acc, l) => acc.Sumar(l.SubtotalConIVA.Restar(l.Subtotal)));

    /// <summary>Grand total: SubTotal + TotalIVA.</summary>
    public Dinero Total => SubTotal.Sumar(TotalIVA);

    /// <summary>Sum of all registered payments.</summary>
    public Dinero TotalPagado =>
        _pagos.Aggregate(new Dinero(0m), (acc, p) => acc.Sumar(p.Monto));

    /// <summary>True when total paid amount has reached or exceeded Total.</summary>
    public bool EstaPagada => TotalPagado.Monto >= Total.Monto;

    // ── EF Core / private ─────────────────────────────────────────────────────

#pragma warning disable CS8618
    private Factura() { }
#pragma warning restore CS8618

    private Factura(
        Guid id,
        TipoComprobante tipoComprobante,
        Guid clienteId,
        List<Guid> pedidos,
        List<FacturaLinea> lineas,
        DateTime fechaAlta) : base(id)
    {
        TipoComprobante = tipoComprobante;
        ClienteId       = clienteId;
        Estado          = EstadoFactura.Creada;
        FechaAlta       = fechaAlta;

        _pedidosFacturados.AddRange(pedidos);
        _lineas.AddRange(lineas);
    }

    // ── Factories ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates an internal ticket (no IVA). All lines are forced to
    /// <see cref="PorcentajeIVA.Cero"/> regardless of the passed IVA values.
    /// CAE is not applicable and must remain null.
    /// </summary>
    public static Factura CrearTicket(
        Guid clienteId,
        List<Guid> pedidos,
        List<FacturaLinea> lineas)
    {
        ValidarArgumentosFactura(clienteId, pedidos, lineas);

        // Force all lines to zero IVA (ticket invariant).
        var lineasSinIVA = lineas
            .Select(l => new FacturaLinea(l.Id, l.LineaPedidoId, l.PrecioUnitario, PorcentajeIVA.Cero, l.Cantidad))
            .ToList();

        return new Factura(Guid.NewGuid(), TipoComprobante.TicketInterno, clienteId, pedidos, lineasSinIVA, DateTime.UtcNow);
    }

    /// <summary>
    /// Creates a standard VAT invoice. IVA rates from lines are preserved.
    /// CAE is not applicable and must remain null.
    /// </summary>
    public static Factura CrearFacturaConIVA(
        Guid clienteId,
        List<Guid> pedidos,
        List<FacturaLinea> lineas)
    {
        ValidarArgumentosFactura(clienteId, pedidos, lineas);

        return new Factura(Guid.NewGuid(), TipoComprobante.FacturaConIVA, clienteId, pedidos, lineas, DateTime.UtcNow);
    }

    /// <summary>
    /// Creates an AFIP/ARCA electronic invoice. CAE starts null and must be
    /// assigned via <see cref="AsignarCae"/> after the AFIP web-service responds.
    /// Raises <see cref="FacturaNecesitaCAE"/> (REQ-15, Scenario 13-B).
    /// </summary>
    public static Factura CrearFacturaElectronica(
        Guid clienteId,
        List<Guid> pedidos,
        List<FacturaLinea> lineas)
    {
        ValidarArgumentosFactura(clienteId, pedidos, lineas);

        var factura = new Factura(Guid.NewGuid(), TipoComprobante.FacturaElectronica, clienteId, pedidos, lineas, DateTime.UtcNow);
        factura.AddDomainEvent(new FacturaNecesitaCAE(factura.Id, clienteId, factura.Total, DateTime.UtcNow));
        return factura;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Assigns the AFIP/ARCA CAE to this electronic invoice. Set-once guard prevents
    /// overwrite. Only valid for <see cref="TipoComprobante.FacturaElectronica"/>.
    /// </summary>
    /// <param name="cae">The 14-digit AFIP authorization code.</param>
    /// <param name="vencimiento">Expiry date returned by AFIP.</param>
    /// <exception cref="DomainException">
    /// Thrown when called on a non-electronic invoice, or when CAE is already set.
    /// </exception>
    public void AsignarCae(string cae, DateOnly vencimiento)
    {
        if (TipoComprobante != TipoComprobante.FacturaElectronica)
            throw new DomainException(
                $"CAE can only be assigned to FacturaElectronica. This invoice is {TipoComprobante}.");
        if (CAE is not null)
            throw new DomainException(
                $"CAE is already assigned to this Factura ({CAE}). AsignarCae is set-once.");
        if (string.IsNullOrWhiteSpace(cae))
            throw new DomainException("CAE value cannot be empty.");

        CAE            = cae;
        VencimientoCAE = vencimiento;
    }

    /// <summary>
    /// Registers a payment against this invoice. Allowed only while the invoice
    /// is in state <see cref="EstadoFactura.Creada"/>.
    /// Automatically transitions to <see cref="EstadoFactura.Pagada"/> when
    /// total paid amount reaches or exceeds <see cref="Total"/>.
    /// </summary>
    public void RegistrarPago(Dinero monto, MetodoPago metodoPago, DateTime fechaPago)
    {
        if (Estado == EstadoFactura.Cancelada)
            throw new DomainException("Cannot register a payment on a cancelled Factura.");
        if (Estado == EstadoFactura.Pagada)
            throw new DomainException("Factura is already fully paid.");

        var pago = new Pago(Guid.NewGuid(), monto, metodoPago, fechaPago);
        _pagos.Add(pago);

        if (EstaPagada)
            Estado = EstadoFactura.Pagada;
    }

    /// <summary>
    /// Cancels the invoice. Only valid from <see cref="EstadoFactura.Creada"/>.
    /// </summary>
    /// <exception cref="DomainException">
    /// Thrown when attempting to cancel a paid or already-cancelled invoice.
    /// </exception>
    public void Cancelar()
    {
        if (Estado == EstadoFactura.Pagada)
            throw new DomainException("Cannot cancel a Factura that has already been paid.");
        if (Estado == EstadoFactura.Cancelada)
            throw new DomainException("Factura is already cancelled.");

        Estado = EstadoFactura.Cancelada;
    }

    /// <summary>
    /// Returns whether this invoice can be grouped with <paramref name="otra"/>
    /// for billing purposes (same client, same document type).
    /// Same-client grouping seam per REQ-13.
    /// </summary>
    public bool PuedeCombinarseConFactura(Factura otra)
    {
        if (otra is null)
            throw new DomainException("Cannot compare with a null Factura.");
        return ClienteId == otra.ClienteId && TipoComprobante == otra.TipoComprobante;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static void ValidarArgumentosFactura(
        Guid clienteId,
        List<Guid> pedidos,
        List<FacturaLinea> lineas)
    {
        if (clienteId == Guid.Empty)
            throw new DomainException("Factura.ClienteId cannot be empty.");
        if (pedidos is null || pedidos.Count == 0)
            throw new DomainException("At least one PedidoId must be associated with the Factura.");
        if (lineas is null || lineas.Count == 0)
            throw new DomainException("At least one FacturaLinea must be provided.");
    }
}
