using Onkai.EventBus.Abstractions;
using Onkai.EventBus.Core.Serialization;
using Onkai.EventBus.Core.Transport;

namespace Onkai.EventBus.Core;

/// <summary>
/// Default implementation of the event publisher that serializes events and publishes them via the registered transport.
/// </summary>
public sealed class EventPublisher : IEventPublisher
{
    private readonly IMessageTransport _transport;
    private readonly IEventSerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the EventPublisher class.
    /// </summary>
    /// <param name="transport">The transport mechanism to use.</param>
    /// <param name="serializer">The serializer to use.</param>
    public EventPublisher(IMessageTransport transport, IEventSerializer serializer)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <inheritdoc />
    public Task PublishAsync(
        IEvent @event,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (@event is null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        var eventId = Guid.NewGuid().ToString();
        var eventName = @event.GetType().Name;
        var correlationId = options?.CorrelationId ?? Guid.NewGuid().ToString();
        var body = _serializer.Serialize(@event);

        var envelope = new TransportEnvelope
        {
            EventId = eventId,
            EventName = eventName,
            CorrelationId = correlationId,
            Body = body,
            Headers = options?.Headers != null ? new(options.Headers) : new()
        };

        if (options?.RoutingKey != null)
        {
            envelope.Headers["RoutingKeyOverride"] = options.RoutingKey;
        }

        return _transport.SendAsync(envelope, cancellationToken);
    }
}
