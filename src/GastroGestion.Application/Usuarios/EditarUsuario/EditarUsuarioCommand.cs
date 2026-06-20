using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Usuarios.EditarUsuario;

public sealed record EditarUsuarioCommand(
    Guid Id,
    string NombreCompleto,
    RolUsuario Rol);
