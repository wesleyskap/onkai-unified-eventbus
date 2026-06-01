using Microsoft.Extensions.DependencyInjection;
using Onkai.EventBus.Core.Extensions;
using Onkai.EventBus.Core.Transport;
using RabbitMQ.Client;

namespace Onkai.EventBus.RabbitMQ.Extensions;

/// <summary>
/// Extension methods to configure the RabbitMQ provider for the EventBus.
/// </summary>
public static class RabbitMqExtensions
{
    /// <summary>
    /// Configures the EventBus to use RabbitMQ as the message transport.
    /// </summary>
    /// <param name="builder">The event bus builder.</param>
    /// <param name="configure">An optional action to configure the RabbitMQ connection factory.</param>
    /// <returns>The event bus builder.</returns>
    public static EventBusBuilder UseRabbitMq(
        this EventBusBuilder builder,
        Action<ConnectionFactory>? configure = null)
    {
        var factory = new ConnectionFactory();
        configure?.Invoke(factory);

        builder.Services.AddSingleton(factory);
        builder.Services.AddSingleton<IMessageTransport, RabbitMqTransport>();

        return builder;
    }
}
