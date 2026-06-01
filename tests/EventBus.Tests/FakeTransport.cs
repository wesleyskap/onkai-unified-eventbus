using Onkai.EventBus.Core.Transport;

namespace Onkai.EventBus.Tests;

/// <summary>
/// A fake message transport implementation to collect envelopes in memory for unit testing.
/// </summary>
public sealed class FakeTransport : IMessageTransport
{
    /// <summary>
    /// Gets the list of envelopes that were sent through this transport.
    /// </summary>
    public List<TransportEnvelope> SentEnvelopes { get; } = new();

    /// <inheritdoc />
    public Task SendAsync(
        TransportEnvelope envelope,
        CancellationToken cancellationToken)
    {
        SentEnvelopes.Add(envelope);
        return Task.CompletedTask;
    }
}
