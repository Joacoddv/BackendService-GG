using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;

namespace GastroGestion.Application.Mesas.DesactivarMesa;

public sealed class DesactivarMesaHandler
{
    private readonly IMesaRepository _mesas;
    private readonly IUnitOfWork     _uow;

    public DesactivarMesaHandler(IMesaRepository mesas, IUnitOfWork uow)
    {
        _mesas = mesas;
        _uow   = uow;
    }

    public async Task Handle(DesactivarMesaCommand cmd, CancellationToken ct = default)
    {
        var mesa = await _mesas.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException($"Mesa '{cmd.Id}' was not found.");

        // Desactivar() throws DomainException (→ 422) if the table has an active Pedido.
        mesa.Desactivar();

        await _uow.SaveChangesAsync(ct);
    }
}
