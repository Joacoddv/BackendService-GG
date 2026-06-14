using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Services;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Application.Services;

/// <summary>
/// Application-layer implementation of <see cref="IEfectivoPrecioService"/>.
/// Resolves the effective unit price for a Plato on a given date:
///   1. Find the first active Menu with FechaVigencia &gt;= fecha that contains the platoId.
///   2. If a matching MenuItem has a non-null PrecioOverride, return that override.
///   3. Otherwise, return Plato.PrecioBase.
/// IVA always comes from Plato.AlicuotaIVA regardless of menu overrides.
/// </summary>
internal sealed class EfectivoPrecioService : IEfectivoPrecioService
{
    private readonly IMenuRepository _menus;
    private readonly IPlatoRepository _platos;

    public EfectivoPrecioService(IMenuRepository menus, IPlatoRepository platos)
    {
        _menus  = menus;
        _platos = platos;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Note: the domain interface is synchronous. This implementation uses synchronous
    /// repository access via blocking calls. Phase 4 will introduce async use-case orchestration
    /// that pre-loads the required data before calling this service.
    /// </remarks>
    public (Dinero Precio, PorcentajeIVA IVA) ResolverPrecioEfectivo(Guid platoId, DateOnly fecha)
    {
        var plato = _platos.GetByIdAsync(platoId).GetAwaiter().GetResult()
            ?? throw new InvalidOperationException($"Plato {platoId} not found.");

        var iva = new PorcentajeIVA(plato.AlicuotaIVA);

        // Check for a menu price override on the given date.
        var menus = _menus.GetActivosByFechaAsync(fecha).GetAwaiter().GetResult();
        var overridePrice = menus
            .SelectMany(m => m.Items)
            .Where(it => it.PlatoId == platoId && it.PrecioOverride is not null)
            .Select(it => it.PrecioOverride)
            .FirstOrDefault();

        var precio = overridePrice ?? plato.PrecioBase;
        return (precio, iva);
    }
}
