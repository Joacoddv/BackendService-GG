using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Proveedores;
using Microsoft.EntityFrameworkCore;

namespace GastroGestion.Infrastructure.Persistence.Repositories;

internal sealed class ProveedorRepository : IProveedorRepository
{
    private readonly GastroGestionDbContext _ctx;

    public ProveedorRepository(GastroGestionDbContext ctx) => _ctx = ctx;

    public Task<Proveedor?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.Proveedores.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(Proveedor proveedor, CancellationToken ct = default)
        => await _ctx.Proveedores.AddAsync(proveedor, ct);

    public async Task<IReadOnlyList<Proveedor>> SearchAsync(
        string? nombre, bool incluirInactivos, CancellationToken ct = default)
    {
        var query = _ctx.Proveedores.AsQueryable();

        if (!incluirInactivos)
            query = query.Where(p => p.Activo);

        if (!string.IsNullOrWhiteSpace(nombre))
            query = query.Where(p => EF.Functions.Like(p.Nombre, $"%{nombre}%"));

        return (await query.OrderBy(p => p.Nombre).ToListAsync(ct)).AsReadOnly();
    }

    public Task<bool> CuitExistsForOtherAsync(string cuit, Guid excludeId, CancellationToken ct = default)
        => _ctx.Proveedores
               .FromSqlInterpolated(
                   $"SELECT * FROM Proveedores WHERE Id <> {excludeId} AND Cuit = {cuit}")
               .AnyAsync(ct);
}
