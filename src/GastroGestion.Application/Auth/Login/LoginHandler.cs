using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Abstractions.Security;
using GastroGestion.Application.Common.Exceptions;

namespace GastroGestion.Application.Auth.Login;

/// <summary>
/// Handles the login use case: load user, verify password, issue token.
/// Throws AuthenticationFailedException for any credential failure — all three failure paths
/// produce the same exception type and message (AUTH-03-E: indistinguishable failure).
/// Does NOT inject IUnitOfWork — login is read-only (ADR-8).
/// </summary>
public sealed class LoginHandler
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordHasher    _hasher;
    private readonly ITokenIssuer       _tokens;

    public LoginHandler(IUsuarioRepository usuarios, IPasswordHasher hasher, ITokenIssuer tokens)
    {
        _usuarios = usuarios;
        _hasher   = hasher;
        _tokens   = tokens;
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

        var token = _tokens.Issue(usuario);
        return new LoginResult(token.Value, token.ExpiresAtUtc, usuario.Id, usuario.Rol);
    }
}
