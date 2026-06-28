using GastroGestion.Domain.Mesas;

namespace GastroGestion.Application.Abstractions.Persistence;

public interface IMesaRepository
{
    Task<Mesa?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Mesa mesa, CancellationToken ct = default);
    Task<IReadOnlyList<Mesa>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns true if another mesa (id != <paramref name="excludeId"/>) already has
    /// the given <paramref name="numero"/>.
    /// </summary>
    Task<bool> NumeroExistsForOtherAsync(int numero, Guid excludeId, CancellationToken ct = default);
}
