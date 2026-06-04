using Onkai.EventBus.Abstractions;

namespace Onkai.EventBus.Core.Subscription;

public sealed class SubscriptionManager
{
    private readonly Dictionary<string, Type> _eventTypes = new();
    private readonly Dictionary<string, List<Type>> _handlers = new();

    /// <summary>
    /// Initializes a new instance of the SubscriptionManager class.
    /// </summary>
    /// <param name="subscriptions">Optional pre-registered subscriptions from DI.</param>
    public SubscriptionManager(IEnumerable<SubscriptionInfo>? subscriptions = null)
    {
        if (subscriptions == null)
        {
            return;
        }

        foreach (var sub in subscriptions)
        {
            AddSubscription(sub.EventType, sub.ConsumerType);
        }
    }

    /// <summary>
    /// Adds an event subscription registration.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <typeparam name="THandler">The consumer/handler type.</typeparam>
    public void AddSubscription<TEvent, THandler>()
        where TEvent : IEvent
        where THandler : IEventConsumer<TEvent>
    {
        AddSubscription(typeof(TEvent), typeof(THandler));
    }

    /// <summary>
    /// Adds an event subscription registration dynamically by type.
    /// </summary>
    /// <param name="eventType">The type of the event.</param>
    /// <param name="handlerType">The type of the consumer.</param>
    public void AddSubscription(Type eventType, Type handlerType)
    {
        var eventName = eventType.Name;
        EnsureEventNameRegistered(eventName, eventType);
        EnsureHandlerListCreated(eventName);
        EnsureHandlerNotDuplicate(eventName, handlerType);

        _handlers[eventName].Add(handlerType);
    }

    private void EnsureEventNameRegistered(string eventName, Type eventType)
    {
        if (!_eventTypes.ContainsKey(eventName))
        {
            _eventTypes.Add(eventName, eventType);
        }
    }

    private void EnsureHandlerListCreated(string eventName)
    {
        if (!_handlers.ContainsKey(eventName))
        {
            _handlers.Add(eventName, new List<Type>());
        }
    }

    private void EnsureHandlerNotDuplicate(string eventName, Type handlerType)
    {
        if (_handlers[eventName].Contains(handlerType))
        {
            throw new ArgumentException(
                $"Handler type {handlerType.Name} already registered for '{eventName}'",
                nameof(handlerType));
        }
    }

    /// <summary>
    /// Checks if there are active subscriptions for the given event name.
    /// </summary>
    public bool HasSubscriptionsForEvent(string eventName) => _handlers.ContainsKey(eventName);

    /// <summary>
    /// Gets all event names that have active subscriptions.
    /// </summary>
    public IEnumerable<string> GetRegisteredEventNames() => _eventTypes.Keys;

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
