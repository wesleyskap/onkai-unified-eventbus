using System.Text.Json;

namespace Onkai.EventBus.Core.Serialization;

/// <summary>
/// A JSON-based implementation of the event serializer.
/// </summary>
public sealed class JsonEventSerializer : IEventSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public byte[] Serialize<T>(T @event)
    {
        if (@event is null)
        {
            throw new ArgumentNullException(nameof(@event));
        }
        return JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), Options);
    }

    /// <inheritdoc />
    public object Deserialize(byte[] data, Type type)
    {
        var result = JsonSerializer.Deserialize(data, type, Options);
        return result ?? throw new InvalidOperationException("Deserialization returned null.");
    }

    /// <inheritdoc />
    public T Deserialize<T>(byte[] data)
    {
        var result = JsonSerializer.Deserialize<T>(data, Options);
        return result ?? throw new InvalidOperationException("Deserialization returned null.");
    }
}
