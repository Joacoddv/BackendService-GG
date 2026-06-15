using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Menus;

namespace GastroGestion.Application.Menus.GetAllMenus;

public sealed class GetAllMenusHandler
{
    private readonly IMenuRepository _menus;

    public GetAllMenusHandler(IMenuRepository menus) => _menus = menus;

    public Task<IReadOnlyList<Menu>> Handle(GetAllMenusQuery query, CancellationToken ct = default)
        => _menus.GetAllAsync(ct);
}
