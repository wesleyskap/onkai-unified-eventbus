using Onkai.EventBus.Abstractions;

namespace Onkai.EventBus.Core.Inbox;

/// <summary>
/// A base decorator class to enforce consumer idempotency via an <see cref="IInboxStore"/>.
/// 
/// Example:
/// <code>
/// public sealed class MyIdempotentConsumer : IdempotentConsumer&lt;MyEvent&gt;
/// {
///     public MyIdempotentConsumer(IInboxStore store) : base(store) { }
///     protected override Task ConsumeIdempotentAsync(MyEvent @event, ConsumeContext ctx, CancellationToken token) => Task.CompletedTask;
/// }
/// </code>
/// </summary>
/// <typeparam name="TEvent">The event type to consume.</typeparam>
public abstract class IdempotentConsumer<TEvent> : IEventConsumer<TEvent>
    where TEvent : IEvent
{
    private readonly IInboxStore _inboxStore;

    /// <summary>
    /// Initializes a new instance of the IdempotentConsumer class.
    /// </summary>
    protected IdempotentConsumer(IInboxStore inboxStore)
    {
        _inboxStore = inboxStore ?? throw new ArgumentNullException(nameof(inboxStore));
    }

    /// <inheritdoc />
    public async Task ConsumeAsync(TEvent @event, ConsumeContext context, CancellationToken cancellationToken)
    {
        var messageId = context.MessageId ?? context.CorrelationId ?? Guid.NewGuid().ToString();
        if (await _inboxStore.HasBeenProcessedAsync(messageId, cancellationToken))
        {
            return;
        }

        await ConsumeIdempotentAsync(@event, context, cancellationToken);
        await _inboxStore.MarkAsProcessedAsync(messageId, cancellationToken);
    }

    /// <summary>
    /// Executes the consumer logic once idempotency has been successfully verified.
    /// </summary>
    protected abstract Task ConsumeIdempotentAsync(TEvent @event, ConsumeContext context, CancellationToken cancellationToken);
}
