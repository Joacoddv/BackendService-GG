using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Platos;

namespace GastroGestion.Application.Platos.GetPlatoById;

public sealed class GetPlatoByIdHandler
{
    private readonly IPlatoRepository _platos;

    public GetPlatoByIdHandler(IPlatoRepository platos) => _platos = platos;

    public Task<Plato?> Handle(GetPlatoByIdQuery query, CancellationToken ct = default)
        => _platos.GetByIdAsync(query.Id, ct);
}
