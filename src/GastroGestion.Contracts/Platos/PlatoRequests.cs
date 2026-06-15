using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Platos;

public sealed record CrearPlatoRequest(
    string Nombre,
    decimal PrecioBase,
    AlicuotaIVA AlicuotaIVA,
    RecetaLineaRequest[] Lineas);

public sealed record RecetaLineaRequest(
    Guid IngredienteId,
    decimal Cantidad,
    UnidadDeMedida Unidad);
