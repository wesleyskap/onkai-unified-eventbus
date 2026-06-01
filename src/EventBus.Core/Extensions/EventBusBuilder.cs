using Microsoft.Extensions.DependencyInjection;

namespace Onkai.EventBus.Core.Extensions;

/// <summary>
/// Builder to facilitate chaining dependency injection registrations for the event bus.
/// </summary>
public sealed class EventBusBuilder
{
    /// <summary>
    /// Gets the service collection.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Initializes a new instance of the EventBusBuilder class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public EventBusBuilder(IServiceCollection services)
    {
        Services = services;
    }
}
