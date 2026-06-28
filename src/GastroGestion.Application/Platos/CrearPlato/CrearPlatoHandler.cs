using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Platos;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Application.Platos.CrearPlato;

public sealed class CrearPlatoHandler
{
    private readonly IPlatoRepository       _platos;
    private readonly IIngredienteRepository _ingredientes;
    private readonly IUnitOfWork            _uow;

    public CrearPlatoHandler(IPlatoRepository platos, IIngredienteRepository ingredientes, IUnitOfWork uow)
    {
        _platos       = platos;
        _ingredientes = ingredientes;
        _uow          = uow;
    }

    public async Task<Guid> Handle(CrearPlatoCommand cmd, CancellationToken ct = default)
    {
        var plato = Plato.Crear(cmd.Nombre, new Dinero(cmd.PrecioBase), cmd.AlicuotaIVA);

        // Each recipe line must be expressed in its ingredient's base unit (no conversion table),
        // so load the referenced ingrediente to pass its UnidadBase into the domain guard.
        // Cache by id to avoid re-fetching an ingredient referenced by more than one line.
        var cache = new Dictionary<Guid, Domain.Ingredientes.Ingrediente>();
        foreach (var line in cmd.Lineas)
        {
            if (!cache.TryGetValue(line.IngredienteId, out var ingrediente))
            {
                ingrediente = await _ingredientes.GetByIdAsync(line.IngredienteId, ct)
                    ?? throw new NotFoundException($"Ingrediente '{line.IngredienteId}' was not found.");
                cache[line.IngredienteId] = ingrediente;
            }

            plato.AgregarLineaReceta(
                line.IngredienteId,
                ingrediente.UnidadBase,
                new Cantidad(line.Cantidad, line.Unidad));
        }

        await _platos.AddAsync(plato, ct);
        await _uow.SaveChangesAsync(ct);

        return plato.Id;
    }
}
