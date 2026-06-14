using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Facturacion;
using Microsoft.EntityFrameworkCore;

namespace GastroGestion.Infrastructure.Persistence.Repositories;

internal sealed class FacturaRepository : IFacturaRepository
{
    private readonly GastroGestionDbContext _ctx;

    public FacturaRepository(GastroGestionDbContext ctx) => _ctx = ctx;

    /// <summary>
    /// Loads a Factura by Id. Owned entities (Lineas, Pagos) load automatically
    /// because they are configured as OwnedMany on the root entity.
    /// </summary>
    public Task<Factura?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.Facturas.FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task AddAsync(Factura factura, CancellationToken ct = default)
        => await _ctx.Facturas.AddAsync(factura, ct);
}
