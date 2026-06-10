using System.Collections.Generic;

namespace Onkai.EventBus.Core.Sagas;

/// <summary>
/// Holds the context of a Saga execution, tracking its status, custom data, and completed steps.
///
/// Example:
/// var context = new SagaContext&lt;OrderState&gt; { SagaId = "123" };
/// </summary>
/// <typeparam name="TState">The type of the custom saga state data.</typeparam>
public sealed class SagaContext<TState>
    where TState : class, new()
{
    /// <summary>
    /// Gets or sets the unique identifier of the Saga instance.
    /// </summary>
    public string SagaId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the Saga (e.g., "Started", "Completed", "Failed", "Compensated").
    /// </summary>
    public string Status { get; set; } = "Started";

    /// <summary>
    /// Gets or sets the custom state data containing business information for the Saga.
    /// </summary>
    public TState Data { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of event names that have been successfully processed in this Saga.
    /// </summary>
    public List<string> CompletedSteps { get; set; } = new();
}
