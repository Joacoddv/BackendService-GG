using GastroGestion.Application.Menus.CrearMenu;
using GastroGestion.Application.Menus.EditarMenu;
using GastroGestion.Domain.Menus;

namespace GastroGestion.Contracts.Menus;

public static class MenuMappings
{
    public static EditarMenuCommand ToCommand(this EditarMenuRequest request, Guid id)
        => new(id, request.Nombre, request.FechaVigencia);

    public static CrearMenuCommand ToCommand(this CrearMenuRequest request)
        => new(
            request.Nombre,
            request.FechaVigencia,
            request.Items
                .Select(i => new MenuItemInput(i.PlatoId, i.PrecioOverride))
                .ToList()
                .AsReadOnly());

    public static MenuResponse ToResponse(this Menu menu)
        => new(
            menu.Id,
            menu.Nombre,
            menu.FechaVigencia,
            menu.Activo,
            menu.Items
                .Select(i => new MenuItemResponse(i.Id, i.PlatoId, i.PrecioOverride?.Monto))
                .ToArray());
}
