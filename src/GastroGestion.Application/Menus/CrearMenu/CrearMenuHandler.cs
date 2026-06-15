using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Menus;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Application.Menus.CrearMenu;

public sealed class CrearMenuHandler
{
    private readonly IMenuRepository _menus;
    private readonly IUnitOfWork     _uow;

    public CrearMenuHandler(IMenuRepository menus, IUnitOfWork uow)
    {
        _menus = menus;
        _uow   = uow;
    }

    public async Task<Guid> Handle(CrearMenuCommand cmd, CancellationToken ct = default)
    {
        // CRITICAL: real factory param is FechaVigencia (not fechaMenu)
        var menu = Menu.Crear(cmd.Nombre, cmd.FechaVigencia);

        foreach (var item in cmd.Items)
        {
            var precioOverride = item.PrecioOverride is null ? null : new Dinero(item.PrecioOverride.Value);
            menu.AgregarItem(item.PlatoId, precioOverride);
        }

        await _menus.AddAsync(menu, ct);
        await _uow.SaveChangesAsync(ct);

        return menu.Id;
    }
}
