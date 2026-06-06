namespace Onkai.EventBus.Core.Inbox;

/// <summary>
/// Defines the storage contract to track processed message identifiers, ensuring consumer idempotency.
/// 
/// Example:
/// <code>
/// IInboxStore store = serviceProvider.GetRequiredService&lt;IInboxStore&gt;();
/// if (!await store.HasBeenProcessedAsync(messageId, cancellationToken))
/// {
///     await store.MarkAsProcessedAsync(messageId, cancellationToken);
/// }
/// </code>
/// </summary>
public interface IInboxStore
{
    /// <summary>
    /// Checks if a message with the specified identifier has already been processed successfully.
    /// </summary>
    /// <param name="messageId">The unique message identifier.</param>
    /// <param name="cancellationToken">Token to cancel execution.</param>
    /// <returns>True if the message has been processed; otherwise, false.</returns>
    Task<bool> HasBeenProcessedAsync(string messageId, CancellationToken cancellationToken);

    /// <summary>
    /// Marks a message identifier as processed.
    /// </summary>
    /// <param name="messageId">The unique message identifier.</param>
    /// <param name="cancellationToken">Token to cancel execution.</param>
    /// <returns>A task representing the database operation.</returns>
    Task MarkAsProcessedAsync(string messageId, CancellationToken cancellationToken);
}
