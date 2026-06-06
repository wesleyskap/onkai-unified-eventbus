namespace Onkai.EventBus.Core.Outbox;

/// <summary>
/// Represents a message stored in the transactional outbox to be dispatched later.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>
    /// Gets or sets the unique identifier of the outbox database record.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the unique event identifier.
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name/type of the event.
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the correlation identifier for tracing.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized binary payload of the event.
    /// </summary>
    public byte[] Body { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the serialized headers dictionary.
    /// </summary>
    public string SerializedHeaders { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the timestamp when the message was recorded.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when the message was successfully published.
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; set; }
}
