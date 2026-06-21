using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Stock;

/// <summary>
/// Append-only aggregate root representing a single stock ledger entry.
/// Each movement is its own aggregate root; the "ledger" is a repository query
/// over all movements for a given IngredienteId (design §5b).
/// <para>
/// <b>Sign convention:</b><br/>
/// - Positive (inflow): <see cref="TipoMovimientoStock.Compra"/>,
///   <see cref="TipoMovimientoStock.LiberacionReserva"/>,
///   <see cref="TipoMovimientoStock.DevolucionCancelacion"/>.<br/>
/// - Negative (outflow): <see cref="TipoMovimientoStock.Consumo"/>,
///   <see cref="TipoMovimientoStock.Reserva"/>.<br/>
/// - Signed (caller-determined): <see cref="TipoMovimientoStock.Ajuste"/>
///   (positive = ingress adjustment, negative = egress adjustment).
/// </para>
/// <para>
/// Once created a <see cref="MovimientoStock"/> is immutable — no mutation methods exist.
/// </para>
/// </summary>
public class MovimientoStock : AggregateRoot
{
    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>Cross-boundary reference to the Ingrediente being tracked.</summary>
    public Guid IngredienteId { get; private set; }

    /// <summary>
    /// Signed quantity. Positive = inflow; negative = outflow.
    /// Zero is never valid (rejected at construction).
    /// </summary>
    public decimal Cantidad { get; private set; }

    /// <summary>Classification of this stock movement.</summary>
    public TipoMovimientoStock Tipo { get; private set; }

    /// <summary>UTC timestamp when this movement was recorded.</summary>
    public DateTime FechaMovimiento { get; private set; }

    /// <summary>
    /// Optional link to the <c>OrdenTrabajo</c> that triggered this movement
    /// (e.g. Consumo or Reserva during OT processing).
    /// </summary>
    public Guid? OrdenTrabajoId { get; private set; }

    /// <summary>
    /// Optional link to the <c>LineaPedido</c> associated with this movement
    /// (e.g. Reserva at order-line level).
    /// </summary>
    public Guid? LineaPedidoId { get; private set; }

    /// <summary>
    /// Optional lot identifier for traceability (e.g. supplier batch "L2024-01").
    /// Populated on <see cref="TipoMovimientoStock.Compra"/> movements when known.
    /// </summary>
    public string? Lote { get; private set; }

    /// <summary>
    /// Optional expiry date for the lot.
    /// Nullable from day one to support the future near-expiry suggestion seam
    /// (design §5b). Not required — many ingredients do not expire.
    /// </summary>
    public DateOnly? FechaVencimiento { get; private set; }

    // ── EF Core / private ─────────────────────────────────────────────────────

#pragma warning disable CS8618
    private MovimientoStock() { }
#pragma warning restore CS8618

    private MovimientoStock(
        Guid id,
        Guid ingredienteId,
        decimal cantidad,
        TipoMovimientoStock tipo,
        DateTime fechaMovimiento,
        Guid? ordenTrabajoId,
        Guid? lineaPedidoId,
        string? lote,
        DateOnly? fechaVencimiento) : base(id)
    {
        IngredienteId    = ingredienteId;
        Cantidad         = cantidad;
        Tipo             = tipo;
        FechaMovimiento  = fechaMovimiento;
        OrdenTrabajoId   = ordenTrabajoId;
        LineaPedidoId    = lineaPedidoId;
        Lote             = lote;
        FechaVencimiento = fechaVencimiento;
    }

