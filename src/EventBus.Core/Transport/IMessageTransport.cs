namespace Onkai.EventBus.Core.Transport;

/// <summary>
/// Defines the broker-agnostic contract that providers must implement to transport envelopes.
/// </summary>
public interface IMessageTransport
{
    /// <summary>
    /// Transports the envelope to the broker.
    /// </summary>
    /// <param name="envelope">The message container.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the delivery succeeds.</returns>
    Task SendAsync(
        TransportEnvelope envelope,
        CancellationToken cancellationToken);
}
