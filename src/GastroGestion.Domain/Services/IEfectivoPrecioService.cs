using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Services;

/// <summary>
/// Domain service contract for resolving the effective unit price of a Plato
/// on a given date. Implementation lives in the Application layer because it
/// requires repository access to load Menú entries and Plato.PrecioBase.
/// <para>
/// Resolution rule: menu price override for the date → else Plato.PrecioBase.
/// </para>
/// <para>
/// W-01: <c>Task</c> is BCL — adding the async signature requires zero new
/// <c>PackageReference</c> or <c>ProjectReference</c> on Domain.csproj.
/// </para>
/// </summary>
public interface IEfectivoPrecioService
{
    /// <summary>
    /// Resolves the effective (Dinero, IVA) pair for a given Plato on a given date.
    /// </summary>
    /// <param name="platoId">The Plato whose price needs resolution.</param>
    /// <param name="fecha">The date of the order (used to look up menu overrides).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A tuple of (effectivePrice, ivaRate). The caller should immediately snapshot
    /// this onto the <c>LineaPedido</c> via ConfirmarPrecio.
    /// </returns>
    Task<(Dinero Precio, PorcentajeIVA IVA)> ResolverPrecioEfectivoAsync(
        Guid platoId, DateOnly fecha, CancellationToken ct = default);
}
