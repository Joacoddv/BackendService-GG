namespace GastroGestion.Contracts.Menus;

public sealed record MenuResponse(
    Guid Id,
    string Nombre,
    DateOnly FechaVigencia,
    bool Activo,
    MenuItemResponse[] Items);

public sealed record MenuItemResponse(
    Guid Id,
    Guid PlatoId,
    decimal? PrecioOverride);
