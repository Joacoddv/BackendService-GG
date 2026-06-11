using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Services;

/// <summary>
/// Domain service contract for resolving the effective unit price of a Plato
/// on a given date. Implementation lives in the Application layer (phase 4)
/// because it requires repository access to load Menú entries and Plato.PrecioBase.
/// <para>
/// Resolution rule (design §5c): menu price override for the date → else Plato.PrecioBase.
/// </para>
/// </summary>
public interface IEfectivoPrecioService
{
    /// <summary>
    /// Resolves the effective (Dinero, IVA) pair for a given Plato on a given date.
    /// </summary>
    /// <param name="platoId">The Plato whose price needs resolution.</param>
    /// <param name="fecha">The date of the order (used to look up menu overrides).</param>
    /// <returns>
    /// A tuple of (effectivePrice, ivaRate). The caller should immediately snapshot this
    /// onto the <see cref="GastroGestion.Domain.Pedidos.LineaPedido"/> via ConfirmarPrecio.
    /// </returns>
    (Dinero Precio, PorcentajeIVA IVA) ResolverPrecioEfectivo(Guid platoId, DateOnly fecha);
}
