using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Mesas;

namespace GastroGestion.Application.Mesas.CrearMesa;

public sealed class CrearMesaHandler
{
    private readonly IMesaRepository _mesas;
    private readonly IUnitOfWork     _uow;

    public CrearMesaHandler(IMesaRepository mesas, IUnitOfWork uow)
    {
        _mesas = mesas;
        _uow   = uow;
    }

    public async Task<Guid> Handle(CrearMesaCommand cmd, CancellationToken ct = default)
    {
        var mesa = Mesa.Crear(cmd.Numero, cmd.Capacidad);

        await _mesas.AddAsync(mesa, ct);
        await _uow.SaveChangesAsync(ct);

        return mesa.Id;
    }
}
