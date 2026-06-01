namespace Onkai.EventBus.Core.Subscription;

/// <summary>
/// Represents subscription metadata mapping an event type to a consumer type.
/// </summary>
public sealed record SubscriptionInfo(Type EventType, Type ConsumerType);
