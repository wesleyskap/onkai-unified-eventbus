using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Onkai.EventBus.Abstractions;
using Onkai.EventBus.Core;
using Onkai.EventBus.Core.Outbox;
using Onkai.EventBus.Core.Serialization;

namespace Onkai.EventBus.Tests;

/// <summary>
/// A named fake outbox store to record outbox operations in memory.
/// </summary>
public sealed class FakeOutboxStore : IOutboxStore
{
    /// <summary>
    /// Gets the list of messages in memory.
    /// </summary>
    public List<OutboxMessage> Messages { get; } = new();

    /// <inheritdoc />
    public Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<OutboxMessage>> GetUnpublishedMessagesAsync(CancellationToken cancellationToken)
    {
        var pending = Messages.Where(m => m.ProcessedAt == null).ToList();
        return Task.FromResult<IEnumerable<OutboxMessage>>(pending);
    }

    /// <inheritdoc />
    public Task MarkAsPublishedAsync(Guid id, CancellationToken cancellationToken)
    {
        var message = Messages.FirstOrDefault(m => m.Id == id);
        if (message != null)
        {
            message.ProcessedAt = DateTimeOffset.UtcNow;
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// Unit tests for Tracing and Outbox features.
/// </summary>
public class OutboxAndTracingTests
{
    [Fact]
    public async Task OutboxPublisher_ShouldSaveToStoreWithTracingHeaders()
    {
        var store = new FakeOutboxStore();
        var serializer = new JsonEventSerializer();
        var publisher = new OutboxPublisher(store, serializer);
        var @event = new TestEvent("Hello Outbox!");

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);
        var activitySource = new ActivitySource("Onkai.EventBus");
        using var act = activitySource.StartActivity("RootTrace");

        await publisher.PublishAsync(@event);

        Assert.Single(store.Messages);
        var msg = store.Messages[0];
        Assert.Equal(nameof(TestEvent), msg.EventName);
        Assert.Contains("traceparent", msg.SerializedHeaders);
    }

    [Fact]
    public async Task OutboxProcessor_ShouldPublishPendingMessagesAndMarkProcessed()
    {
        var services = new ServiceCollection();
        var store = new FakeOutboxStore();
        var transport = new FakeTransport();
        
        await store.SaveAsync(new OutboxMessage
        {
            EventId = Guid.NewGuid().ToString(),
            EventName = "TestEvent",
            CorrelationId = "correlation-123",
            Body = new JsonEventSerializer().Serialize(new TestEvent("Outbox message")),
            SerializedHeaders = "{\"traceparent\":\"00-trace-span-01\"}"
        }, CancellationToken.None);

        services.AddSingleton<IOutboxStore>(store);
        services.AddSingleton<Onkai.EventBus.Core.Transport.IMessageTransport>(transport);
        var provider = services.BuildServiceProvider();

        var processor = new OutboxProcessor(provider, NullLogger<OutboxProcessor>.Instance);
        await processor.StartAsync(CancellationToken.None);
        await Task.Delay(100); // Allow background loop to process
        await processor.StopAsync(CancellationToken.None);

        Assert.Single(transport.SentEnvelopes);
        Assert.NotNull(store.Messages[0].ProcessedAt);
        Assert.Equal("correlation-123", transport.SentEnvelopes[0].CorrelationId);
    }
}
