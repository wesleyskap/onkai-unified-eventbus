using Microsoft.Extensions.DependencyInjection;
using Onkai.EventBus.Abstractions;
using Onkai.EventBus.Core.Serialization;
using Onkai.EventBus.Core.Subscription;

namespace Onkai.EventBus.Core.Extensions;

/// <summary>
/// Extension methods for setting up event bus services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core EventBus services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>An <see cref="EventBusBuilder"/> to chain configuration.</returns>
    public static EventBusBuilder AddEventBus(this IServiceCollection services)
    {
        services.AddSingleton<IEventSerializer, JsonEventSerializer>();
        services.AddSingleton<SubscriptionManager>();
        services.AddSingleton<IEventPublisher, EventPublisher>();

        return new EventBusBuilder(services);
    }

    /// <summary>
    /// Registers an event consumer.
    /// </summary>
    /// <typeparam name="TEvent">The event type to consume.</typeparam>
    /// <typeparam name="TConsumer">The consumer implementation.</typeparam>
    /// <param name="builder">The event bus builder.</param>
    /// <returns>The event bus builder.</returns>
    public static EventBusBuilder AddConsumer<TEvent, TConsumer>(this EventBusBuilder builder)
        where TEvent : IEvent
        where TConsumer : class, IEventConsumer<TEvent>
    {
        builder.Services.AddTransient<TConsumer>();
        builder.Services.AddSingleton(new SubscriptionInfo(typeof(TEvent), typeof(TConsumer)));

        return builder;
    }
}
