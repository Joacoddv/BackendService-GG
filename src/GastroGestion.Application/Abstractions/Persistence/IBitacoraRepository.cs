using GastroGestion.Application.Bitacora.GetBitacora;

namespace GastroGestion.Application.Abstractions.Persistence;

/// <summary>
/// Read-side access to the audit log. Write-side goes through <see cref="IBitacoraWriter"/>,
/// which bypasses this repository to use its own isolated persistence scope.
/// </summary>
public interface IBitacoraRepository
{
    /// <summary>
    /// Returns a paginated, newest-first slice of audit log entries with optional filters.
    /// </summary>
    Task<GetBitacoraResult> GetPagedAsync(GetBitacoraQuery query, CancellationToken ct = default);
}
