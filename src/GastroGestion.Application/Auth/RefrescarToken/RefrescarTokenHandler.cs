using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Abstractions.Security;
using GastroGestion.Application.Auth.Login;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Usuarios;

namespace GastroGestion.Application.Auth.RefrescarToken;

/// <summary>
/// Exchanges a valid refresh token for a new access token + a new refresh token (rotation):
/// the presented token is revoked and a fresh one is issued. Any invalid, expired, revoked,
/// or orphaned token yields the same generic AuthenticationFailedException.
/// Reuses <see cref="LoginResult"/> since the response shape is identical to login.
/// </summary>
public sealed class RefrescarTokenHandler
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUsuarioRepository      _usuarios;
    private readonly ITokenIssuer            _tokens;
    private readonly IRefreshTokenGenerator  _refreshGenerator;
    private readonly ISeguridadUnitOfWork             _uow;

    public RefrescarTokenHandler(
        IRefreshTokenRepository refreshTokens,
        IUsuarioRepository usuarios,
        ITokenIssuer tokens,
        IRefreshTokenGenerator refreshGenerator,
        ISeguridadUnitOfWork uow)
    {
        _refreshTokens    = refreshTokens;
        _usuarios         = usuarios;
        _tokens           = tokens;
        _refreshGenerator = refreshGenerator;
        _uow              = uow;
    }

    public async Task<LoginResult> Handle(RefrescarTokenCommand cmd, CancellationToken ct = default)
    {
        var nowUtc = DateTime.UtcNow;

        var hash     = _refreshGenerator.Hash(cmd.RefreshToken);
        var existing = await _refreshTokens.GetByHashAsync(hash, ct);

        if (existing is null || !existing.EsActivo(nowUtc))
            throw new AuthenticationFailedException("Invalid refresh token.");

        var usuario = await _usuarios.GetByIdAsync(existing.UsuarioId, ct);
        if (usuario is null || !usuario.Activo)
            throw new AuthenticationFailedException("Invalid refresh token.");

        // Rotation: revoke the presented token and issue a brand-new one.
        existing.Revocar(nowUtc);

        var access    = _tokens.Issue(usuario);
        var generated = _refreshGenerator.Generate();
        var refresh   = RefreshToken.Crear(usuario.Id, generated.Hash, nowUtc.Add(RefreshTokenLifetime), nowUtc);
        await _refreshTokens.AddAsync(refresh, ct);

        await _uow.SaveChangesAsync(ct);

        return new LoginResult(
            access.Value, access.ExpiresAtUtc,
            generated.Raw, refresh.ExpiresAtUtc,
            usuario.Id, usuario.Rol);
    }
}
