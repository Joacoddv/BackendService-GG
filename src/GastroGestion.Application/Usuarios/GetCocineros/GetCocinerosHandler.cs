using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Usuarios;

namespace GastroGestion.Application.Usuarios.GetCocineros;

/// <summary>
/// Returns the list of active Cocinero users (CCC-A01).
/// </summary>
public sealed class GetCocinerosHandler
{
    private readonly IUsuarioRepository _usuarios;

    public GetCocinerosHandler(IUsuarioRepository usuarios) => _usuarios = usuarios;

    public Task<IReadOnlyList<Usuario>> Handle(GetCocinerosQuery query, CancellationToken ct = default)
        => _usuarios.GetByRolAsync(RolUsuario.Cocinero, ct);
}
