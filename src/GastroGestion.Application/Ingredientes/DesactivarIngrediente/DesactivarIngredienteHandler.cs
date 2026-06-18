using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;

namespace GastroGestion.Application.Ingredientes.DesactivarIngrediente;

public sealed class DesactivarIngredienteHandler
{
    private readonly IIngredienteRepository _ingredientes;
    private readonly IUnitOfWork            _uow;

    public DesactivarIngredienteHandler(IIngredienteRepository ingredientes, IUnitOfWork uow)
    {
        _ingredientes = ingredientes;
        _uow          = uow;
    }

    public async Task Handle(DesactivarIngredienteCommand cmd, CancellationToken ct = default)
    {
        var ingrediente = await _ingredientes.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException($"Ingrediente '{cmd.Id}' was not found.");

        // Desactivar() is idempotent — calling on an already-inactive ingrediente is a no-op.
        ingrediente.Desactivar();

        await _uow.SaveChangesAsync(ct);
    }
}
