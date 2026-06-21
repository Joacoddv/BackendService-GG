namespace GastroGestion.Application.Abstractions.Security;

/// <summary>
/// Port for generating and hashing opaque refresh tokens. The implementation lives in
/// Infrastructure (cryptographic RNG + SHA-256). No crypto types leak into the Application layer.
/// </summary>
public interface IRefreshTokenGenerator
{
    /// <summary>Generates a new high-entropy refresh token (raw value + its hash for storage).</summary>
    GeneratedRefreshToken Generate();

    /// <summary>Hashes a raw refresh token so it can be matched against the stored hash.</summary>
    string Hash(string rawToken);
}

/// <summary>A freshly generated refresh token: the raw value (returned to the client once) and its stored hash.</summary>
public sealed record GeneratedRefreshToken(string Raw, string Hash);
