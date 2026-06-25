using GastroGestion.Domain.Bitacora;

namespace GastroGestion.Application.Abstractions;

/// <summary>
/// Writes audit log entries to the persistent store.
/// Implementations must swallow exceptions — audit logging must never break the primary flow.
/// </summary>
public interface IBitacoraWriter
{
    /// <summary>
    /// Persists a <see cref="BitacoraEntry"/> asynchronously.
    /// Must not throw — any persistence errors should be logged and suppressed internally.
    /// </summary>
    Task RegistrarAsync(BitacoraEntry entry, CancellationToken ct = default);
}
