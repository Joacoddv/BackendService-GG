using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Abstractions.Security;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Usuarios;

namespace GastroGestion.Application.Usuarios.CrearUsuario;

public sealed class CrearUsuarioHandler
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordHasher    _hasher;
    private readonly ISeguridadUnitOfWork        _uow;

    public CrearUsuarioHandler(IUsuarioRepository usuarios, IPasswordHasher hasher, ISeguridadUnitOfWork uow)
    {
        _usuarios = usuarios;
        _hasher   = hasher;
        _uow      = uow;
    }

    public async Task<Guid> Handle(CrearUsuarioCommand cmd, CancellationToken ct = default)
    {
        // Email is the login identity — it must be unique.
        var existing = await _usuarios.GetByEmailAsync(cmd.Email, ct);
        if (existing is not null)
            throw new ConflictException($"Email '{cmd.Email}' is already registered.");

        // Hash needs a Usuario instance; build a placeholder, hash, then create the real one
        // (same two-step the seeder uses, since the hasher salts per-user).
        var placeholder = Usuario.Crear(cmd.Email, cmd.NombreCompleto, cmd.Rol, "placeholder");
        var hash        = _hasher.Hash(placeholder, cmd.Password);

        var usuario = Usuario.Crear(cmd.Email, cmd.NombreCompleto, cmd.Rol, hash);

        await _usuarios.AddAsync(usuario, ct);
        await _uow.SaveChangesAsync(ct);

        return usuario.Id;
    }
}
