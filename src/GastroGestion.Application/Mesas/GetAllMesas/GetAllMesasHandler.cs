using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Mesas;

namespace GastroGestion.Application.Mesas.GetAllMesas;

public sealed class GetAllMesasHandler
{
    private readonly IMesaRepository _mesas;

    public GetAllMesasHandler(IMesaRepository mesas) => _mesas = mesas;

    public Task<IReadOnlyList<Mesa>> Handle(GetAllMesasQuery query, CancellationToken ct = default)
        => _mesas.GetAllAsync(ct);
}
