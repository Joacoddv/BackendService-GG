using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Usuarios;

/// <summary>
/// Response DTO for a Cocinero user exposed via GET /usuarios/cocineros (CCC-A01).
/// </summary>
public sealed record CocineroResponse(Guid Id, string NombreCompleto);

/// <summary>
/// Response DTO for a user in the management CRUD. PasswordHash is never exposed.
/// </summary>
public sealed record UsuarioResponse(
    Guid Id,
    string Email,
    string NombreCompleto,
    RolUsuario Rol,
    bool Activo);
