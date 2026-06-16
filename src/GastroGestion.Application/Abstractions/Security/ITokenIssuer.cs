using GastroGestion.Domain.Usuarios;

namespace GastroGestion.Application.Abstractions.Security;

/// <summary>
/// Port for issuing signed access tokens. Implemented in Infrastructure behind
/// JwtSecurityTokenHandler. No JWT or Infrastructure namespace appears in this port (ADR-3, ADR-4).
/// </summary>
public interface ITokenIssuer
{
    /// <summary>Issues a signed access token for the given user and returns it with its expiry.</summary>
    AccessToken Issue(Usuario usuario);
}

/// <summary>
/// Lightweight Application-layer record carrying an issued token's value and expiry instant.
/// Keeps JwtSecurityToken out of the Application layer.
/// </summary>
public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);
