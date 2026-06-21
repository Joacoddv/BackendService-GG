using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Abstractions.Security;

namespace GastroGestion.Application.Auth.CerrarSesion;

/// <summary>
/// Revokes the presented refresh token (logout of a single session/device). Idempotent and
/// non-revealing: an unknown or already-revoked token completes silently, so the endpoint can
/// always answer 204 without leaking which tokens exist.
/// </summary>
public sealed class CerrarSesionHandler
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IRefreshTokenGenerator  _refreshGenerator;
    private readonly ISeguridadUnitOfWork     _uow;

    public CerrarSesionHandler(
        IRefreshTokenRepository refreshTokens,
        IRefreshTokenGenerator refreshGenerator,
        ISeguridadUnitOfWork uow)
    {
        _refreshTokens    = refreshTokens;
        _refreshGenerator = refreshGenerator;
        _uow              = uow;
    }

    public async Task Handle(CerrarSesionCommand cmd, CancellationToken ct = default)
    {
        var hash     = _refreshGenerator.Hash(cmd.RefreshToken);
        var existing = await _refreshTokens.GetByHashAsync(hash, ct);

        // No-op for an unknown or already-revoked token — keep logout idempotent and silent.
        if (existing is null || existing.RevocadoEnUtc is not null)
            return;

        existing.Revocar(DateTime.UtcNow);
        await _uow.SaveChangesAsync(ct);
    }
}
