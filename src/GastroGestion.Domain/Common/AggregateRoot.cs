namespace GastroGestion.Domain.Common;

/// <summary>
/// Base class for aggregate roots. Extends <see cref="Entity"/> with a domain-event
/// buffer. Events are raised inside the aggregate and dispatched by the infrastructure
/// layer after persistence (post-SaveChanges). The aggregate never dispatches itself.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Domain events raised during this aggregate's lifetime, to be dispatched
    /// by the infrastructure layer after the unit of work is committed.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot(Guid id) : base(id) { }

    // Required for EF Core materialization.
#pragma warning disable CS8618
    protected AggregateRoot() { }
#pragma warning restore CS8618

    /// <summary>
    /// Buffers a domain event to be dispatched after persistence.
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    /// <summary>
    /// Clears the event buffer. Called by the infrastructure layer after
    /// successful dispatch.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
