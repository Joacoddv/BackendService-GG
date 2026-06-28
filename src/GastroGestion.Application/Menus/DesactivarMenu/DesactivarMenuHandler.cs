using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;

namespace GastroGestion.Application.Menus.DesactivarMenu;

public sealed class DesactivarMenuHandler
{
    private readonly IMenuRepository _menus;
    private readonly IUnitOfWork     _uow;

    public DesactivarMenuHandler(IMenuRepository menus, IUnitOfWork uow)
    {
        _menus = menus;
        _uow   = uow;
    }

    public async Task Handle(DesactivarMenuCommand cmd, CancellationToken ct = default)
    {
        var menu = await _menus.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException($"Menu '{cmd.Id}' was not found.");

        // Desactivar() is idempotent — calling on an already-inactive menu is a no-op.
        menu.Desactivar();

        await _uow.SaveChangesAsync(ct);
    }
}
