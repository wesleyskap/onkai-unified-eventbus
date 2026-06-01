namespace Onkai.EventBus.Abstractions;

/// <summary>
/// Marker interface for event consumers.
/// </summary>
public interface IEventConsumer
{
}

/// <summary>
/// Defines a consumer for a specific event type.
/// </summary>
/// <typeparam name="TEvent">The type of the event to consume.</typeparam>
public interface IEventConsumer<in TEvent> : IEventConsumer
    where TEvent : IEvent
{
    /// <summary>
    /// Consumes the event.
    /// </summary>
    /// <param name="event">The event payload.</param>
    /// <param name="context">The context of the consumption.</param>
    /// <param name="cancellationToken">A token to cancel the consume operation.</param>
    /// <returns>A task that represents the consumer execution.</returns>
    Task ConsumeAsync(TEvent @event, ConsumeContext context, CancellationToken cancellationToken);
}
