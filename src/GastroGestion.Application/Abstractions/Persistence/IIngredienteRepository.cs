using GastroGestion.Domain.Ingredientes;

namespace GastroGestion.Application.Abstractions.Persistence;

public interface IIngredienteRepository
{
    Task<Ingrediente?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Ingrediente ingrediente, CancellationToken ct = default);
    Task<IReadOnlyList<Ingrediente>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns ingredientes filtered by active status and optional name substring.</summary>
    Task<IReadOnlyList<Ingrediente>> SearchAsync(
        string? nombre,
        bool incluirInactivos,
        CancellationToken ct = default);

    /// <summary>
    /// Returns true if another ingrediente (id != <paramref name="excludeId"/>) already has
    /// the given <paramref name="nombre"/> (case-insensitive, exact match for uniqueness).
    /// </summary>
    Task<bool> NombreExistsForOtherAsync(string nombre, Guid excludeId, CancellationToken ct = default);
}
