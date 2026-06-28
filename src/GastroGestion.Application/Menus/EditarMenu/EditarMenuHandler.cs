using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Menus;

namespace GastroGestion.Application.Menus.EditarMenu;

public sealed class EditarMenuHandler
{
    private readonly IMenuRepository _menus;
    private readonly IUnitOfWork     _uow;

    public EditarMenuHandler(IMenuRepository menus, IUnitOfWork uow)
    {
        _menus = menus;
        _uow   = uow;
    }

    public async Task<Menu> Handle(EditarMenuCommand cmd, CancellationToken ct = default)
    {
        var menu = await _menus.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException($"Menu '{cmd.Id}' was not found.");

        // Domain methods validate; DomainException bubbles → 422.
        menu.Renombrar(cmd.Nombre);
        menu.CambiarFechaVigencia(cmd.FechaVigencia);

        await _uow.SaveChangesAsync(ct);

        return menu;
    }
}
