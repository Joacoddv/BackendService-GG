using GastroGestion.Domain.Usuarios;

namespace GastroGestion.Contracts.Usuarios;

/// <summary>
/// Extension methods for mapping Usuario domain entities to contract DTOs (CCC-A01).
/// </summary>
public static class UsuarioMappings
{
    public static CocineroResponse ToCocineroResponse(this Usuario usuario)
        => new(usuario.Id, usuario.NombreCompleto);
}
