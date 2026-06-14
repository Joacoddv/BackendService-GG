namespace GastroGestion.Application.Menus.CrearMenu;

public sealed record CrearMenuCommand(
    string Nombre,
    DateOnly FechaVigencia,
    IReadOnlyList<MenuItemInput> Items);
