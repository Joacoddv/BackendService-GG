using GastroGestion.Domain.Mesas;

namespace GastroGestion.Application.Abstractions.Persistence;

public interface IMesaRepository
{
    Task<Mesa?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Mesa mesa, CancellationToken ct = default);
}
