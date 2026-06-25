using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Abstractions;

/// <summary>
/// Provides access to the currently authenticated user's identity claims.
/// Resolved from the HTTP context in the API layer; can be substituted in tests.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The user's unique identifier from the <c>sub</c> JWT claim.</summary>
    Guid UsuarioId { get; }

    /// <summary>The user's email address from the JWT claims.</summary>
    string Email { get; }

    /// <summary>The user's role from the JWT claims, or <c>null</c> if no valid role claim is present.</summary>
    RolUsuario? Rol { get; }

    /// <summary><c>true</c> when the current request carries a valid, authenticated identity.</summary>
    bool IsAuthenticated { get; }
}
