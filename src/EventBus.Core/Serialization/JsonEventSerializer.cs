using System.Text.Json;

namespace Onkai.EventBus.Core.Serialization;

/// <summary>
/// A JSON-based implementation of the event serializer.
/// </summary>
public sealed class JsonEventSerializer : IEventSerializer
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the JsonEventSerializer class.
    /// </summary>
    /// <param name="options">Optional JSON serializer options.</param>
    public JsonEventSerializer(JsonSerializerOptions? options = null)
    {
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public byte[] Serialize<T>(T @event)
    {
        if (@event is null)
        {
            throw new ArgumentNullException(nameof(@event));
        }
        return JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), _options);
    }

    /// <inheritdoc />
    public object Deserialize(byte[] data, Type type)
    {
        var result = JsonSerializer.Deserialize(data, type, _options);
        return result ?? throw new InvalidOperationException("Deserialization returned null.");
    }

    /// <inheritdoc />
    public T Deserialize<T>(byte[] data)
    {
        var result = JsonSerializer.Deserialize<T>(data, _options);
        return result ?? throw new InvalidOperationException("Deserialization returned null.");
    }
}
