using GastroGestion.Domain.Common;

namespace GastroGestion.Application.Abstractions.Events;

/// <summary>
/// Port for dispatching domain events after a successful SaveChanges.
/// Implemented in Infrastructure; wired by DI.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct = default);
}
