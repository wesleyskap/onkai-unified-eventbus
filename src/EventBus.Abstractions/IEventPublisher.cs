namespace Onkai.EventBus.Abstractions;

/// <summary>
/// Defines the contract for publishing events.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes an event to the configured transport.
    /// </summary>
    /// <param name="event">The event payload to publish.</param>
    /// <param name="options">Optional metadata and publishing configurations.</param>
    /// <param name="cancellationToken">Token to cancel the publish operation.</param>
    /// <returns>A task that completes when the event is published.</returns>
    Task PublishAsync(
        IEvent @event,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default);
}
