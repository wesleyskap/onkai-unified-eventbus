namespace Onkai.EventBus.Core.Transport;

/// <summary>
/// Defines the contract for background message listening/consuming.
/// 
/// Example:
/// <code>
/// IMessageConsumer consumer = serviceProvider.GetRequiredService&lt;IMessageConsumer&gt;();
/// await consumer.StartConsumingAsync(cancellationToken);
/// </code>
/// </summary>
public interface IMessageConsumer
{
    /// <summary>
    /// Starts the background listening loop.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel starting.</param>
    /// <returns>A task representing the startup operations.</returns>
    Task StartConsumingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gracefully stops the listening loop and releases connections.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel stopping.</param>
    /// <returns>A task representing the shutdown operations.</returns>
    Task StopConsumingAsync(CancellationToken cancellationToken);
}
