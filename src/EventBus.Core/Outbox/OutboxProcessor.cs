using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Onkai.EventBus.Core.Transport;

namespace Onkai.EventBus.Core.Outbox;

/// <summary>
/// A background hosted service that polls the outbox store and publishes messages to the registered transport.
/// 
/// Example:
/// <code>
/// builder.Services.AddHostedService&lt;OutboxProcessor&gt;();
/// </code>
/// </summary>
public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;

    /// <summary>
    /// Initializes a new instance of the OutboxProcessor class.
    /// </summary>
    public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing outbox messages.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
        var transport = scope.ServiceProvider.GetRequiredService<IMessageTransport>();

        var pendingMessages = await store.GetUnpublishedMessagesAsync(cancellationToken);
        foreach (var message in pendingMessages)
        {
            await PublishMessageAsync(transport, store, message, cancellationToken);
        }
    }

    private async Task PublishMessageAsync(
        IMessageTransport transport,
        IOutboxStore store,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        var headers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(message.SerializedHeaders) 
            ?? new Dictionary<string, object>();

        var envelope = new TransportEnvelope
        {
            EventId = message.EventId,
            EventName = message.EventName,
            CorrelationId = message.CorrelationId,
            Body = message.Body,
            Headers = headers
        };

        await transport.SendAsync(envelope, cancellationToken);
        await store.MarkAsPublishedAsync(message.Id, cancellationToken);
    }
}
