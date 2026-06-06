using System.Runtime.CompilerServices;
using Onkai.EventBus.Abstractions;

[assembly: InternalsVisibleTo("EventBus.RabbitMQ")]
[assembly: InternalsVisibleTo("EventBus.Tests")]

namespace Onkai.EventBus.Core.Subscription;

/// <summary>
/// Defines a contract to dispatch events to untyped consumer instances without using reflection invocation.
/// 
/// Example:
/// <code>
/// IEventConsumerExecutor executor = new EventConsumerExecutor&lt;MyEvent&gt;();
/// await executor.ExecuteAsync(consumerInstance, eventInstance, context, cancellationToken);
/// </code>
/// </summary>
internal interface IEventConsumerExecutor
{
    /// <summary>
    /// Executes the typed ConsumeAsync method on the consumer instance.
    /// </summary>
    /// <param name="consumer">The untyped consumer instance.</param>
    /// <param name="event">The untyped event payload.</param>
    /// <param name="context">The consume context.</param>
    /// <param name="cancellationToken">A token to cancel execution.</param>
    /// <returns>A task representing execution.</returns>
    Task ExecuteAsync(object consumer, IEvent @event, ConsumeContext context, CancellationToken cancellationToken);
}
