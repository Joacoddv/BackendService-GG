using GastroGestion.Domain.Usuarios;

namespace GastroGestion.Application.Abstractions.Persistence;

/// <summary>Persistence port for the RefreshToken aggregate.</summary>
public interface IRefreshTokenRepository
{
    /// <summary>Stages a new refresh token for insertion (persisted on next SaveChangesAsync).</summary>
    Task AddAsync(RefreshToken token, CancellationToken ct = default);

    /// <summary>Returns the refresh token matching the given hash, or <c>null</c> if none exists.</summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
}
