using GastroGestion.Domain.Common;

namespace GastroGestion.Domain.Pedidos.Events;

/// <summary>
/// Raised when an <see cref="OrdenTrabajo"/> starts preparation (a cook is assigned and it moves
/// Creada → Preparandose). The stock handler turns the existing RESERVA into actual CONSUMO:
/// releasing the reservation and recording consumption for the same ingredients.
/// </summary>
public sealed record OrdenTrabajoIniciada(
    Guid PedidoId,
    Guid OrdenTrabajoId,
    Guid PlatoId,
    IReadOnlyList<LineaRecetaSnapshot> RecetaSnapshot,
    DateTime OccurredOnUtc) : IDomainEvent;
