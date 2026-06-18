using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Ingredientes;
using Microsoft.EntityFrameworkCore;

namespace GastroGestion.Infrastructure.Persistence.Repositories;

internal sealed class IngredienteRepository : IIngredienteRepository
{
    private readonly GastroGestionDbContext _ctx;

    public IngredienteRepository(GastroGestionDbContext ctx) => _ctx = ctx;

    public Task<Ingrediente?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.Ingredientes.FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task AddAsync(Ingrediente ingrediente, CancellationToken ct = default)
        => await _ctx.Ingredientes.AddAsync(ingrediente, ct);

    public async Task<IReadOnlyList<Ingrediente>> GetAllAsync(CancellationToken ct = default)
        => (await _ctx.Ingredientes.ToListAsync(ct)).AsReadOnly();

    public async Task<IReadOnlyList<Ingrediente>> SearchAsync(
        string? nombre,
        bool incluirInactivos,
        CancellationToken ct = default)
    {
        var query = _ctx.Ingredientes.AsQueryable();

        if (!incluirInactivos)
            query = query.Where(i => i.Activo);

        if (!string.IsNullOrWhiteSpace(nombre))
            query = query.Where(i => EF.Functions.Like(i.Nombre, $"%{nombre}%"));

        return (await query.ToListAsync(ct)).AsReadOnly();
    }

    public Task<bool> NombreExistsForOtherAsync(string nombre, Guid excludeId, CancellationToken ct = default)
        => _ctx.Ingredientes
               .AnyAsync(i => i.Id != excludeId && i.Nombre == nombre, ct);
}
