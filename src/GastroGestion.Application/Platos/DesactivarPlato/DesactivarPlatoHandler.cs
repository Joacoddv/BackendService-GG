using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;

namespace GastroGestion.Application.Platos.DesactivarPlato;

public sealed class DesactivarPlatoHandler
{
    private readonly IPlatoRepository _platos;
    private readonly IUnitOfWork      _uow;

    public DesactivarPlatoHandler(IPlatoRepository platos, IUnitOfWork uow)
    {
        _platos = platos;
        _uow    = uow;
    }

    public async Task Handle(DesactivarPlatoCommand cmd, CancellationToken ct = default)
    {
        var plato = await _platos.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException($"Plato '{cmd.Id}' was not found.");

        // Desactivar() is idempotent — calling on an already-inactive plato is a no-op.
        plato.Desactivar();

        await _uow.SaveChangesAsync(ct);
    }
}
