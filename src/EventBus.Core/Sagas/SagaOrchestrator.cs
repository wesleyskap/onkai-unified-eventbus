using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Onkai.EventBus.Abstractions;

namespace Onkai.EventBus.Core.Sagas;

/// <summary>
/// Orchestrates Saga execution steps and manages rollback compensations.
///
/// Example:
/// var orchestrator = new SagaOrchestrator&lt;OrderState&gt;(store, logger);
/// orchestrator.RegisterCompensation("PaymentStep", async (ctx, token) => { ... });
/// </summary>
/// <typeparam name="TState">The custom saga state type.</typeparam>
public sealed class SagaOrchestrator<TState>
    where TState : class, new()
{
    private readonly ISagaStateStore<TState> _store;
    private readonly ILogger<SagaOrchestrator<TState>> _logger;
    private readonly ConcurrentDictionary<string, Func<SagaContext<TState>, CancellationToken, Task>> _compensations = new();

    /// <summary>
    /// Initializes a new instance of the SagaOrchestrator class.
    /// </summary>
    public SagaOrchestrator(ISagaStateStore<TState> store, ILogger<SagaOrchestrator<TState>> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a compensation callback for a given step name.
    /// </summary>
    public void RegisterCompensation(string stepName, Func<SagaContext<TState>, CancellationToken, Task> compensation)
    {
        if (string.IsNullOrEmpty(stepName))
        {
            throw new ArgumentException("Step name cannot be null or empty.", nameof(stepName));
        }
        _compensations[stepName] = compensation ?? throw new ArgumentNullException(nameof(compensation));
    }

    /// <summary>
    /// Executes a Saga step reactively when an event is received.
    /// </summary>
    public async Task ExecuteStepAsync<TEvent>(
        string sagaId,
        TEvent @event,
        Func<SagaContext<TState>, TEvent, CancellationToken, Task> stepAction,
        CancellationToken cancellationToken)
        where TEvent : IEvent
    {
        var context = await _store.GetAsync(sagaId, cancellationToken) ?? new SagaContext<TState> { SagaId = sagaId };

        if (context.Status is "Failed" or "Compensated")
        {
            _logger.LogWarning("Saga {SagaId} is in terminal status {Status}. Skipping step {StepName}.", sagaId, context.Status, typeof(TEvent).Name);
            return;
        }

        await RunStepInternalAsync(context, @event, stepAction, cancellationToken);
    }

    private async Task RunStepInternalAsync<TEvent>(
        SagaContext<TState> context,
        TEvent @event,
        Func<SagaContext<TState>, TEvent, CancellationToken, Task> stepAction,
        CancellationToken cancellationToken)
        where TEvent : IEvent
    {
        try
        {
            await stepAction(context, @event, cancellationToken);
            context.CompletedSteps.Add(typeof(TEvent).Name);
            await _store.SaveAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Step {StepName} failed for Saga {SagaId}. Triggering rollback.", typeof(TEvent).Name, context.SagaId);
            await RollbackSagaAsync(context, typeof(TEvent).Name, cancellationToken);
        }
    }

    private async Task RollbackSagaAsync(SagaContext<TState> context, string failingStep, CancellationToken cancellationToken)
    {
        context.Status = "Failed";
        await _store.SaveAsync(context, cancellationToken);

        await CompensateStepAsync(context, failingStep, cancellationToken);

        for (var i = context.CompletedSteps.Count - 1; i >= 0; i--)
        {
            var completedStep = context.CompletedSteps[i];
            await CompensateStepAsync(context, completedStep, cancellationToken);
        }

        context.Status = "Compensated";
        await _store.SaveAsync(context, cancellationToken);
    }

    private async Task CompensateStepAsync(SagaContext<TState> context, string stepName, CancellationToken cancellationToken)
    {
        if (!_compensations.TryGetValue(stepName, out var compensation))
        {
            _logger.LogWarning("No compensation registered for step {StepName} in Saga {SagaId}.", stepName, context.SagaId);
            return;
        }

        try
        {
            await compensation(context, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Compensation failed for step {StepName} in Saga {SagaId}.", stepName, context.SagaId);
        }
    }
}
