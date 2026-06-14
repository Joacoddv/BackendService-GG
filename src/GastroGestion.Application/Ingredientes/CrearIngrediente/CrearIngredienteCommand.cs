using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Ingredientes.CrearIngrediente;

public sealed record CrearIngredienteCommand(string Nombre, UnidadDeMedida UnidadBase);
