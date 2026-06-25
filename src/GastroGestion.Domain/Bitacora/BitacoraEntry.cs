using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;

namespace GastroGestion.Domain.Bitacora;

/// <summary>
/// Append-only audit log entry. Every mutating or security-sensitive action writes
/// one of these. Entities are never modified or deleted — only inserted.
/// </summary>
public sealed class BitacoraEntry : AggregateRoot
{
    /// <summary>The authenticated user's id, or <see cref="Guid.Empty"/> for anonymous actions.</summary>
    public Guid UsuarioId { get; private set; }

    /// <summary>The email address of the actor (from the JWT or the request body for login failures).</summary>
    public string Email { get; private set; }

    /// <summary>Role of the actor at the time of the action, or <c>null</c> for anonymous actions.</summary>
    public RolUsuario? Rol { get; private set; }

    /// <summary>Short English label describing the action (e.g. "Create client").</summary>
    public string Accion { get; private set; }

    /// <summary>Optional additional context: route params, failure reason, etc.</summary>
    public string? Detalle { get; private set; }

    /// <summary>HTTP status code returned to the caller.</summary>
    public int ResultadoHttp { get; private set; }

    /// <summary>UTC timestamp at which the entry was recorded.</summary>
    public DateTime FechaUtc { get; private set; }

#pragma warning disable CS8618
    private BitacoraEntry() { } // EF Core
#pragma warning restore CS8618

    private BitacoraEntry(
        Guid id,
        Guid usuarioId,
        string email,
        RolUsuario? rol,
        string accion,
        string? detalle,
        int resultadoHttp,
        DateTime fechaUtc)
        : base(id)
    {
        UsuarioId    = usuarioId;
        Email        = email;
        Rol          = rol;
        Accion       = accion;
        Detalle      = detalle;
        ResultadoHttp = resultadoHttp;
        FechaUtc     = fechaUtc;
    }

    /// <summary>
    /// Records an authenticated user's action.
    /// </summary>
    /// <param name="usuarioId">Authenticated user's id.</param>
    /// <param name="email">Authenticated user's email claim.</param>
    /// <param name="rol">Role of the user at the time of the action, or <c>null</c> if no role claim is present.</param>
    /// <param name="accion">Short English label for the action. Must be non-empty.</param>
    /// <param name="detalle">Optional context or detail string (nullable).</param>
    /// <param name="resultadoHttp">HTTP response status code.</param>
    /// <param name="fechaUtc">UTC timestamp of the action.</param>
    public static BitacoraEntry Registrar(
        Guid usuarioId,
        string email,
        RolUsuario? rol,
        string accion,
        string? detalle,
        int resultadoHttp,
        DateTime fechaUtc)
    {
        if (string.IsNullOrWhiteSpace(accion))
            throw new DomainException("BitacoraEntry.Accion cannot be null or empty.");

        return new BitacoraEntry(Guid.NewGuid(), usuarioId, email, rol, accion, detalle, resultadoHttp, fechaUtc);
    }

    /// <summary>
    /// Records an anonymous (unauthenticated) action, such as a failed login attempt.
    /// Sets <see cref="UsuarioId"/> to <see cref="Guid.Empty"/> and leaves <see cref="Rol"/>
    /// as <c>null</c> — there is no role for an unauthenticated actor.
    /// </summary>
    /// <param name="email">Email provided in the request (e.g. the login attempt email).</param>
    /// <param name="accion">Short English label for the action. Must be non-empty.</param>
    /// <param name="detalle">Optional context or detail string (nullable).</param>
    /// <param name="resultadoHttp">HTTP response status code.</param>
    /// <param name="fechaUtc">UTC timestamp of the action.</param>
    public static BitacoraEntry RegistrarAnonimo(
        string email,
        string accion,
        string? detalle,
        int resultadoHttp,
        DateTime fechaUtc)
    {
        if (string.IsNullOrWhiteSpace(accion))
            throw new DomainException("BitacoraEntry.Accion cannot be null or empty.");

        return new BitacoraEntry(
            Guid.NewGuid(),
            Guid.Empty,
            email,
            null, // no role for an unauthenticated actor
            accion,
            detalle,
            resultadoHttp,
            fechaUtc);
    }
}
