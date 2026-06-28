using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Platos;
using Microsoft.EntityFrameworkCore;

namespace GastroGestion.Infrastructure.Persistence.Repositories;

internal sealed class PlatoRepository : IPlatoRepository
{
    private readonly GastroGestionDbContext _ctx;

    public PlatoRepository(GastroGestionDbContext ctx) => _ctx = ctx;

    public Task<Plato?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.Platos.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(Plato plato, CancellationToken ct = default)
        => await _ctx.Platos.AddAsync(plato, ct);

    public async Task<IReadOnlyList<Plato>> GetAllAsync(CancellationToken ct = default)
        => (await _ctx.Platos.ToListAsync(ct)).AsReadOnly();

    public async Task<IReadOnlyList<Plato>> GetActivosAsync(CancellationToken ct = default)
        => (await _ctx.Platos.Where(p => p.Activo).ToListAsync(ct)).AsReadOnly();

    public async Task<IReadOnlyList<Plato>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        => await _ctx.Platos
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(ct);
}
