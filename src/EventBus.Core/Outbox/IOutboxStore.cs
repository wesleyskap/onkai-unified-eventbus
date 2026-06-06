namespace Onkai.EventBus.Core.Outbox;

/// <summary>
/// Defines the storage contract for persisting and retrieving transactional outbox messages.
/// 
/// Example:
/// <code>
/// IOutboxStore store = serviceProvider.GetRequiredService&lt;IOutboxStore&gt;();
/// await store.SaveAsync(outboxMessage, cancellationToken);
/// </code>
/// </summary>
public interface IOutboxStore
{
    /// <summary>
    /// Persists a new outbox message.
    /// </summary>
    /// <param name="message">The message to persist.</param>
    /// <param name="cancellationToken">Token to cancel execution.</param>
    /// <returns>A task representing the database operation.</returns>
    Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a collection of unpublished outbox messages, ordered chronologically.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel execution.</param>
    /// <returns>An enumerable of pending outbox messages.</returns>
    Task<IEnumerable<OutboxMessage>> GetUnpublishedMessagesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Marks a specific message as published successfully.
    /// </summary>
    /// <param name="id">The unique identifier of the outbox record.</param>
    /// <param name="cancellationToken">Token to cancel execution.</param>
    /// <returns>A task representing the database operation.</returns>
    Task MarkAsPublishedAsync(Guid id, CancellationToken cancellationToken);
}
