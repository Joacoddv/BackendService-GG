using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Platos;

namespace GastroGestion.Application.Platos.GetAllPlatos;

public sealed class GetAllPlatosHandler
{
    private readonly IPlatoRepository _platos;

    public GetAllPlatosHandler(IPlatoRepository platos) => _platos = platos;

    public Task<IReadOnlyList<Plato>> Handle(GetAllPlatosQuery query, CancellationToken ct = default)
        => _platos.GetAllAsync(ct);
}
