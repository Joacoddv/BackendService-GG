namespace GastroGestion.Contracts.Menus;

public sealed record CrearMenuRequest(
    string Nombre,
    DateOnly FechaVigencia,
    MenuItemRequest[] Items);

public sealed record MenuItemRequest(Guid PlatoId, decimal? PrecioOverride);
