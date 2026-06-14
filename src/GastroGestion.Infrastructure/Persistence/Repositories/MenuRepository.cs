using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Menus;
using Microsoft.EntityFrameworkCore;

namespace GastroGestion.Infrastructure.Persistence.Repositories;

internal sealed class MenuRepository : IMenuRepository
{
    private readonly GastroGestionDbContext _ctx;

    public MenuRepository(GastroGestionDbContext ctx) => _ctx = ctx;

    public Task<Menu?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.Menus.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task AddAsync(Menu menu, CancellationToken ct = default)
        => await _ctx.Menus.AddAsync(menu, ct);
}
