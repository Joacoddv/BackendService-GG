using GastroGestion.Domain.Platos;

namespace GastroGestion.Application.Abstractions.Persistence;

public interface IPlatoRepository
{
    Task<Plato?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Plato plato, CancellationToken ct = default);
    Task<IReadOnlyList<Plato>> GetAllAsync(CancellationToken ct = default);
}
