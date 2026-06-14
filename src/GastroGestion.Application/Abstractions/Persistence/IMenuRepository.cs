using GastroGestion.Domain.Menus;

namespace GastroGestion.Application.Abstractions.Persistence;

public interface IMenuRepository
{
    Task<Menu?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Menu menu, CancellationToken ct = default);
}
