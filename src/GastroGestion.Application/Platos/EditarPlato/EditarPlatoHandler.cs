using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Platos;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Application.Platos.EditarPlato;

public sealed class EditarPlatoHandler
{
    private readonly IPlatoRepository _platos;
    private readonly IUnitOfWork      _uow;

    public EditarPlatoHandler(IPlatoRepository platos, IUnitOfWork uow)
    {
        _platos = platos;
        _uow    = uow;
    }

    public async Task<Plato> Handle(EditarPlatoCommand cmd, CancellationToken ct = default)
    {
        var plato = await _platos.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException($"Plato '{cmd.Id}' was not found.");

        // Domain methods validate; DomainException bubbles → 422.
        plato.Renombrar(cmd.Nombre);
        plato.ActualizarPrecio(new Dinero(cmd.PrecioBase));

        await _uow.SaveChangesAsync(ct);

        return plato;
    }
}
