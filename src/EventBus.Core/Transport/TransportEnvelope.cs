namespace Onkai.EventBus.Core.Transport;

/// <summary>
/// A provider-agnostic container representing a message payload and its headers/metadata.
/// </summary>
public sealed class TransportEnvelope
{
    /// <summary>
    /// Gets the unique identifier for the event.
    /// </summary>
    public required string EventId { get; init; }

    /// <summary>
    /// Gets the name of the event type.
    /// </summary>
    public required string EventName { get; init; }

    /// <summary>
    /// Gets the correlation identifier for tracking message flows across boundaries.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the raw serialized event body.
    /// </summary>
    public required byte[] Body { get; init; }

    /// <summary>
    /// Gets the metadata headers associated with this envelope.
    /// </summary>
    public Dictionary<string, object> Headers { get; init; } = new();
}
