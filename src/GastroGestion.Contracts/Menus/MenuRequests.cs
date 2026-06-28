namespace GastroGestion.Contracts.Menus;

public sealed record CrearMenuRequest(
    string Nombre,
    DateOnly FechaVigencia,
    MenuItemRequest[] Items);

public sealed record MenuItemRequest(Guid PlatoId, decimal? PrecioOverride);

/// <summary>
/// Request DTO for editing an existing Menu (PUT /menus/{id}).
/// Item editing is out of scope — only Nombre and FechaVigencia are updatable here.
/// </summary>
public sealed record EditarMenuRequest(string Nombre, DateOnly FechaVigencia);
