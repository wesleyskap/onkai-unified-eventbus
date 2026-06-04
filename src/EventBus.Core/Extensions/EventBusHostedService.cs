using Microsoft.Extensions.Hosting;
using Onkai.EventBus.Core.Transport;

namespace Onkai.EventBus.Core.Extensions;

/// <summary>
/// A hosted service to start and stop message consumption within the application life cycle.
/// 
/// Example:
/// <code>
/// builder.Services.AddHostedService&lt;EventBusHostedService&gt;();
/// </code>
/// </summary>
public sealed class EventBusHostedService : IHostedService
{
    private readonly IEnumerable<IMessageConsumer> _consumers;

    /// <summary>
    /// Initializes a new instance of the EventBusHostedService class.
    /// </summary>
    /// <param name="consumers">The registered message consumers.</param>
    public EventBusHostedService(IEnumerable<IMessageConsumer> consumers)
    {
        _consumers = consumers ?? throw new ArgumentNullException(nameof(consumers));
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in _consumers)
        {
            await consumer.StartConsumingAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in _consumers)
        {
            await consumer.StopConsumingAsync(cancellationToken);
        }
    }
}
