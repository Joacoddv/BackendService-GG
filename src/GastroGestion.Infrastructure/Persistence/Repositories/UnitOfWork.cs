using GastroGestion.Application.Abstractions.Persistence;

namespace GastroGestion.Infrastructure.Persistence.Repositories;

/// <summary>
/// Delegates to GastroGestionDbContext.SaveChangesAsync which runs the
/// append-only guard and post-commit domain event dispatch.
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly GastroGestionDbContext _ctx;

    public UnitOfWork(GastroGestionDbContext ctx) => _ctx = ctx;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _ctx.SaveChangesAsync(ct);
}
