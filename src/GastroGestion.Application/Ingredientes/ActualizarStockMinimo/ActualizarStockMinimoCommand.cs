namespace GastroGestion.Application.Ingredientes.ActualizarStockMinimo;

/// <summary>Command to set an ingrediente's reorder threshold (low-stock alert point).</summary>
public sealed record ActualizarStockMinimoCommand(Guid IngredienteId, decimal StockMinimo);
