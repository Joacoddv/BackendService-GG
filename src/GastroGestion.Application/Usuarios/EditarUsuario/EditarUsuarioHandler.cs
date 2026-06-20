using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Usuarios;

namespace GastroGestion.Application.Usuarios.EditarUsuario;

public sealed class EditarUsuarioHandler
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork        _uow;

    public EditarUsuarioHandler(IUsuarioRepository usuarios, IUnitOfWork uow)
    {
        _usuarios = usuarios;
        _uow      = uow;
    }

    public async Task<Usuario> Handle(EditarUsuarioCommand cmd, CancellationToken ct = default)
    {
        var usuario = await _usuarios.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException($"Usuario '{cmd.Id}' was not found.");

        // Domain method validates and updates mutable fields (name + role).
        // DomainException bubbles up → 422 via GastroGestionExceptionHandler.
        usuario.ActualizarDatos(cmd.NombreCompleto, cmd.Rol);

        await _uow.SaveChangesAsync(ct);

        return usuario;
    }
}
