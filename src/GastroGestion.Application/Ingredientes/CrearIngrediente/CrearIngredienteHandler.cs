using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Ingredientes;

namespace GastroGestion.Application.Ingredientes.CrearIngrediente;

public sealed class CrearIngredienteHandler
{
    private readonly IIngredienteRepository _ingredientes;
    private readonly IUnitOfWork            _uow;

    public CrearIngredienteHandler(IIngredienteRepository ingredientes, IUnitOfWork uow)
    {
        _ingredientes = ingredientes;
        _uow          = uow;
    }

    public async Task<Guid> Handle(CrearIngredienteCommand cmd, CancellationToken ct = default)
    {
        var ingrediente = Ingrediente.Crear(cmd.Nombre, cmd.UnidadBase);

        await _ingredientes.AddAsync(ingrediente, ct);
        await _uow.SaveChangesAsync(ct);

        return ingrediente.Id;
    }
}
