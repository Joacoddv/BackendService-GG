using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Platos;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Application.Platos.CrearPlato;

public sealed class CrearPlatoHandler
{
    private readonly IPlatoRepository _platos;
    private readonly IUnitOfWork      _uow;

    public CrearPlatoHandler(IPlatoRepository platos, IUnitOfWork uow)
    {
        _platos = platos;
        _uow    = uow;
    }

    public async Task<Guid> Handle(CrearPlatoCommand cmd, CancellationToken ct = default)
    {
        var plato = Plato.Crear(cmd.Nombre, new Dinero(cmd.PrecioBase), cmd.AlicuotaIVA);

        foreach (var line in cmd.Lineas)
            plato.AgregarLineaReceta(line.IngredienteId, new Cantidad(line.Cantidad, line.Unidad));

        await _platos.AddAsync(plato, ct);
        await _uow.SaveChangesAsync(ct);

        return plato.Id;
    }
}
