using Onkai.EventBus.Abstractions;

namespace Onkai.EventBus.Core.Subscription;

/// <summary>
/// A generic helper that casts untyped inputs and executes the strongly-typed ConsumeAsync method directly.
/// </summary>
/// <typeparam name="TEvent">The event type.</typeparam>
internal sealed class EventConsumerExecutor<TEvent> : IEventConsumerExecutor
    where TEvent : IEvent
{
    /// <inheritdoc />
    public Task ExecuteAsync(object consumer, IEvent @event, ConsumeContext context, CancellationToken cancellationToken)
    {
        if (consumer == null)
        {
            throw new ArgumentNullException(nameof(consumer));
        }
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        var typedConsumer = (IEventConsumer<TEvent>)consumer;
        var typedEvent = (TEvent)@event;

        return typedConsumer.ConsumeAsync(typedEvent, context, cancellationToken);
    }
}
