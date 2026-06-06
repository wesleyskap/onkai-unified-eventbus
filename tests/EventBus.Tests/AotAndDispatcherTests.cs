using System.Text.Json;
using Onkai.EventBus.Abstractions;
using Onkai.EventBus.Core.Serialization;
using Onkai.EventBus.Core.Subscription;

namespace Onkai.EventBus.Tests;

/// <summary>
/// A test consumer implementing IEventConsumer&lt;TestEvent&gt; to verify executor dispatching.
/// </summary>
public sealed class TestExecutorConsumer : IEventConsumer<TestEvent>
{
    /// <summary>
    /// Gets the received event payload.
    /// </summary>
    public TestEvent? ReceivedEvent { get; private set; }

    /// <inheritdoc />
    public Task ConsumeAsync(TestEvent @event, ConsumeContext context, CancellationToken cancellationToken)
    {
        ReceivedEvent = @event;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Unit tests for Native AOT and Dispatcher optimizations.
/// </summary>
public class AotAndDispatcherTests
{
    [Fact]
    public void JsonEventSerializer_ShouldRespectCustomOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower
        };
        var serializer = new JsonEventSerializer(options);
        var @event = new TestEvent("AOT options");

        var bytes = serializer.Serialize(@event);
        var jsonString = System.Text.Encoding.UTF8.GetString(bytes);

        Assert.Contains("message", jsonString); // Kebab case lower matches kebab case for single word
    }

    [Fact]
    public async Task EventConsumerExecutor_ShouldDispatchDirectlyToConsumer()
    {
        IEventConsumerExecutor executor = new EventConsumerExecutor<TestEvent>();
        var consumer = new TestExecutorConsumer();
        var @event = new TestEvent("Direct Dispatch");
        var context = new ConsumeContext { MessageId = "msg-direct-1" };

        await executor.ExecuteAsync(consumer, @event, context, CancellationToken.None);

        Assert.NotNull(consumer.ReceivedEvent);
        Assert.Equal("Direct Dispatch", consumer.ReceivedEvent.Message);
    }
}
