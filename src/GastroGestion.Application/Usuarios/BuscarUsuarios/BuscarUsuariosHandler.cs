using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Usuarios;

namespace GastroGestion.Application.Usuarios.BuscarUsuarios;

public sealed class BuscarUsuariosHandler
{
    private readonly IUsuarioRepository _usuarios;

    public BuscarUsuariosHandler(IUsuarioRepository usuarios) => _usuarios = usuarios;

    public Task<IReadOnlyList<Usuario>> Handle(BuscarUsuariosQuery query, CancellationToken ct = default)
        => _usuarios.SearchAsync(query.Nombre, query.Rol, query.IncluirInactivos, ct);
}
