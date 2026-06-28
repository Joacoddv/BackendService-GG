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

    /// <summary>Returns the ledger movements for an ingredient, newest first.</summary>
    Task<IReadOnlyList<MovimientoStock>> GetByIngredienteAsync(Guid ingredienteId, CancellationToken ct = default);

    /// <summary>
    /// Returns the net balance (SUM of all movements) for EVERY ingredient in a single query.
    /// Use this instead of looping over <see cref="CalcularBalanceAsync"/> to avoid N+1.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, decimal>> CalcularBalancesAsync(CancellationToken ct = default);
}
