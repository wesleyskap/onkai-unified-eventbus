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

        // In RabbitMQ.Client 7+, CreateChannelAsync returns a Task<IChannel>
        using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        const string exchangeName = "Onkai.EventBus";
        var routingKey = envelope.Headers.TryGetValue("RoutingKeyOverride", out var keyObj) && keyObj is string rKey
            ? rKey
            : envelope.EventName;

        // Ensure exchange is declared
        await channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var properties = new BasicProperties
        {
            MessageId = envelope.EventId,
            CorrelationId = envelope.CorrelationId,
            Type = envelope.EventName
        };

        if (properties.Headers == null)
        {
            properties.Headers = new Dictionary<string, object?>();
        }

        foreach (var (headerKey, headerValue) in envelope.Headers)
        {
            properties.Headers[headerKey] = headerValue;
        }

        await channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: envelope.Body,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _connectionSemaphore.Dispose();
        _connection?.Dispose();
    }
}
