using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Ingredientes;

public sealed record CrearIngredienteRequest(string Nombre, UnidadDeMedida UnidadBase);
