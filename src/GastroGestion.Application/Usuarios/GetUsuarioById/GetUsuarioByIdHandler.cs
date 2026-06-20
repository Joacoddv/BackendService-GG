using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Usuarios;

namespace GastroGestion.Application.Usuarios.GetUsuarioById;

public sealed class GetUsuarioByIdHandler
{
    private readonly IUsuarioRepository _usuarios;

    public GetUsuarioByIdHandler(IUsuarioRepository usuarios) => _usuarios = usuarios;

    public Task<Usuario?> Handle(GetUsuarioByIdQuery query, CancellationToken ct = default)
        => _usuarios.GetByIdAsync(query.Id, ct);
}
