using GastroGestion.Domain.Pedidos;

namespace GastroGestion.Application.Pedidos.GetOrdenesByEstado;

/// <summary>
/// Query to retrieve kitchen work orders, optionally filtered by state.
/// When <see cref="Estado"/> is null, all non-Cancelada OTs are returned (ADR-002).
/// </summary>
public sealed record GetOrdenesByEstadoQuery(EstadoOT? Estado);
