using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Enums;
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

    /// <summary>
    /// Returns a read-only list of Facturas ordered newest-first.
    /// Applies estado and clienteId filters only when the nullable arg has a value.
    /// </summary>
    public async Task<IReadOnlyList<Factura>> ListAsync(
        EstadoFactura? estado,
        Guid? clienteId,
        CancellationToken ct = default)
    {
        var query = _ctx.Facturas.AsNoTracking();

        if (estado.HasValue)
            query = query.Where(f => f.Estado == estado.Value);

        if (clienteId.HasValue)
            query = query.Where(f => f.ClienteId == clienteId.Value);

        return await query
            .OrderByDescending(f => f.FechaAlta)
            .ToListAsync(ct);
    }
}
