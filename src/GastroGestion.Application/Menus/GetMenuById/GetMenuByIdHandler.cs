using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Menus;

namespace GastroGestion.Application.Menus.GetMenuById;

public sealed class GetMenuByIdHandler
{
    private readonly IMenuRepository _menus;

    public GetMenuByIdHandler(IMenuRepository menus) => _menus = menus;

    public Task<Menu?> Handle(GetMenuByIdQuery query, CancellationToken ct = default)
        => _menus.GetByIdAsync(query.Id, ct);
}
