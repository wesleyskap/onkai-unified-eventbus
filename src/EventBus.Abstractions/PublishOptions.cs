namespace Onkai.EventBus.Abstractions;

/// <summary>
/// Options and metadata configuration used when publishing an event.
/// </summary>
public sealed class PublishOptions
{
    /// <summary>
    /// Gets or sets the Correlation ID for tracing across services.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets the dictionary of custom metadata headers to be sent with the event.
    /// </summary>
    public Dictionary<string, object> Headers { get; } = new();

    /// <summary>
    /// Gets or sets an optional routing key override.
    /// </summary>
    public string? RoutingKey { get; set; }

    /// <summary>
    /// Gets or sets an optional delay duration before the event is delivered.
    /// </summary>
    public TimeSpan? Delay { get; set; }
}
