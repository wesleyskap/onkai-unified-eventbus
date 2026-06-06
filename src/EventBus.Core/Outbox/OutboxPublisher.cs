using Onkai.EventBus.Abstractions;
using Onkai.EventBus.Core.Serialization;

namespace Onkai.EventBus.Core.Outbox;

/// <summary>
/// An implementation of <see cref="IEventPublisher"/> that redirects publishing calls to the transactional outbox store.
/// 
/// Example:
/// <code>
/// IEventPublisher publisher = new OutboxPublisher(outboxStore, serializer);
/// await publisher.PublishAsync(new OrderCreatedEvent(...));
/// </code>
/// </summary>
public sealed class OutboxPublisher : IEventPublisher
{
    private readonly IOutboxStore _outboxStore;
    private readonly IEventSerializer _serializer;

    private static readonly System.Diagnostics.ActivitySource ActivitySource = new("Onkai.EventBus");

    /// <summary>
    /// Initializes a new instance of the OutboxPublisher class.
    /// </summary>
    public OutboxPublisher(IOutboxStore outboxStore, IEventSerializer serializer)
    {
        _outboxStore = outboxStore ?? throw new ArgumentNullException(nameof(outboxStore));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <inheritdoc />
    public async Task PublishAsync(
        IEvent @event,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (@event is null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        using var activity = ActivitySource.StartActivity($"Onkai.EventBus.OutboxPublish {@event.GetType().Name}", System.Diagnostics.ActivityKind.Producer);

        var message = CreateOutboxMessage(@event, options, activity);
        await _outboxStore.SaveAsync(message, cancellationToken);
    }

    private OutboxMessage CreateOutboxMessage(IEvent @event, PublishOptions? options, System.Diagnostics.Activity? activity)
    {
        var correlationId = options?.CorrelationId ?? activity?.TraceId.ToString() ?? Guid.NewGuid().ToString();
        var headers = options?.Headers != null ? new Dictionary<string, object>(options.Headers) : new Dictionary<string, object>();

        PopulateHeaders(headers, options, activity);

        var serializedHeaders = System.Text.Json.JsonSerializer.Serialize(headers);

        return new OutboxMessage
        {
            EventId = Guid.NewGuid().ToString(),
            EventName = @event.GetType().Name,
            CorrelationId = correlationId,
            Body = _serializer.Serialize(@event),
            SerializedHeaders = serializedHeaders
        };
    }

    private void PopulateHeaders(Dictionary<string, object> headers, PublishOptions? options, System.Diagnostics.Activity? activity)
    {
        if (activity?.Id != null)
        {
            headers["traceparent"] = activity.Id;
        }
        if (options?.RoutingKey != null)
        {
            headers["RoutingKeyOverride"] = options.RoutingKey;
        }
        if (options?.Delay != null)
        {
            headers["DelayMs"] = (long)options.Delay.Value.TotalMilliseconds;
        }
    }
}
