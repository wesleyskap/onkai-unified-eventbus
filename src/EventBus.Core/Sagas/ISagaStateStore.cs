using System.Threading;
using System.Threading.Tasks;

namespace Onkai.EventBus.Core.Sagas;

/// <summary>
/// Defines the storage contract to persist and retrieve Saga execution state.
///
/// Example:
/// var state = await store.GetAsync("saga-123", cancellationToken);
/// </summary>
/// <typeparam name="TState">The type of the custom saga state data.</typeparam>
public interface ISagaStateStore<TState>
    where TState : class, new()
{
    /// <summary>
    /// Retrieves the Saga context by its unique identifier.
    /// </summary>
    /// <param name="sagaId">The Saga instance identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Saga context if found; otherwise, null.</returns>
    Task<SagaContext<TState>?> GetAsync(string sagaId, CancellationToken cancellationToken);

    /// <summary>
    /// Persists or updates the Saga context.
    /// </summary>
    /// <param name="context">The Saga context to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    Task SaveAsync(SagaContext<TState> context, CancellationToken cancellationToken);
}