    // ── Factories ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Records a general stock movement. Validates that Cantidad is non-zero and
    /// that the sign matches the expected convention for the given type.
    /// <para>
    /// Sign rules enforced here:<br/>
    /// - <c>Compra</c> / <c>LiberacionReserva</c> / <c>DevolucionCancelacion</c>: must be positive.<br/>
    /// - <c>Consumo</c> / <c>Reserva</c>: stored as negative (caller passes the absolute value;
    ///   the factory negates it).<br/>
    /// - <c>Ajuste</c>: sign follows the caller (positive = ingreso, negative = egreso);
    ///   zero is still rejected.
    /// </para>
    /// </summary>
    public static MovimientoStock RegistrarMovimiento(
        Guid ingredienteId,
        TipoMovimientoStock tipo,
        decimal cantidad,
        Guid? ordenTrabajoId = null,
        Guid? lineaPedidoId = null)
    {
        if (ingredienteId == Guid.Empty)
            throw new DomainException("MovimientoStock.IngredienteId cannot be empty.");
        if (cantidad == 0)
            throw new DomainException("MovimientoStock.Cantidad cannot be zero.");

        var cantidadFinal = tipo switch
        {
            TipoMovimientoStock.Compra or
            TipoMovimientoStock.LiberacionReserva or
            TipoMovimientoStock.DevolucionCancelacion => ValidarPositivo(cantidad, tipo),

            TipoMovimientoStock.Consumo or
            TipoMovimientoStock.Reserva or
            TipoMovimientoStock.Merma => -ValidarPositivo(cantidad, tipo),

            TipoMovimientoStock.Ajuste => cantidad, // signed: caller determines direction

            _ => throw new DomainException($"Unrecognised TipoMovimientoStock: {tipo}.")
        };

        return new MovimientoStock(
            Guid.NewGuid(),
            ingredienteId,
            cantidadFinal,
            tipo,
            DateTime.UtcNow,
            ordenTrabajoId,
            lineaPedidoId,
            lote: null,
            fechaVencimiento: null);
    }

    /// <summary>
    /// Records a purchase movement (<see cref="TipoMovimientoStock.Compra"/>)
    /// with optional lot and expiry information.
    /// Wraps <see cref="RegistrarMovimiento"/> and adds lot/expiry traceability.
    /// </summary>
    /// <param name="ingredienteId">Ingredient receiving the stock.</param>
    /// <param name="cantidad">Absolute quantity purchased (must be &gt; 0).</param>
    /// <param name="lote">Optional supplier lot identifier.</param>
    /// <param name="fechaVencimiento">Optional expiry date for this lot.</param>
    public static MovimientoStock RegistrarCompra(
        Guid ingredienteId,
        decimal cantidad,
        string? lote = null,
        DateOnly? fechaVencimiento = null)
    {
        if (ingredienteId == Guid.Empty)
            throw new DomainException("MovimientoStock.IngredienteId cannot be empty.");
        if (cantidad <= 0)
            throw new DomainException($"Compra cantidad must be greater than zero. Received: {cantidad}.");

        return new MovimientoStock(
            Guid.NewGuid(),
            ingredienteId,
            cantidad,
            TipoMovimientoStock.Compra,
            DateTime.UtcNow,
            ordenTrabajoId: null,
            lineaPedidoId: null,
            lote,
            fechaVencimiento);
    }

    // ── Projection ────────────────────────────────────────────────────────────

    /// <summary>
    /// Pure static projection: sums signed quantities over a set of movements.
    /// The domain invariant (balance ≥ 0) is stated here; enforcement at the
    /// row-lock level is an infrastructure concern (design §5b).
    /// </summary>
    /// <param name="movimientos">All movements for a single IngredienteId.</param>
    /// <returns>Net available quantity.</returns>
    /// <exception cref="DomainException">
    /// Thrown when the projected balance is negative, indicating a domain invariant
    /// violation that the infrastructure concurrency layer must prevent.
    /// </exception>
    public static decimal CalcularDisponible(IEnumerable<MovimientoStock> movimientos)
    {
        var balance = movimientos.Sum(m => m.Cantidad);
        if (balance < 0)
            throw new DomainException(
                $"Projected stock balance is negative ({balance}). " +
                "The infrastructure layer must prevent this via optimistic/pessimistic concurrency.");
        return balance;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static decimal ValidarPositivo(decimal cantidad, TipoMovimientoStock tipo)
    {
        if (cantidad <= 0)
            throw new DomainException(
                $"TipoMovimientoStock.{tipo} requires a positive Cantidad. Received: {cantidad}.");
        return cantidad;
    }
}
