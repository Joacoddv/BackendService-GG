using GastroGestion.Application.Abstractions.Events;
using GastroGestion.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace GastroGestion.Infrastructure.Events;

/// <summary>
/// In-process post-commit dispatcher. For each raised event it resolves every registered
/// <see cref="IDomainEventHandler{TEvent}"/> for the event's runtime type and invokes them in
/// sequence. Handlers run after the originating unit of work has committed (eventual consistency);
/// a handler failure does not roll back the already-committed change.
/// </summary>
public sealed class InProcessDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public InProcessDomainEventDispatcher(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    public async Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var domainEvent in events)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;

            foreach (var handler in _serviceProvider.GetServices(handlerType))
            {
                if (handler is null)
                    continue;
                await (Task)method.Invoke(handler, new object[] { domainEvent, ct })!;
            }
        }
    }
}
