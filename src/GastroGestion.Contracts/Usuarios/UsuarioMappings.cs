using GastroGestion.Application.Usuarios.CrearUsuario;
using GastroGestion.Application.Usuarios.EditarUsuario;
using GastroGestion.Domain.Usuarios;

namespace GastroGestion.Contracts.Usuarios;

/// <summary>
/// Extension methods for mapping Usuario domain entities to contract DTOs (CCC-A01)
/// and request DTOs to Application commands.
/// </summary>
public static class UsuarioMappings
{
    public static CocineroResponse ToCocineroResponse(this Usuario usuario)
        => new(usuario.Id, usuario.NombreCompleto);

    public static UsuarioResponse ToResponse(this Usuario usuario)
        => new(usuario.Id, usuario.Email, usuario.NombreCompleto, usuario.Rol, usuario.Activo);

    public static CrearUsuarioCommand ToCommand(this CrearUsuarioRequest request)
        => new(request.Email, request.NombreCompleto, request.Rol, request.Password);

    public static EditarUsuarioCommand ToCommand(this EditarUsuarioRequest request, Guid id)
        => new(id, request.NombreCompleto, request.Rol);
}
