using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Mesas;

namespace GastroGestion.Application.Mesas.EditarMesa;

public sealed class EditarMesaHandler
{
    private readonly IMesaRepository _mesas;
    private readonly IUnitOfWork     _uow;

    public EditarMesaHandler(IMesaRepository mesas, IUnitOfWork uow)
    {
        _mesas = mesas;
        _uow   = uow;
    }

    public async Task<Mesa> Handle(EditarMesaCommand cmd, CancellationToken ct = default)
    {
        var mesa = await _mesas.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException($"Mesa '{cmd.Id}' was not found.");

        // Pre-check Numero uniqueness before mutating the aggregate.
        var conflict = await _mesas.NumeroExistsForOtherAsync(cmd.Numero, cmd.Id, ct);
        if (conflict)
            throw new ConflictException($"Numero '{cmd.Numero}' is already assigned to another mesa.");

        // Domain method validates; DomainException bubbles → 422.
        mesa.Actualizar(cmd.Numero, cmd.Capacidad);

        await _uow.SaveChangesAsync(ct);

        return mesa;
    }
}
