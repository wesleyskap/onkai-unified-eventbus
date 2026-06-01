namespace Onkai.EventBus.Abstractions;

/// <summary>
/// Provides context information for a consumed message.
/// </summary>
public sealed class ConsumeContext
{
    /// <summary>
    /// Gets the Correlation ID associated with this message execution.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the headers associated with this message.
    /// </summary>
    public IReadOnlyDictionary<string, object> Headers { get; init; } = new Dictionary<string, object>();
}
