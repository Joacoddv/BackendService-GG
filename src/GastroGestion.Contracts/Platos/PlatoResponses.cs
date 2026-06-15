using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Platos;

public sealed record PlatoResponse(
    Guid Id,
    string Nombre,
    decimal PrecioBase,
    string Moneda,
    AlicuotaIVA AlicuotaIVA,
    bool Activo,
    RecetaLineaResponse[] Receta);

public sealed record RecetaLineaResponse(
    Guid Id,
    Guid IngredienteId,
    decimal Cantidad,
    UnidadDeMedida Unidad);
