using GastroGestion.Domain.Clientes;

namespace GastroGestion.Application.Abstractions.Persistence;

public interface IClienteRepository
{
    Task<Cliente?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Cliente cliente, CancellationToken ct = default);
}
