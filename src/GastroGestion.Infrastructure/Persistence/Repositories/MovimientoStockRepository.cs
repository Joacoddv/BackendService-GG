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
}
