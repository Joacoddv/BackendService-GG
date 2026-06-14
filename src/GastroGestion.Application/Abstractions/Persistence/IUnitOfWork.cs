namespace GastroGestion.Application.Abstractions.Persistence;

/// <summary>
/// Atomic commit boundary. Delegates to the underlying DbContext SaveChanges override
/// which runs the append-only guard and post-commit domain event dispatch.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
