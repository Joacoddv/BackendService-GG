using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Mesas;

namespace GastroGestion.Application.Mesas.GetMesaById;

public sealed class GetMesaByIdHandler
{
    private readonly IMesaRepository _mesas;

    public GetMesaByIdHandler(IMesaRepository mesas) => _mesas = mesas;

    public Task<Mesa?> Handle(GetMesaByIdQuery query, CancellationToken ct = default)
        => _mesas.GetByIdAsync(query.Id, ct);
}
