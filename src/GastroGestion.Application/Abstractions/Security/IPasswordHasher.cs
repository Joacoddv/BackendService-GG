using GastroGestion.Domain.Usuarios;

namespace GastroGestion.Application.Abstractions.Security;

/// <summary>
/// Port for password hashing and verification. Implemented in Infrastructure behind
/// Microsoft.AspNetCore.Identity.PasswordHasher&lt;Usuario&gt; (PBKDF2-SHA256).
/// No Infrastructure or Identity namespace appears in this port (ADR-3, ADR-2).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Hashes <paramref name="plainPassword"/> for the given user and returns the hash string.</summary>
    string Hash(Usuario usuario, string plainPassword);

    /// <summary>
    /// Verifies <paramref name="providedPassword"/> against <paramref name="hashedPassword"/>.
    /// Returns <c>true</c> when the password is correct (including rehash-needed cases).
    /// </summary>
    bool Verify(Usuario usuario, string hashedPassword, string providedPassword);
}
