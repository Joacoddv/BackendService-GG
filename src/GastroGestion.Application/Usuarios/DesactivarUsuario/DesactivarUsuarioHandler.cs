using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;

namespace GastroGestion.Application.Usuarios.DesactivarUsuario;

public sealed class DesactivarUsuarioHandler
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork        _uow;

    public DesactivarUsuarioHandler(IUsuarioRepository usuarios, IUnitOfWork uow)
    {
        _usuarios = usuarios;
        _uow      = uow;
    }

    public async Task Handle(DesactivarUsuarioCommand cmd, CancellationToken ct = default)
    {
        var usuario = await _usuarios.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException($"Usuario '{cmd.Id}' was not found.");

        // Desactivar() is idempotent — calling on an already-inactive user is a no-op.
        usuario.Desactivar();

        await _uow.SaveChangesAsync(ct);
    }
}
