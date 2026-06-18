using GastroGestion.Domain.Clientes;

namespace GastroGestion.Application.Abstractions.Persistence;

public interface IClienteRepository
{
    Task<Cliente?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Cliente cliente, CancellationToken ct = default);
    Task<IReadOnlyList<Cliente>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns clientes filtered by active status and optional partial name match.
    /// When <paramref name="incluirInactivos"/> is false, only active clientes are returned.
    /// When <paramref name="nombre"/> is provided, applies a case-insensitive partial match.
    /// </summary>
    Task<IReadOnlyList<Cliente>> SearchAsync(
        string? nombre,
        bool incluirInactivos,
        CancellationToken ct = default);

    /// <summary>
    /// Returns true when another cliente (different from <paramref name="excludeId"/>)
    /// already holds the given CUIT value.
    /// </summary>
    Task<bool> CuitExistsForOtherAsync(string cuit, Guid excludeId, CancellationToken ct = default);
}
