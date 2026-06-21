using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Abstractions.Security;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Usuarios;

namespace GastroGestion.Application.Auth.Login;

/// <summary>
/// Handles the login use case: load user, verify password, issue an access token and a
/// rotating refresh token. Throws AuthenticationFailedException for any credential failure —
/// all three failure paths produce the same exception type and message (AUTH-03-E).
/// Writes the refresh token, so it commits via IUnitOfWork (supersedes the read-only ADR-8).
/// </summary>
public sealed class LoginHandler
{
    // Refresh-token lifetime. Mirrors the hardcoded 8h access-token expiry in JwtTokenIssuer.
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    private readonly IUsuarioRepository      _usuarios;
    private readonly IPasswordHasher         _hasher;
    private readonly ITokenIssuer            _tokens;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IRefreshTokenGenerator  _refreshGenerator;
    private readonly IUnitOfWork             _uow;

    public LoginHandler(
        IUsuarioRepository usuarios,
        IPasswordHasher hasher,
        ITokenIssuer tokens,
        IRefreshTokenRepository refreshTokens,
        IRefreshTokenGenerator refreshGenerator,
        IUnitOfWork uow)
    {
        _usuarios         = usuarios;
        _hasher           = hasher;
        _tokens           = tokens;
        _refreshTokens    = refreshTokens;
        _refreshGenerator = refreshGenerator;
        _uow              = uow;
    }

    public async Task<LoginResult> Handle(LoginCommand cmd, CancellationToken ct = default)
    {
        var usuario = await _usuarios.GetByEmailAsync(cmd.Email, ct);

        // Unknown user or inactive account — same generic error to avoid leaking existence (AUTH-03-E)
        if (usuario is null || !usuario.Activo)
            throw new AuthenticationFailedException("Invalid credentials.");

        // Wrong password — same generic error
        if (!_hasher.Verify(usuario, usuario.PasswordHash, cmd.Password))
            throw new AuthenticationFailedException("Invalid credentials.");

        var access = _tokens.Issue(usuario);

        var nowUtc      = DateTime.UtcNow;
        var generated   = _refreshGenerator.Generate();
        var refresh     = RefreshToken.Crear(usuario.Id, generated.Hash, nowUtc.Add(RefreshTokenLifetime), nowUtc);
        await _refreshTokens.AddAsync(refresh, ct);
        await _uow.SaveChangesAsync(ct);

        return new LoginResult(
            access.Value, access.ExpiresAtUtc,
            generated.Raw, refresh.ExpiresAtUtc,
            usuario.Id, usuario.Rol);
    }
}
