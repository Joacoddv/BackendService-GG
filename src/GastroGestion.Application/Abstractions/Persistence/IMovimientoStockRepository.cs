using GastroGestion.Domain.Stock;

namespace GastroGestion.Application.Abstractions.Persistence;

/// <summary>
/// Append-only repository for the stock ledger.
/// Intentionally exposes NO Update, Remove, or Delete — the ledger is immutable.
/// </summary>
public interface IMovimientoStockRepository
{
    Task AddAsync(MovimientoStock movimiento, CancellationToken ct = default);

    /// <summary>
    /// Returns <c>SUM(Cantidad)</c> for all movements of the given ingredient.
    /// Positive result = net available stock.
    /// </summary>
    Task<decimal> CalcularBalanceAsync(Guid ingredienteId, CancellationToken ct = default);
}
