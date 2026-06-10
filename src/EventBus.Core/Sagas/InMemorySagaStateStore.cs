using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Onkai.EventBus.Core.Sagas;

/// <summary>
/// An in-memory implementation of <see cref="ISagaStateStore{TState}"/> for testing and mock environments.
///
/// Example:
/// var store = new InMemorySagaStateStore&lt;OrderState&gt;();
/// </summary>
/// <typeparam name="TState">The type of the custom saga state data.</typeparam>
public sealed class InMemorySagaStateStore<TState> : ISagaStateStore<TState>
    where TState : class, new()
{
    private readonly ConcurrentDictionary<string, SagaContext<TState>> _states = new();

    /// <inheritdoc />
    public Task<SagaContext<TState>?> GetAsync(string sagaId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(sagaId))
        {
            throw new ArgumentException("Saga ID cannot be null or empty.", nameof(sagaId));
        }

        _states.TryGetValue(sagaId, out var context);
        return Task.FromResult(context);
    }

    /// <inheritdoc />
    public Task SaveAsync(SagaContext<TState> context, CancellationToken cancellationToken)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrEmpty(context.SagaId))
        {
            throw new ArgumentException("Saga ID cannot be null or empty.", nameof(context));
        }

        _states[context.SagaId] = context;
        return Task.CompletedTask;
    }
}
