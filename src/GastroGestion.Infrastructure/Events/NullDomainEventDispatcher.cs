using GastroGestion.Application.Abstractions.Events;
using GastroGestion.Domain.Common;

namespace GastroGestion.Infrastructure.Events;

/// <summary>
/// No-op dispatcher used at design time (IDesignTimeDbContextFactory).
/// Never dispatches events; exists solely so the factory can construct a valid DbContext.
/// </summary>
public sealed class NullDomainEventDispatcher : IDomainEventDispatcher
{
    public Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct = default)
        => Task.CompletedTask;
}
