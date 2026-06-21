using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace GastroGestion.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of IRefreshTokenRepository. Mirrors the other repositories.</summary>
internal sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly GastroGestionDbContext _ctx;

    public RefreshTokenRepository(GastroGestionDbContext ctx) => _ctx = ctx;

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
        => await _ctx.RefreshTokens.AddAsync(token, ct);

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
        => _ctx.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
}
