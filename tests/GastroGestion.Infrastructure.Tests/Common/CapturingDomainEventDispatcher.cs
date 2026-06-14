using GastroGestion.Application.Abstractions.Events;
using GastroGestion.Domain.Common;

namespace GastroGestion.Infrastructure.Tests.Common;

/// <summary>
/// Test double that records all dispatched domain events.
/// Used in DomainEventDispatchTests to assert post-commit firing.
/// </summary>
public sealed class CapturingDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly List<IDomainEvent> _captured = [];

    public IReadOnlyList<IDomainEvent> CapturedEvents => _captured.AsReadOnly();

    public Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct = default)
    {
        _captured.AddRange(events);
        return Task.CompletedTask;
    }
}
