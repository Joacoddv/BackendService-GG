using GastroGestion.Domain.Proveedores;

namespace GastroGestion.Application.Abstractions.Persistence;

public interface IProveedorRepository
{
    Task<Proveedor?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Proveedor proveedor, CancellationToken ct = default);

    /// <summary>
    /// Returns proveedores filtered by active status and optional partial name match.
    /// When <paramref name="incluirInactivos"/> is false, only active proveedores are returned.
    /// </summary>
    Task<IReadOnlyList<Proveedor>> SearchAsync(
        string? nombre, bool incluirInactivos, CancellationToken ct = default);

    /// <summary>Returns true when another proveedor already holds the given CUIT value.</summary>
    Task<bool> CuitExistsForOtherAsync(string cuit, Guid excludeId, CancellationToken ct = default);
}
