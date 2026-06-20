using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace GastroGestion.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IUsuarioRepository.
/// Mirrors the ClienteRepository pattern (internal sealed class, ctor-injected context).
/// </summary>
internal sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly GastroGestionDbContext _ctx;

    public UsuarioRepository(GastroGestionDbContext ctx) => _ctx = ctx;

    public Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default)
        => _ctx.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> AnyAsync(CancellationToken ct = default)
        => _ctx.Usuarios.AnyAsync(ct);

    public async Task AddAsync(Usuario usuario, CancellationToken ct = default)
        => await _ctx.Usuarios.AddAsync(usuario, ct);

    public async Task<IReadOnlyList<Usuario>> GetByRolAsync(RolUsuario rol, CancellationToken ct = default)
    {
        var list = await _ctx.Usuarios
            .Where(u => u.Rol == rol && u.Activo)
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public Task<Usuario?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<IReadOnlyList<Usuario>> SearchAsync(
        string? nombre,
        RolUsuario? rol,
        bool incluirInactivos,
        CancellationToken ct = default)
    {
        var query = _ctx.Usuarios.AsQueryable();

        if (!incluirInactivos)
            query = query.Where(u => u.Activo);

        if (rol is not null)
            query = query.Where(u => u.Rol == rol);

        if (!string.IsNullOrWhiteSpace(nombre))
            query = query.Where(u => EF.Functions.Like(u.NombreCompleto, $"%{nombre}%"));

        return (await query.ToListAsync(ct)).AsReadOnly();
    }
}
