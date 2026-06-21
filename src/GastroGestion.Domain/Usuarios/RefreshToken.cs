using GastroGestion.Domain.Common;

namespace GastroGestion.Domain.Usuarios;

/// <summary>
/// A refresh token issued to a user. Only the SHA-256 hash of the opaque token value is
/// persisted — the raw value is shown to the client once and never stored (defence in depth).
/// Tokens are single-use: refreshing rotates (revokes the old, issues a new one).
/// </summary>
public class RefreshToken : AggregateRoot
{
    public Guid     UsuarioId      { get; private set; }
    public string   TokenHash      { get; private set; }
    public DateTime ExpiresAtUtc   { get; private set; }
    public DateTime CreadoEnUtc    { get; private set; }
    public DateTime? RevocadoEnUtc { get; private set; }

#pragma warning disable CS8618
    private RefreshToken() { } // EF Core
#pragma warning restore CS8618

    private RefreshToken(Guid id, Guid usuarioId, string tokenHash, DateTime expiresAtUtc, DateTime creadoEnUtc)
        : base(id)
    {
        UsuarioId    = usuarioId;
        TokenHash    = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreadoEnUtc  = creadoEnUtc;
    }

    public static RefreshToken Crear(Guid usuarioId, string tokenHash, DateTime expiresAtUtc, DateTime creadoEnUtc)
    {
        if (usuarioId == Guid.Empty)
            throw new DomainException("RefreshToken.UsuarioId cannot be empty.");
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("RefreshToken.TokenHash cannot be null or empty.");
        if (expiresAtUtc <= creadoEnUtc)
            throw new DomainException("RefreshToken.ExpiresAtUtc must be after CreadoEnUtc.");

        return new RefreshToken(Guid.NewGuid(), usuarioId, tokenHash, expiresAtUtc, creadoEnUtc);
    }

    /// <summary>True while the token is neither revoked nor expired at <paramref name="nowUtc"/>.</summary>
    public bool EsActivo(DateTime nowUtc) => RevocadoEnUtc is null && nowUtc < ExpiresAtUtc;

    /// <summary>Revokes the token. Idempotent — keeps the original revocation instant.</summary>
    public void Revocar(DateTime nowUtc) => RevocadoEnUtc ??= nowUtc;
}
