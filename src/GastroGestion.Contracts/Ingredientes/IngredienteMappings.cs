using GastroGestion.Application.Ingredientes.CrearIngrediente;
using GastroGestion.Domain.Ingredientes;

namespace GastroGestion.Contracts.Ingredientes;

public static class IngredienteMappings
{
    public static CrearIngredienteCommand ToCommand(this CrearIngredienteRequest request)
        => new(request.Nombre, request.UnidadBase);

    public static IngredienteResponse ToResponse(this Ingrediente ingrediente)
        => new(ingrediente.Id, ingrediente.Nombre, ingrediente.UnidadBase, ingrediente.Activo);
}
