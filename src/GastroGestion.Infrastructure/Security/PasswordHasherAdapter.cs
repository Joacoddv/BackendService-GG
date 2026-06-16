using GastroGestion.Application.Abstractions.Security;
using GastroGestion.Domain.Usuarios;
using Microsoft.AspNetCore.Identity;

namespace GastroGestion.Infrastructure.Security;

/// <summary>
/// Infrastructure implementation of IPasswordHasher backed by ASP.NET Core Identity's
/// PasswordHasher&lt;Usuario&gt; (PBKDF2-SHA256). This type lives only in Infrastructure (ADR-2).
/// </summary>
internal sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<Usuario> _inner = new();

    public string Hash(Usuario usuario, string plainPassword)
        => _inner.HashPassword(usuario, plainPassword);

    public bool Verify(Usuario usuario, string hashedPassword, string providedPassword)
    {
        var result = _inner.VerifyHashedPassword(usuario, hashedPassword, providedPassword);
        // Accept both Success and SuccessRehashNeeded — both mean the password was correct
        return result is PasswordVerificationResult.Success
                      or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
