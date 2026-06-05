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

    private static readonly System.Diagnostics.ActivitySource ActivitySource = new("Onkai.EventBus");

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

        using var activity = ActivitySource.StartActivity($"Onkai.EventBus.Publish {@event.GetType().Name}", System.Diagnostics.ActivityKind.Producer);
        var envelope = CreateEnvelope(@event, options, activity);

        return _transport.SendAsync(envelope, cancellationToken);
    }

    private TransportEnvelope CreateEnvelope(IEvent @event, PublishOptions? options, System.Diagnostics.Activity? activity)
    {
        var correlationId = options?.CorrelationId ?? activity?.TraceId.ToString() ?? Guid.NewGuid().ToString();
        var headers = options?.Headers != null ? new Dictionary<string, object>(options.Headers) : new Dictionary<string, object>();

        if (activity?.Id != null)
        {
            headers["traceparent"] = activity.Id;
        }

        if (options?.RoutingKey != null)
        {
            headers["RoutingKeyOverride"] = options.RoutingKey;
        }

        return new TransportEnvelope
        {
            EventId = Guid.NewGuid().ToString(),
            EventName = @event.GetType().Name,
            CorrelationId = correlationId,
            Body = _serializer.Serialize(@event),
            Headers = headers
        };
    }
}
