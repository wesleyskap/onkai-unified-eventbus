using Onkai.EventBus.Core.Transport;
using RabbitMQ.Client;

namespace Onkai.EventBus.RabbitMQ;

/// <summary>
/// A RabbitMQ-backed implementation of the <see cref="IMessageTransport"/> interface.
/// </summary>
public sealed class RabbitMqTransport : IMessageTransport, IDisposable
{
    private readonly ConnectionFactory _connectionFactory;
    private IConnection? _connection;
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the RabbitMqTransport class.
    /// </summary>
    /// <param name="connectionFactory">The factory to create connections.</param>
    public RabbitMqTransport(ConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection != null)
        {
            return _connection;
        }

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            _connection ??= await _connectionFactory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    private const string DelayExchangeName = "Onkai.EventBus.Delay";

    /// <inheritdoc />
    public async Task SendAsync(
        TransportEnvelope envelope,
        CancellationToken cancellationToken)
    {
        if (envelope is null)
        {
            throw new ArgumentNullException(nameof(envelope));
        }

        var connection = await GetConnectionAsync(cancellationToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        var routingKey = envelope.Headers.TryGetValue("RoutingKeyOverride", out var keyObj) && keyObj is string rKey ? rKey : envelope.EventName;
        var properties = CreateBasicProperties(envelope);

        if (envelope.Headers.TryGetValue("DelayMs", out var delayObj) && delayObj is long delayMs)
        {
            await PublishWithDelayAsync(channel, envelope, routingKey, delayMs, properties, cancellationToken);
            return;
        }

        await PublishDirectAsync(channel, envelope, routingKey, properties, cancellationToken);
    }

    private async Task PublishWithDelayAsync(
        IChannel channel,
        TransportEnvelope envelope,
        string routingKey,
        long delayMs,
        BasicProperties properties,
        CancellationToken cancellationToken)
    {
        var delayQueueName = $"Onkai.EventBus.Delay.{delayMs}";
        await DeclareDelayTopologyAsync(channel, delayQueueName, delayMs, routingKey, cancellationToken);

        await channel.BasicPublishAsync(
            exchange: DelayExchangeName,
            routingKey: $"delay.{delayMs}",
            mandatory: false,
            basicProperties: properties,
            body: envelope.Body,
            cancellationToken: cancellationToken);
    }

    private async Task DeclareDelayTopologyAsync(
        IChannel channel,
        string delayQueueName,
        long delayMs,
        string routingKey,
        CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: DelayExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await DeclareAndBindDelayQueueAsync(channel, delayQueueName, delayMs, routingKey, cancellationToken);
    }

    private async Task DeclareAndBindDelayQueueAsync(
        IChannel channel,
        string delayQueueName,
        long delayMs,
        string routingKey,
        CancellationToken cancellationToken)
    {
        var args = GetDelayQueueArguments(delayMs, routingKey);
        await channel.QueueDeclareAsync(queue: delayQueueName, durable: true, exclusive: false, autoDelete: false, arguments: args, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(queue: delayQueueName, exchange: DelayExchangeName, routingKey: $"delay.{delayMs}", cancellationToken: cancellationToken);
    }

    private Dictionary<string, object?> GetDelayQueueArguments(long delayMs, string routingKey)
    {
        return new Dictionary<string, object?>
        {
            { "x-message-ttl", delayMs },
            { "x-dead-letter-exchange", "Onkai.EventBus" },
            { "x-dead-letter-routing-key", routingKey }
        };
    }

    private async Task PublishDirectAsync(
        IChannel channel,
        TransportEnvelope envelope,
        string routingKey,
        BasicProperties properties,
        CancellationToken cancellationToken)
    {
        const string exchangeName = "Onkai.EventBus";
        await channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: envelope.Body,
            cancellationToken: cancellationToken);
    }

    private BasicProperties CreateBasicProperties(TransportEnvelope envelope)
    {
        var properties = new BasicProperties
        {
            MessageId = envelope.EventId,
            CorrelationId = envelope.CorrelationId,
            Type = envelope.EventName,
            Headers = new Dictionary<string, object?>()
        };

        foreach (var (headerKey, headerValue) in envelope.Headers)
        {
            properties.Headers[headerKey] = headerValue;
        }

        return properties;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _connectionSemaphore.Dispose();
        _connection?.Dispose();
    }
}
