namespace GastroGestion.Application.Stock.GetMovimientosStock;

/// <summary>Query for GET /stock/movimientos/{ingredienteId} — the ingredient's ledger, newest first.</summary>
public sealed record GetMovimientosStockQuery(Guid IngredienteId);
