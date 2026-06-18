using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Clientes;
using Microsoft.EntityFrameworkCore;

namespace GastroGestion.Infrastructure.Persistence.Repositories;

internal sealed class ClienteRepository : IClienteRepository
{
    private readonly GastroGestionDbContext _ctx;

    public ClienteRepository(GastroGestionDbContext ctx) => _ctx = ctx;

    public Task<Cliente?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(Cliente cliente, CancellationToken ct = default)
        => await _ctx.Clientes.AddAsync(cliente, ct);

    public async Task<IReadOnlyList<Cliente>> GetAllAsync(CancellationToken ct = default)
        => (await _ctx.Clientes.ToListAsync(ct)).AsReadOnly();

    public async Task<IReadOnlyList<Cliente>> SearchAsync(
        string? nombre,
        bool incluirInactivos,
        CancellationToken ct = default)
    {
        var query = _ctx.Clientes.AsQueryable();

        if (!incluirInactivos)
            query = query.Where(c => c.Activo);

        if (!string.IsNullOrWhiteSpace(nombre))
            query = query.Where(c => EF.Functions.Like(c.Nombre, $"%{nombre}%"));

        return (await query.ToListAsync(ct)).AsReadOnly();
    }

    public Task<bool> CuitExistsForOtherAsync(string cuit, Guid excludeId, CancellationToken ct = default)
    {
        // Cuit is stored as nvarchar(11) via an inline value converter.
        // Comparing c.Cuit == new Cuit(cuit) would work via converter translation but
        // constructs a value object (with validation) for every row. Instead, use
        // FromSqlInterpolated for a direct parameterized column comparison.
        return _ctx.Clientes
                   .FromSqlInterpolated(
                       $"SELECT * FROM Clientes WHERE Id <> {excludeId} AND Cuit = {cuit}")
                   .AnyAsync(ct);
    }
}
