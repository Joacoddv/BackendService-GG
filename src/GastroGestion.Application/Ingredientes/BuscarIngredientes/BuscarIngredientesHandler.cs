using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Ingredientes;

namespace GastroGestion.Application.Ingredientes.BuscarIngredientes;

public sealed class BuscarIngredientesHandler
{
    private readonly IIngredienteRepository _ingredientes;

    public BuscarIngredientesHandler(IIngredienteRepository ingredientes)
        => _ingredientes = ingredientes;

    public Task<IReadOnlyList<Ingrediente>> Handle(BuscarIngredientesQuery query, CancellationToken ct = default)
        => _ingredientes.SearchAsync(query.Nombre, query.IncluirInactivos, ct);
}
