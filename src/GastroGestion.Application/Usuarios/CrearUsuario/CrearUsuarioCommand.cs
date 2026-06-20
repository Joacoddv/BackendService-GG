using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Usuarios.CrearUsuario;

public sealed record CrearUsuarioCommand(
    string Email,
    string NombreCompleto,
    RolUsuario Rol,
    string Password);
