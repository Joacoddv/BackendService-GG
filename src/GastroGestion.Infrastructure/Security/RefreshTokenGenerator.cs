using System.Security.Cryptography;
using System.Text;
using GastroGestion.Application.Abstractions.Security;

namespace GastroGestion.Infrastructure.Security;

/// <summary>
/// Generates 256-bit cryptographically-random refresh tokens and hashes them with SHA-256.
/// Only the hash is persisted; the raw value is returned to the client once. SHA-256 (no salt)
/// is sufficient here because the token is already high-entropy random — unlike passwords.
/// </summary>
internal sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    public GeneratedRefreshToken Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw   = Base64UrlEncode(bytes);
        return new GeneratedRefreshToken(raw, Hash(raw));
    }

    public string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes); // uppercase hex, stable for column comparison
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
