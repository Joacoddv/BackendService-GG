using GastroGestion.Application.Ingredientes.ActualizarStockMinimo;
using GastroGestion.Application.Ingredientes.CrearIngrediente;
using GastroGestion.Application.Ingredientes.EditarIngrediente;
using GastroGestion.Domain.Ingredientes;

namespace GastroGestion.Contracts.Ingredientes;

public static class IngredienteMappings
{
    public static CrearIngredienteCommand ToCommand(this CrearIngredienteRequest request)
        => new(request.Nombre, request.UnidadBase);

    public static EditarIngredienteCommand ToCommand(this EditarIngredienteRequest request, Guid id)
        => new(id, request.Nombre);

    public static ActualizarStockMinimoCommand ToCommand(this ActualizarStockMinimoRequest request, Guid id)
        => new(id, request.StockMinimo);

    public static IngredienteResponse ToResponse(this Ingrediente ingrediente)
        => new(ingrediente.Id, ingrediente.Nombre, ingrediente.UnidadBase, ingrediente.Activo);
}
