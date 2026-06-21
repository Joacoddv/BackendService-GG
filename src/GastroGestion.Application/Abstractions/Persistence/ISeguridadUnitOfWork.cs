namespace GastroGestion.Application.Abstractions.Persistence;

/// <summary>
/// Unit of work for the security database (Usuario, RefreshToken). Separate from the domain
/// IUnitOfWork because security aggregates live in their own DbContext / physical database.
/// </summary>
public interface ISeguridadUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
