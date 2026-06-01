namespace Onkai.EventBus.Core.Serialization;

/// <summary>
/// Defines the contract for serializing and deserializing events.
/// </summary>
public interface IEventSerializer
{
    /// <summary>
    /// Serializes an event to a byte array.
    /// </summary>
    /// <typeparam name="T">The type of the event.</typeparam>
    /// <param name="event">The event instance.</param>
    /// <returns>A serialized byte array.</returns>
    byte[] Serialize<T>(T @event);

    /// <summary>
    /// Deserializes a byte array to the specified event type.
    /// </summary>
    /// <param name="data">The raw bytes.</param>
    /// <param name="type">The type of the event.</param>
    /// <returns>The deserialized event object.</returns>
    object Deserialize(byte[] data, Type type);

    /// <summary>
    /// Deserializes a byte array to the specified generic event type.
    /// </summary>
    /// <typeparam name="T">The type of the event.</typeparam>
    /// <param name="data">The raw bytes.</param>
    /// <returns>The deserialized event object.</returns>
    T Deserialize<T>(byte[] data);
}
