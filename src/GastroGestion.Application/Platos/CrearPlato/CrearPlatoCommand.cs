using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Platos.CrearPlato;

public sealed record CrearPlatoCommand(
    string Nombre,
    decimal PrecioBase,
    AlicuotaIVA AlicuotaIVA,
    IReadOnlyList<RecetaLineaInput> Lineas);
