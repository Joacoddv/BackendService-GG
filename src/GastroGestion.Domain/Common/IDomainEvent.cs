namespace GastroGestion.Domain.Common;

/// <summary>
/// Marker interface for all domain events. Events are raised inside aggregates
/// and dispatched by the infrastructure layer after persistence (post-SaveChanges).
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
