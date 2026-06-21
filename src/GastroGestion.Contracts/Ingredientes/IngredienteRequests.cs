using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Ingredientes;

public sealed record CrearIngredienteRequest(string Nombre, UnidadDeMedida UnidadBase);

/// <summary>
/// Request DTO for editing an existing Ingrediente.
/// Used by PUT /ingredientes/{id}. UnidadBase is intentionally absent — it is
/// immutable after creation (ADR-CCC-1). Any attempt to change it via this DTO
/// is structurally impossible, not rejected with 422.
/// </summary>
/// <param name="Nombre">New display name — required, non-empty.</param>
public sealed record EditarIngredienteRequest(string Nombre);

/// <summary>Request DTO for PUT /ingredientes/{id}/stock-minimo — sets the reorder threshold.</summary>
public sealed record ActualizarStockMinimoRequest(decimal StockMinimo);
