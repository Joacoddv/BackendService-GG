using GastroGestion.Application.Abstractions.Persistence;

namespace GastroGestion.Infrastructure.Persistence;

/// <summary>Commits changes tracked by the SeguridadDbContext.</summary>
internal sealed class SeguridadUnitOfWork : ISeguridadUnitOfWork
{
    private readonly SeguridadDbContext _ctx;

    public SeguridadUnitOfWork(SeguridadDbContext ctx) => _ctx = ctx;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _ctx.SaveChangesAsync(ct);
}
