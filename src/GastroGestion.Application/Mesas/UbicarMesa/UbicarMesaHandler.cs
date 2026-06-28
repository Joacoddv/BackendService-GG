using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Mesas;

namespace GastroGestion.Application.Mesas.UbicarMesa;

public sealed class UbicarMesaHandler
{
    private readonly IMesaRepository _mesas;
    private readonly IUnitOfWork     _uow;

    public UbicarMesaHandler(IMesaRepository mesas, IUnitOfWork uow)
    {
        _mesas = mesas;
        _uow   = uow;
    }

    public async Task<Mesa> Handle(UbicarMesaCommand cmd, CancellationToken ct = default)
    {
        var mesa = await _mesas.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException($"Mesa '{cmd.Id}' was not found.");

        mesa.Ubicar(cmd.X, cmd.Y);
        await _uow.SaveChangesAsync(ct);
        return mesa;
    }
}
