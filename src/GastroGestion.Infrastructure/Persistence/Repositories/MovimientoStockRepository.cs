using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Stock;
using GastroGestion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GastroGestion.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IMovimientoStockRepository.
/// Append-only: exposes only AddAsync and balance query — no Update or Remove.
/// </summary>
internal sealed class MovimientoStockRepository : IMovimientoStockRepository
{
    private readonly GastroGestionDbContext _ctx;

    public MovimientoStockRepository(GastroGestionDbContext ctx) => _ctx = ctx;

    public async Task AddAsync(MovimientoStock movimiento, CancellationToken ct = default)
        => await _ctx.MovimientosStock.AddAsync(movimiento, ct);

    public async Task<decimal> CalcularBalanceAsync(Guid ingredienteId, CancellationToken ct = default)
        => await _ctx.MovimientosStock
            .Where(m => m.IngredienteId == ingredienteId)
            .SumAsync(m => m.Cantidad, ct);

    public async Task<IReadOnlyList<MovimientoStock>> GetByIngredienteAsync(Guid ingredienteId, CancellationToken ct = default)
        => await _ctx.MovimientosStock
            .Where(m => m.IngredienteId == ingredienteId)
            .OrderByDescending(m => m.FechaMovimiento)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, decimal>> CalcularBalancesAsync(CancellationToken ct = default)
    {
        var rows = await _ctx.MovimientosStock
            .GroupBy(m => m.IngredienteId)
            .Select(g => new { IngredienteId = g.Key, Balance = g.Sum(x => x.Cantidad) })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.IngredienteId, r => r.Balance);
    }
}
