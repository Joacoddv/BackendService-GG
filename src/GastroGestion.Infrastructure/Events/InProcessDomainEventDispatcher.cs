using GastroGestion.Application.Abstractions.Events;
using GastroGestion.Domain.Common;

namespace GastroGestion.Infrastructure.Events;

/// <summary>
/// Minimal in-process post-commit dispatcher for Phase 3.
/// No handlers registered yet; the seam is wired so Phase 5 can register handlers
/// (e.g. FacturaNecesitaCAE) without touching the DbContext.
/// </summary>
public sealed class InProcessDomainEventDispatcher : IDomainEventDispatcher
{
    public Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct = default)
        => Task.CompletedTask; // no handlers in Phase 3; seam established
}
