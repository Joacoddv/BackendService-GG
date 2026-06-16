using GastroGestion.Application.Abstractions.Persistence;
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
}
