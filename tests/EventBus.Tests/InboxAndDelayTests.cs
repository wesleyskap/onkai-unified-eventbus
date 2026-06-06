using Onkai.EventBus.Abstractions;
using Onkai.EventBus.Core;
using Onkai.EventBus.Core.Inbox;
using Onkai.EventBus.Core.Serialization;

namespace Onkai.EventBus.Tests;

/// <summary>
/// A named fake inbox store for unit testing message idempotency.
/// </summary>
public sealed class FakeInboxStore : IInboxStore
{
    private readonly HashSet<string> _processedIds = new();

    /// <inheritdoc />
    public Task<bool> HasBeenProcessedAsync(string messageId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_processedIds.Contains(messageId));
    }

    /// <inheritdoc />
    public Task MarkAsProcessedAsync(string messageId, CancellationToken cancellationToken)
    {
        _processedIds.Add(messageId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// A test idempotent consumer for unit testing.
/// </summary>
public sealed class TestIdempotentConsumer : IdempotentConsumer<TestEvent>
{
    /// <summary>
    /// Gets the number of times the consumer executed successfully.
    /// </summary>
    public int CallCount { get; private set; }

    /// <summary>
    /// Initializes a new instance of the TestIdempotentConsumer class.
    /// </summary>
    public TestIdempotentConsumer(IInboxStore store) : base(store) { }

    /// <inheritdoc />
    protected override Task ConsumeIdempotentAsync(TestEvent @event, ConsumeContext context, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Unit tests for Scheduled Messages and Inbox Pattern.
/// </summary>
public class InboxAndDelayTests
{
    [Fact]
    public async Task IdempotentConsumer_ShouldOnlyExecuteOnceForDuplicateMessageIds()
    {
        var store = new FakeInboxStore();
        var consumer = new TestIdempotentConsumer(store);
        var @event = new TestEvent("Idempotent check");
        var context = new ConsumeContext { MessageId = "msg-unique-123" };

        await consumer.ConsumeAsync(@event, context, CancellationToken.None);
        await consumer.ConsumeAsync(@event, context, CancellationToken.None);

        Assert.Equal(1, consumer.CallCount);
    }

    [Fact]
    public async Task PublishAsync_WithDelay_ShouldIncludeDelayMsInEnvelopeHeaders()
    {
        var transport = new FakeTransport();
        var serializer = new JsonEventSerializer();
        var publisher = new EventPublisher(transport, serializer);
        var @event = new TestEvent("Delayed message");
        var options = new PublishOptions { Delay = TimeSpan.FromSeconds(5) };

        await publisher.PublishAsync(@event, options);

        Assert.Single(transport.SentEnvelopes);
        var envelope = transport.SentEnvelopes[0];
        Assert.True(envelope.Headers.TryGetValue("DelayMs", out var val));
        Assert.Equal(5000L, val);
    }
}
