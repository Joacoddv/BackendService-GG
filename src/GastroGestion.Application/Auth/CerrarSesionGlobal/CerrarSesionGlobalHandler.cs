using GastroGestion.Application.Abstractions.Persistence;

namespace GastroGestion.Application.Auth.CerrarSesionGlobal;

/// <summary>
/// Revokes every active refresh token of a user ("log out everywhere" / panic button). The caller
/// is identified from the access token, so this is authenticated. Returns how many sessions were
/// revoked. Idempotent: a user with no active sessions yields 0 and saves nothing.
/// </summary>
public sealed class CerrarSesionGlobalHandler
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ISeguridadUnitOfWork     _uow;

    public CerrarSesionGlobalHandler(IRefreshTokenRepository refreshTokens, ISeguridadUnitOfWork uow)
    {
        _refreshTokens = refreshTokens;
        _uow           = uow;
    }

    public async Task<int> Handle(CerrarSesionGlobalCommand cmd, CancellationToken ct = default)
    {
        var activos = await _refreshTokens.GetActivosByUsuarioAsync(cmd.UsuarioId, ct);
        if (activos.Count == 0)
            return 0;

        var nowUtc = DateTime.UtcNow;
        foreach (var token in activos)
            token.Revocar(nowUtc);

        await _uow.SaveChangesAsync(ct);
        return activos.Count;
    }
}
