using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Usuarios;

/// <summary>Request DTO for registering a new user. Used by POST /usuarios.</summary>
public sealed record CrearUsuarioRequest(
    string Email,
    string NombreCompleto,
    RolUsuario Rol,
    string Password);

/// <summary>
/// Request DTO for editing an existing user. Used by PUT /usuarios/{id}.
/// Email is immutable (login identity) and the password is changed elsewhere.
/// </summary>
public sealed record EditarUsuarioRequest(
    string NombreCompleto,
    RolUsuario Rol);
