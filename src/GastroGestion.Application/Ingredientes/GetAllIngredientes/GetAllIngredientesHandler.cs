using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Ingredientes;

namespace GastroGestion.Application.Ingredientes.GetAllIngredientes;

public sealed class GetAllIngredientesHandler
{
    private readonly IIngredienteRepository _ingredientes;

    public GetAllIngredientesHandler(IIngredienteRepository ingredientes) => _ingredientes = ingredientes;

    public Task<IReadOnlyList<Ingrediente>> Handle(GetAllIngredientesQuery query, CancellationToken ct = default)
        => _ingredientes.GetAllAsync(ct);
}
