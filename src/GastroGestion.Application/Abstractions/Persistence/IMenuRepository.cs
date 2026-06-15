using GastroGestion.Domain.Menus;

namespace GastroGestion.Application.Abstractions.Persistence;

public interface IMenuRepository
{
    Task<Menu?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Menu menu, CancellationToken ct = default);

    /// <summary>
    /// Returns all active Menus whose <c>FechaVigencia</c> is on or after <paramref name="fecha"/>.
    /// Used by <c>EfectivoPrecioService</c> to resolve menu price overrides for a given date.
    /// </summary>
    Task<IReadOnlyList<Menu>> GetActivosByFechaAsync(DateOnly fecha, CancellationToken ct = default);

    Task<IReadOnlyList<Menu>> GetAllAsync(CancellationToken ct = default);
}
