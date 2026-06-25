using GastroGestion.Application.Abstractions;
using GastroGestion.Domain.Bitacora;
using GastroGestion.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GastroGestion.Infrastructure.Bitacora;

/// <summary>
/// Persists <see cref="BitacoraEntry"/> rows independently of the request's unit of work.
/// </summary>
/// <remarks>
/// Scoping decision: the writer creates a FRESH DI scope (via <see cref="IServiceScopeFactory"/>)
/// and resolves a brand-new <see cref="GastroGestionDbContext"/> per audit write, rather than
/// sharing the request-scoped context. This is deliberate:
/// <list type="bullet">
///   <item>The audited request may have FAILED, leaving the request-scoped context in a faulted
///   state; reusing it could throw or persist nothing.</item>
///   <item>Calling SaveChanges on the shared context could flush unrelated tracked aggregates and
///   dispatch their domain events as a side effect of merely auditing.</item>
/// </list>
/// We use <see cref="IServiceScopeFactory"/> (already available) rather than
/// <c>IDbContextFactory</c>, because adding the factory registration alongside the existing
/// scoped <c>AddDbContext</c> registration causes a duplicate-registration conflict. The scope
/// approach is conflict-free and keeps a single DbContext registration.
/// Any exception is caught and logged — audit must never break the main flow.
/// </remarks>
internal sealed class BitacoraWriter : IBitacoraWriter
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BitacoraWriter> _logger;

    public BitacoraWriter(IServiceScopeFactory scopeFactory, ILogger<BitacoraWriter> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    public async Task RegistrarAsync(BitacoraEntry entry, CancellationToken ct = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<GastroGestionDbContext>();

            await db.BitacoraEntries.AddAsync(entry, ct);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Swallow: audit logging must never surface errors to the caller.
            _logger.LogWarning(ex, "BitacoraWriter: failed to persist audit entry for action '{Accion}'.", entry.Accion);
        }
    }
}
