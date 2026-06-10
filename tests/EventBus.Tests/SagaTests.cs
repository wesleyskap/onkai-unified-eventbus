using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Onkai.EventBus.Abstractions;
using Onkai.EventBus.Core.Sagas;
using Xunit;

namespace Onkai.EventBus.Tests;

/// <summary>
/// A test event representing stock reservation.
/// </summary>
public sealed record ReserveStockEvent(string OrderId) : IEvent;

/// <summary>
/// A test event representing payment processing.
/// </summary>
public sealed record ProcessPaymentEvent(string OrderId) : IEvent;

/// <summary>
/// Holds state for the Order Saga unit tests.
/// </summary>
public sealed class OrderSagaState
{
    /// <summary>
    /// Gets or sets the order identifier.
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether stock has been reserved.
    /// </summary>
    public bool StockReserved { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether payment has been processed.
    /// </summary>
    public bool PaymentProcessed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether stock compensation has run.
    /// </summary>
    public bool StockCompensated { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether payment compensation has run.
    /// </summary>
    public bool PaymentCompensated { get; set; }
}

/// <summary>
/// Unit tests for the Saga Orchestrator.
/// </summary>
public class SagaTests
{
    [Fact]
    public async Task Saga_ShouldCompleteSuccessfully_WhenAllStepsSucceed()
    {
        var store = new InMemorySagaStateStore<OrderSagaState>();
        var orchestrator = new SagaOrchestrator<OrderSagaState>(store, NullLogger<SagaOrchestrator<OrderSagaState>>.Instance);
        var orderId = "order-123";

        await ExecuteStockStepAsync(orchestrator, orderId);
        await ExecutePaymentStepAsync(orchestrator, orderId);

        var context = await store.GetAsync(orderId, CancellationToken.None);
        Assert.NotNull(context);
        Assert.True(context.Data.StockReserved);
        Assert.True(context.Data.PaymentProcessed);
        Assert.Equal("Started", context.Status);
    }

    [Fact]
    public async Task Saga_ShouldCompensateCompletedStepsInReverseOrder_WhenAStepFails()
    {
        var store = new InMemorySagaStateStore<OrderSagaState>();
        var orchestrator = new SagaOrchestrator<OrderSagaState>(store, NullLogger<SagaOrchestrator<OrderSagaState>>.Instance);
        var orderId = "order-456";

        SetupCompensations(orchestrator);
        await ExecuteStockStepAsync(orchestrator, orderId);
        await ExecuteFailingPaymentStepAsync(orchestrator, orderId);

        var context = await store.GetAsync(orderId, CancellationToken.None);
        Assert.NotNull(context);
        Assert.Equal("Compensated", context.Status);
        Assert.True(context.Data.StockCompensated);
        Assert.True(context.Data.PaymentCompensated);
    }

    private static async Task ExecuteStockStepAsync(
        SagaOrchestrator<OrderSagaState> orchestrator,
        string orderId)
    {
        await orchestrator.ExecuteStepAsync(
            orderId,
            new ReserveStockEvent(orderId),
            (ctx, ev, token) =>
            {
                ctx.Data.OrderId = ev.OrderId;
                ctx.Data.StockReserved = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);
    }

    private static async Task ExecutePaymentStepAsync(
        SagaOrchestrator<OrderSagaState> orchestrator,
        string orderId)
    {
        await orchestrator.ExecuteStepAsync(
            orderId,
            new ProcessPaymentEvent(orderId),
            (ctx, ev, token) =>
            {
                ctx.Data.PaymentProcessed = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);
    }

    private static async Task ExecuteFailingPaymentStepAsync(
        SagaOrchestrator<OrderSagaState> orchestrator,
        string orderId)
    {
        await orchestrator.ExecuteStepAsync(
            orderId,
            new ProcessPaymentEvent(orderId),
            (ctx, ev, token) => throw new InvalidOperationException("Payment failed."),
            CancellationToken.None);
    }

    private static void SetupCompensations(SagaOrchestrator<OrderSagaState> orchestrator)
    {
        orchestrator.RegisterCompensation("ReserveStockEvent", (ctx, token) =>
        {
            ctx.Data.StockCompensated = true;
            return Task.CompletedTask;
        });
        orchestrator.RegisterCompensation("ProcessPaymentEvent", (ctx, token) =>
        {
            ctx.Data.PaymentCompensated = true;
            return Task.CompletedTask;
        });
    }
}
