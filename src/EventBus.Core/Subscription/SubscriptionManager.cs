using Onkai.EventBus.Abstractions;

namespace Onkai.EventBus.Core.Subscription;

/// <summary>
/// Manages registrations of event consumers/handlers to facilitate dynamic message routing.
/// </summary>
public sealed class SubscriptionManager
{
    private readonly Dictionary<string, Type> _eventTypes = new();
    private readonly Dictionary<string, List<Type>> _handlers = new();

    /// <summary>
    /// Adds an event subscription registration.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <typeparam name="THandler">The consumer/handler type.</typeparam>
    public void AddSubscription<TEvent, THandler>()
        where TEvent : IEvent
        where THandler : IEventConsumer<TEvent>
    {
        var eventName = typeof(TEvent).Name;

        if (!_eventTypes.ContainsKey(eventName))
        {
            _eventTypes.Add(eventName, typeof(TEvent));
        }

        if (!_handlers.ContainsKey(eventName))
        {
            _handlers.Add(eventName, new List<Type>());
        }

        if (_handlers[eventName].Contains(typeof(THandler)))
        {
            throw new ArgumentException($"Handler type {typeof(THandler).Name} already registered for '{eventName}'");
        }

        _handlers[eventName].Add(typeof(THandler));
    }

    /// <summary>
    /// Checks if there are active subscriptions for the given event name.
    /// </summary>
    public bool HasSubscriptionsForEvent(string eventName) => _handlers.ContainsKey(eventName);

    /// <summary>
    /// Gets the event type matching the event name.
    /// </summary>
    public Type? GetEventTypeByName(string eventName)
    {
        return _eventTypes.TryGetValue(eventName, out var type) ? type : null;
    }

    /// <summary>
    /// Gets all registered handler types for the given event name.
    /// </summary>
    public IEnumerable<Type> GetHandlersForEvent(string eventName)
    {
        return _handlers.TryGetValue(eventName, out var handlers) ? handlers : Array.Empty<Type>();
    }
}
