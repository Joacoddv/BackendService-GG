using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Mesas;
using Microsoft.EntityFrameworkCore;

namespace GastroGestion.Infrastructure.Persistence.Repositories;

internal sealed class MesaRepository : IMesaRepository
{
    private readonly GastroGestionDbContext _ctx;

    public MesaRepository(GastroGestionDbContext ctx) => _ctx = ctx;

    public Task<Mesa?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.Mesas.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task AddAsync(Mesa mesa, CancellationToken ct = default)
        => await _ctx.Mesas.AddAsync(mesa, ct);

    public async Task<IReadOnlyList<Mesa>> GetAllAsync(CancellationToken ct = default)
        => (await _ctx.Mesas.ToListAsync(ct)).AsReadOnly();
}
