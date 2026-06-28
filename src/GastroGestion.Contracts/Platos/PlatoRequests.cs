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

/// <summary>
/// Request DTO for editing an existing Plato (PUT /platos/{id}).
/// Recipe-line editing is out of scope — only Nombre and PrecioBase are updatable here.
/// </summary>
public sealed record EditarPlatoRequest(string Nombre, decimal PrecioBase);
