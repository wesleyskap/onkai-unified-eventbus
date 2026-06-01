using Onkai.EventBus.Abstractions;
using Onkai.EventBus.Core;
using Onkai.EventBus.Core.Serialization;
using Onkai.EventBus.Core.Subscription;

namespace Onkai.EventBus.Tests;

/// <summary>
/// Immutable record representing a test event.
/// </summary>
public sealed record TestEvent(string Message) : IEvent;

/// <summary>
/// A consumer for <see cref="TestEvent"/> used during subscription unit testing.
/// </summary>
public sealed class TestEventConsumer : IEventConsumer<TestEvent>
{
    /// <inheritdoc />
    public Task ConsumeAsync(
        TestEvent @event,
        ConsumeContext context,
        System.Threading.CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Unit tests for EventPublisher and subscription management features.
/// </summary>
public class PublisherTests
{
    [Fact]
    public async Task PublishAsync_ShouldSerializeAndSendEnvelope()
    {
        var transport = new FakeTransport();
        var serializer = new JsonEventSerializer();
        var publisher = new EventPublisher(transport, serializer);
        var @event = new TestEvent("Publish, async!");

        await publisher.PublishAsync(@event);

        Assert.Single(transport.SentEnvelopes);
        var envelope = transport.SentEnvelopes[0];
        Assert.Equal(nameof(TestEvent), envelope.EventName);
        Assert.NotNull(envelope.EventId);
        Assert.NotNull(envelope.CorrelationId);

        var deserialized = serializer.Deserialize<TestEvent>(envelope.Body);
        Assert.Equal("Publish, async!", deserialized.Message);
    }

    [Fact]
    public async Task PublishAsync_WithCorrelationId_ShouldPreserveCorrelationId()
    {
        var transport = new FakeTransport();
        var serializer = new JsonEventSerializer();
        var publisher = new EventPublisher(transport, serializer);
        var @event = new TestEvent("Traceable event");
        var correlationId = Guid.NewGuid().ToString();
        var options = new PublishOptions { CorrelationId = correlationId };

        await publisher.PublishAsync(@event, options);

        Assert.Single(transport.SentEnvelopes);
        var envelope = transport.SentEnvelopes[0];
        Assert.Equal(correlationId, envelope.CorrelationId);
    }

    [Fact]
    public async Task PublishAsync_WithRoutingKeyOverride_ShouldIncludeItInHeaders()
    {
        var transport = new FakeTransport();
        var serializer = new JsonEventSerializer();
        var publisher = new EventPublisher(transport, serializer);
        var @event = new TestEvent("Custom route");
        var options = new PublishOptions { RoutingKey = "custom-routing-key" };

        await publisher.PublishAsync(@event, options);

        Assert.Single(transport.SentEnvelopes);
        var envelope = transport.SentEnvelopes[0];
        Assert.True(envelope.Headers.TryGetValue("RoutingKeyOverride", out var key));
        Assert.Equal("custom-routing-key", key);
    }

    [Fact]
    public void SubscriptionManager_ShouldRegisterAndRetrieveSubscriptions()
    {
        var manager = new SubscriptionManager();

        manager.AddSubscription<TestEvent, TestEventConsumer>();

        Assert.True(manager.HasSubscriptionsForEvent(nameof(TestEvent)));
        var eventType = manager.GetEventTypeByName(nameof(TestEvent));
        Assert.Equal(typeof(TestEvent), eventType);

        var handlers = manager.GetHandlersForEvent(nameof(TestEvent));
        var handlerType = Assert.Single(handlers);
        Assert.Equal(typeof(TestEventConsumer), handlerType);
    }
}
