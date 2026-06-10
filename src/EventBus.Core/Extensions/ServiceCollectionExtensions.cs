using Microsoft.Extensions.DependencyInjection;
using Onkai.EventBus.Abstractions;
using Onkai.EventBus.Core.Serialization;
using Onkai.EventBus.Core.Subscription;
using Onkai.EventBus.Core.Outbox;
using Onkai.EventBus.Core.Sagas;
using System.Text.Json;

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
        services.AddSingleton<IEventSerializer>(sp =>
            new JsonEventSerializer(sp.GetService<JsonSerializerOptions>()));
        services.AddSingleton<SubscriptionManager>();
        services.AddSingleton<IEventPublisher, EventPublisher>();
        services.AddHostedService<EventBusHostedService>();

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

    /// <summary>
    /// Configures the EventBus to use the Transactional Outbox pattern.
    /// </summary>
    /// <typeparam name="TStore">The storage implementation for the outbox.</typeparam>
    /// <param name="builder">The event bus builder.</param>
    /// <returns>The event bus builder.</returns>
    public static EventBusBuilder UseOutbox<TStore>(this EventBusBuilder builder)
        where TStore : class, IOutboxStore
    {
        builder.Services.AddScoped<IOutboxStore, TStore>();
        builder.Services.AddHostedService<OutboxProcessor>();
        builder.Services.AddSingleton<IEventPublisher, OutboxPublisher>();

        return builder;
    }

    /// <summary>
    /// Configures the EventBus to use a Saga Orchestrator with the specified state and storage.
    /// </summary>
    public static EventBusBuilder AddSaga<TState, TStore>(this EventBusBuilder builder)
        where TState : class, new()
        where TStore : class, ISagaStateStore<TState>
    {
        builder.Services.AddSingleton<ISagaStateStore<TState>, TStore>();
        builder.Services.AddSingleton<Sagas.SagaOrchestrator<TState>>();
        return builder;
    }
}
