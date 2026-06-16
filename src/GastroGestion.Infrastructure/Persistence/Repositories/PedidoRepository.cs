using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Pedidos.GetOrdenesByEstado;
using GastroGestion.Domain.Pedidos;
using GastroGestion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GastroGestion.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IPedidoRepository.
/// Owned entities (Lineas, OrdenesTrabajo) load automatically with the aggregate root.
/// </summary>
internal sealed class PedidoRepository : IPedidoRepository
{
    private readonly GastroGestionDbContext _ctx;

    public PedidoRepository(GastroGestionDbContext ctx) => _ctx = ctx;

    public async Task<Pedido?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _ctx.Pedidos.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Pedido>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
        => await _ctx.Pedidos
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(ct);

    public async Task AddAsync(Pedido pedido, CancellationToken ct = default)
        => await _ctx.Pedidos.AddAsync(pedido, ct);

    public async Task<IReadOnlyList<OrdenTrabajoBoardItem>> GetAllOrdenesTrabajoAsync(
        EstadoOT? estado, CancellationToken ct = default)
    {
        var query = _ctx.Pedidos
            .AsNoTracking()
            .SelectMany(p => p.OrdenesTrabajo, (p, ot) => new { p, ot });

        if (estado is not null)
            query = query.Where(x => x.ot.Estado == estado);
        else
            query = query.Where(x => x.ot.Estado != EstadoOT.Cancelada);

        return await query
            .Select(x => new OrdenTrabajoBoardItem(
                x.ot.Id,
                x.p.Id,
                x.p.Tipo,
                x.ot.PlatoId,
                x.ot.LineaPedidoId,
                x.ot.Estado,
                x.ot.CocineroAsignado != null ? x.ot.CocineroAsignado.Valor : (Guid?)null))
            .ToListAsync(ct);
    }
}
