using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Ingredientes;

public sealed record IngredienteResponse(
    Guid Id,
    string Nombre,
    UnidadDeMedida UnidadBase,
    bool Activo);
