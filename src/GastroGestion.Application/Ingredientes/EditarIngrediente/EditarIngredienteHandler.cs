using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Ingredientes;

namespace GastroGestion.Application.Ingredientes.EditarIngrediente;

public sealed class EditarIngredienteHandler
{
    private readonly IIngredienteRepository _ingredientes;
    private readonly IUnitOfWork            _uow;

    public EditarIngredienteHandler(IIngredienteRepository ingredientes, IUnitOfWork uow)
    {
        _ingredientes = ingredientes;
        _uow          = uow;
    }

    public async Task<Ingrediente> Handle(EditarIngredienteCommand cmd, CancellationToken ct = default)
    {
        var ingrediente = await _ingredientes.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException($"Ingrediente '{cmd.Id}' was not found.");

        // Pre-check name uniqueness before mutating the aggregate (ADR-CCC-1).
        var conflict = await _ingredientes.NombreExistsForOtherAsync(cmd.Nombre, cmd.Id, ct);
        if (conflict)
            throw new ConflictException($"Nombre '{cmd.Nombre}' is already assigned to another ingrediente.");

        // Domain method validates non-empty; DomainException bubbles → 422.
        ingrediente.ActualizarNombre(cmd.Nombre);

        await _uow.SaveChangesAsync(ct);

        return ingrediente;
    }
}
