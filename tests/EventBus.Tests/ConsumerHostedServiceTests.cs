using Microsoft.Extensions.DependencyInjection;
using Onkai.EventBus.Core.Extensions;
using Onkai.EventBus.Core.Transport;

namespace Onkai.EventBus.Tests;

/// <summary>
/// A fake message consumer implementation to verify hosted service orchestration.
/// </summary>
public sealed class FakeMessageConsumer : IMessageConsumer
{
    /// <summary>
    /// Gets a value indicating whether StartConsumingAsync was called.
    /// </summary>
    public bool StartCalled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether StopConsumingAsync was called.
    /// </summary>
    public bool StopCalled { get; private set; }

    /// <inheritdoc />
    public Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        StartCalled = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopConsumingAsync(CancellationToken cancellationToken)
    {
        StopCalled = true;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Unit tests for EventBusHostedService.
/// </summary>
public class ConsumerHostedServiceTests
{
    [Fact]
    public async Task StartAsync_ShouldCallStartConsumingOnAllConsumers()
    {
        var consumer = new FakeMessageConsumer();
        var hostedService = new EventBusHostedService([consumer]);

        await hostedService.StartAsync(CancellationToken.None);

        Assert.True(consumer.StartCalled);
    }

    [Fact]
    public async Task StopAsync_ShouldCallStopConsumingOnAllConsumers()
    {
        var consumer = new FakeMessageConsumer();
        var hostedService = new EventBusHostedService([consumer]);

        await hostedService.StopAsync(CancellationToken.None);

        Assert.True(consumer.StopCalled);
    }

    [Fact]
    public void AddEventBus_ShouldRegisterHostedServiceAndCoreComponents()
    {
        var services = new ServiceCollection();

        services.AddEventBus();

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>();

        Assert.Contains(hostedServices, s => s is EventBusHostedService);
    }
}
