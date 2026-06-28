using GastroGestion.Application.Platos.CrearPlato;
using GastroGestion.Application.Platos.EditarPlato;
using GastroGestion.Domain.Platos;

namespace GastroGestion.Contracts.Platos;

public static class PlatoMappings
{
    public static EditarPlatoCommand ToCommand(this EditarPlatoRequest request, Guid id)
        => new(id, request.Nombre, request.PrecioBase);

    public static CrearPlatoCommand ToCommand(this CrearPlatoRequest request)
        => new(
            request.Nombre,
            request.PrecioBase,
            request.AlicuotaIVA,
            request.Lineas
                .Select(l => new RecetaLineaInput(l.IngredienteId, l.Cantidad, l.Unidad))
                .ToList()
                .AsReadOnly());

    public static PlatoResponse ToResponse(this Plato plato)
        => new(
            plato.Id,
            plato.Nombre,
            plato.PrecioBase.Monto,
            plato.PrecioBase.Moneda.ToString(),
            plato.AlicuotaIVA,
            plato.Activo,
            plato.LineasReceta
                .Select(l => new RecetaLineaResponse(l.Id, l.IngredienteId, l.Cantidad.Valor, l.Cantidad.Unidad))
                .ToArray());
}
