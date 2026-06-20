using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Usuarios;

namespace GastroGestion.Application.Abstractions.Persistence;

/// <summary>
/// Persistence port for the Usuario aggregate.
/// </summary>
public interface IUsuarioRepository
{
    /// <summary>Returns the user with the given email, or <c>null</c> if not found.</summary>
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Returns <c>true</c> if any Usuario row exists. Used for idempotency guards (ADR-9).</summary>
    Task<bool> AnyAsync(CancellationToken ct = default);

    /// <summary>Stages a new Usuario for insertion (persisted on next SaveChangesAsync).</summary>
    Task AddAsync(Usuario usuario, CancellationToken ct = default);

    /// <summary>Returns all active users whose role matches <paramref name="rol"/> (CCC-A01).</summary>
    Task<IReadOnlyList<Usuario>> GetByRolAsync(RolUsuario rol, CancellationToken ct = default);

    /// <summary>Returns the user with the given id, or <c>null</c> if not found.</summary>
    Task<Usuario?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Searches users by optional name fragment and role, optionally including inactive ones.
    /// Hides inactive users unless <paramref name="incluirInactivos"/> is true.
    /// </summary>
    Task<IReadOnlyList<Usuario>> SearchAsync(
        string? nombre,
        RolUsuario? rol,
        bool incluirInactivos,
        CancellationToken ct = default);
}
