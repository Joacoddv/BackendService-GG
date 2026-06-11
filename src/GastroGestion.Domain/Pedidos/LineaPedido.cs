using GastroGestion.Domain.Common;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Pedidos;

/// <summary>
/// A single dish line on a <see cref="Pedido"/>. Owned by the Pedido aggregate.
/// <para>
/// Price snapshot is set once via <see cref="ConfirmarPrecio"/>. Totals are computed
/// from the confirmed price and quantity — never read from the live catalogue (design §5c, §3).
/// </para>
/// <para>
/// Edit-lock rule: a line is only editable while it has no OT <b>or</b> its OT is
/// still in state Creada. Once the OT moves to Preparandose/Lista the line is locked.
/// </para>
/// </summary>
public class LineaPedido : Entity
{
    /// <summary>Cross-boundary reference to the ordered Plato.</summary>
    public Guid PlatoId { get; private set; }

    /// <summary>Ordered quantity (minimum 1).</summary>
    public int Cantidad { get; private set; }

    /// <summary>Free-text observations for the kitchen (e.g. "sin cebolla").</summary>
    public string? Observaciones { get; private set; }

    // ── Price snapshot ────────────────────────────────────────────────────────

    /// <summary>
    /// Confirmed unit price at order time. Null until <see cref="ConfirmarPrecio"/> is called.
    /// </summary>
    public Dinero? PrecioUnitario { get; private set; }

    /// <summary>
    /// IVA rate confirmed at order time. Null until <see cref="ConfirmarPrecio"/> is called.
    /// </summary>
    public PorcentajeIVA? IVA { get; private set; }

    private bool _precioConfirmado;

    // ── Computed totals (only valid after ConfirmarPrecio) ───────────────────

    /// <summary>
    /// Net subtotal: PrecioUnitario × Cantidad. Null until price is confirmed.
    /// </summary>
    public Dinero? SubtotalLinea =>
        PrecioUnitario is null ? null
            : PrecioUnitario.Multiplicar(Cantidad);

    /// <summary>
    /// IVA amount: SubtotalLinea × IVA.Tasa. Null until price is confirmed.
    /// </summary>
    public Dinero? IVALinea =>
        SubtotalLinea is null || IVA is null ? null
            : SubtotalLinea.AplicarIVA(IVA);

    /// <summary>
    /// Total with IVA: SubtotalLinea + IVALinea. Null until price is confirmed.
    /// </summary>
    public Dinero? TotalLinea =>
        SubtotalLinea is null || IVALinea is null ? null
            : SubtotalLinea.Sumar(IVALinea);

    private LineaPedido(Guid id, Guid platoId, int cantidad, string? observaciones) : base(id)
    {
        PlatoId      = platoId;
        Cantidad     = cantidad;
        Observaciones = observaciones;
    }

    // EF Core parameterless ctor.
#pragma warning disable CS8618
    protected LineaPedido() { }
#pragma warning restore CS8618

    internal static LineaPedido Crear(Guid platoId, int cantidad, string? observaciones = null)
    {
        if (platoId == Guid.Empty)
            throw new DomainException("LineaPedido.PlatoId cannot be empty.");
        if (cantidad <= 0)
            throw new DomainException($"LineaPedido.Cantidad must be greater than zero. Received: {cantidad}.");

        return new LineaPedido(Guid.NewGuid(), platoId, cantidad, observaciones);
    }

    /// <summary>
    /// Confirms the effective price snapshot for this line. Set-once — throws if
    /// called more than once, preventing accidental overwrite of financial records.
    /// </summary>
    /// <param name="precio">Effective unit price (menu override or base price).</param>
    /// <param name="iva">IVA rate for this line.</param>
    public void ConfirmarPrecio(Dinero precio, PorcentajeIVA iva)
    {
        if (_precioConfirmado)
            throw new DomainException(
                "Price has already been confirmed for this LineaPedido. ConfirmarPrecio is set-once.");
        if (precio is null)
            throw new DomainException("LineaPedido price cannot be null.");
        if (iva is null)
            throw new DomainException("LineaPedido IVA cannot be null.");

        PrecioUnitario  = precio;
        IVA             = iva;
        _precioConfirmado = true;
    }

    /// <summary>
    /// Updates the quantity. Only valid while the line is editable.
    /// The edit-lock check is enforced by the parent Pedido (which knows OT state).
    /// </summary>
    internal void ActualizarCantidad(int nuevaCantidad)
    {
        if (nuevaCantidad <= 0)
            throw new DomainException($"Cantidad must be greater than zero. Received: {nuevaCantidad}.");
        Cantidad = nuevaCantidad;
    }

    /// <summary>
    /// Updates the observations. Only valid while the line is editable.
    /// </summary>
    internal void ActualizarObservaciones(string? observaciones) =>
        Observaciones = observaciones;
}
