using GastroGestion.Domain.Common;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Pedidos.Events;

/// <summary>
/// Raised for each <see cref="OrdenTrabajo"/> in state <c>Creada</c> when the parent
/// <see cref="Pedido"/> is cancelled. Signals that the reserved/consumed stock for this
/// OT must be restored via a compensating ledger movement (design §5b, §10 rule 2).
/// OTs in Preparandose or Lista are NOT restored — stock is already consumed.
/// </summary>
public sealed record StockDebeRestaurarse(
    Guid PedidoId,
    Guid OrdenTrabajoId,
    IReadOnlyList<LineaRecetaSnapshot> RecetaSnapshot,
    DateTime OccurredOnUtc) : IDomainEvent;
