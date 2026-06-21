using GastroGestion.Domain.Common;

namespace GastroGestion.Application.Abstractions.Events;

/// <summary>
/// Handles a single domain-event type after the unit of work has committed.
/// Resolved by the post-commit dispatcher; multiple handlers per event are allowed.
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken ct = default);
}
