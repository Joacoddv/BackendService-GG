using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Platos.CrearPlato;

public sealed record RecetaLineaInput(Guid IngredienteId, decimal Cantidad, UnidadDeMedida Unidad);
