using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Usuarios.BuscarUsuarios;

public sealed record BuscarUsuariosQuery(string? Nombre, RolUsuario? Rol, bool IncluirInactivos);
