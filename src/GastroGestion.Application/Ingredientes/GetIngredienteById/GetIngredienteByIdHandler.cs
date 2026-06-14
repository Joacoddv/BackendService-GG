using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Ingredientes;

namespace GastroGestion.Application.Ingredientes.GetIngredienteById;

public sealed class GetIngredienteByIdHandler
{
    private readonly IIngredienteRepository _ingredientes;

    public GetIngredienteByIdHandler(IIngredienteRepository ingredientes) => _ingredientes = ingredientes;

    public Task<Ingrediente?> Handle(GetIngredienteByIdQuery query, CancellationToken ct = default)
        => _ingredientes.GetByIdAsync(query.Id, ct);
}
