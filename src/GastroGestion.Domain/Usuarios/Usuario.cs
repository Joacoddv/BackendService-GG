using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;

namespace GastroGestion.Domain.Usuarios;

/// <summary>
/// Aggregate root for a system user. Mirrors the Cliente pattern (private setters, EF ctor,
/// static factory). PasswordHash is opaque to the Domain — hashing lives in Infrastructure (ADR-2).
/// Zero package references added to the Domain project.
/// </summary>
public class Usuario : AggregateRoot
{
    public string Email          { get; private set; }
    public string NombreCompleto { get; private set; }
    public RolUsuario Rol        { get; private set; }

    /// <summary>
    /// Opaque PBKDF2-SHA256 hash produced by the Infrastructure hasher (ADR-2).
    /// Domain treats this as a black box — never inspects or produces it.
    /// </summary>
    public string PasswordHash   { get; private set; }

    public bool Activo           { get; private set; }

#pragma warning disable CS8618
    private Usuario() { } // EF Core
#pragma warning restore CS8618

    private Usuario(
        Guid id,
        string email,
        string nombreCompleto,
        RolUsuario rol,
        string passwordHash)
        : base(id)
    {
        Email          = email;
        NombreCompleto = nombreCompleto;
        Rol            = rol;
        PasswordHash   = passwordHash;
        Activo         = true;
    }

    /// <summary>
    /// Creates a new active user. The caller is responsible for hashing the password before
    /// passing <paramref name="passwordHash"/> — the factory never calls any hashing library.
    /// </summary>
    /// <param name="email">Valid email address. Must contain '@' with non-empty local and domain parts.</param>
    /// <param name="nombreCompleto">Display name. Must be non-empty.</param>
    /// <param name="rol">Role assigned at creation.</param>
    /// <param name="passwordHash">Opaque hashed password string from the Infrastructure layer.</param>
    public static Usuario Crear(
        string email,
        string nombreCompleto,
        RolUsuario rol,
        string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Usuario.Email cannot be null or empty.");

        // Basic valid-email shape: non-empty local part + '@' + non-empty domain part
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0 || atIndex >= email.Length - 1)
            throw new DomainException("Usuario.Email must be a valid email address (local@domain).");

        if (string.IsNullOrWhiteSpace(nombreCompleto))
            throw new DomainException("Usuario.NombreCompleto cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Usuario.PasswordHash cannot be null or empty.");

        return new Usuario(Guid.NewGuid(), email, nombreCompleto, rol, passwordHash);
    }

    /// <summary>Deactivates this user. Idempotent.</summary>
    public void Desactivar() => Activo = false;
}
