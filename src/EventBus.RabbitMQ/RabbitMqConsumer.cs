using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Onkai.EventBus.Abstractions;
using Onkai.EventBus.Core.Serialization;
using Onkai.EventBus.Core.Subscription;
using Onkai.EventBus.Core.Transport;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Onkai.EventBus.RabbitMQ;

/// <summary>
/// A RabbitMQ implementation of <see cref="IMessageConsumer"/> for background event listening.
/// 
/// Example:
/// <code>
/// IMessageConsumer consumer = new RabbitMqConsumer(connectionFactory, subscriptionManager, serviceProvider, logger);
/// await consumer.StartConsumingAsync(cancellationToken);
/// </code>
/// </summary>
public sealed class RabbitMqConsumer : IMessageConsumer, IDisposable
{
    private const string ExchangeName = "Onkai.EventBus";
    private const string ErrorExchangeName = "Onkai.EventBus.Error";
    private static readonly ActivitySource ActivitySource = new("Onkai.EventBus");

    private readonly ConnectionFactory _connectionFactory;
    private readonly SubscriptionManager _subscriptionManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the RabbitMqConsumer class.
    /// </summary>
    public RabbitMqConsumer(
        ConnectionFactory connectionFactory,
        SubscriptionManager subscriptionManager,
        IServiceProvider serviceProvider,
        ILogger<RabbitMqConsumer> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    /// <inheritdoc />
    public async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await DeclareExchangesAsync(cancellationToken);
        await SetupErrorQueueAsync(cancellationToken);
        await RegisterConsumersAsync(cancellationToken);
    }

    private async Task DeclareExchangesAsync(CancellationToken cancellationToken)
    {
        if (_channel == null) return;
        await _channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: ErrorExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
    }

    private async Task SetupErrorQueueAsync(CancellationToken cancellationToken)
    {
        if (_channel == null) return;
        var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "onkai-unified-bus";
        var errorQueueName = $"{appName}.Error";

        await _channel.QueueDeclareAsync(
            queue: errorQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: errorQueueName,
            exchange: ErrorExchangeName,
            routingKey: string.Empty,
            cancellationToken: cancellationToken);
    }

    private async Task RegisterConsumersAsync(CancellationToken cancellationToken)
    {
        var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "onkai-unified-bus";
        foreach (var eventName in _subscriptionManager.GetRegisteredEventNames())
        {
            await SetupQueueAndBindAsync(appName, eventName, cancellationToken);
        }
    }

    private async Task SetupQueueAndBindAsync(string appName, string eventName, CancellationToken cancellationToken)
    {
        var queueName = $"{appName}.{eventName}";
        await DeclareMainQueueAsync(queueName, cancellationToken);
        await BindMainQueueAsync(queueName, eventName, cancellationToken);
        await StartBasicConsumeAsync(queueName, cancellationToken);
    }

    private async Task DeclareMainQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        if (_channel == null) throw new InvalidOperationException("RabbitMQ channel is not initialized.");
        var arguments = new Dictionary<string, object?> { { "x-dead-letter-exchange", ErrorExchangeName } };
        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: arguments,
            cancellationToken: cancellationToken);
    }

    private async Task BindMainQueueAsync(string queueName, string eventName, CancellationToken cancellationToken)
    {
        if (_channel == null) throw new InvalidOperationException("RabbitMQ channel is not initialized.");
        await _channel.QueueBindAsync(
            queue: queueName,
            exchange: ExchangeName,
            routingKey: eventName,
            cancellationToken: cancellationToken);
    }

    private async Task StartBasicConsumeAsync(string queueName, CancellationToken cancellationToken)
    {
        if (_channel == null)
        {
            return;
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        var eventName = eventArgs.BasicProperties.Type ?? string.Empty;
        var correlationId = eventArgs.BasicProperties.CorrelationId ?? Guid.NewGuid().ToString();
        var headers = ExtractHeaders(eventArgs.BasicProperties);

        try
        {
            var success = await TryProcessEventWithRetryAsync(eventName, correlationId, headers, eventArgs.Body.ToArray());
            await AckOrNackAsync(eventArgs.DeliveryTag, success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing event {EventName}", eventName);
            await AckOrNackAsync(eventArgs.DeliveryTag, false);
        }
    }

    private IReadOnlyDictionary<string, object> ExtractHeaders(IReadOnlyBasicProperties properties)
    {
        var result = new Dictionary<string, object>();
        if (properties.Headers == null)
        {
            return result;
        }

        foreach (var (key, value) in properties.Headers)
        {
            if (value != null)
            {
                result[key] = value;
            }
        }

        return result;
    }

    private async Task AckOrNackAsync(ulong deliveryTag, bool success)
    {
        if (_channel == null)
        {
            return;
        }

        if (success)
        {
            await _channel.BasicAckAsync(deliveryTag, false);
            return;
        }

        await _channel.BasicNackAsync(deliveryTag, false, false);
    }

    private async Task<bool> TryProcessEventWithRetryAsync(
        string eventName,
        string correlationId,
        IReadOnlyDictionary<string, object> headers,
        byte[] body)
    {
        const int maxAttempts = 3;
        var attempt = 0;

        while (attempt < maxAttempts)
        {
            attempt++;
            if (await InvokeConsumerPipelineAsync(eventName, correlationId, headers, body))
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }

        return false;
    }

    private async Task<bool> InvokeConsumerPipelineAsync(
        string eventName,
        string correlationId,
        IReadOnlyDictionary<string, object> headers,
        byte[] body)
    {
        var traceparent = headers.TryGetValue("traceparent", out var val) ? val?.ToString() : null;
        using var activity = StartConsumerActivity(eventName, traceparent);

        using var scope = _serviceProvider.CreateScope();
        var eventType = _subscriptionManager.GetEventTypeByName(eventName);
        if (eventType == null)
        {
            _logger.LogWarning("No registered event type found for name '{EventName}'", eventName);
            return false;
        }

        var serializer = scope.ServiceProvider.GetRequiredService<IEventSerializer>();
        var stronglyTypedEvent = GetDeserializedEvent(serializer, body, eventType, eventName);
        return await DispatchToSubscribersAsync(scope, stronglyTypedEvent, eventName, correlationId, headers);
    }

    private Activity? StartConsumerActivity(string eventName, string? traceparent)
    {
        return traceparent != null
            ? ActivitySource.StartActivity($"Onkai.EventBus.Consume {eventName}", ActivityKind.Consumer, traceparent)
            : ActivitySource.StartActivity($"Onkai.EventBus.Consume {eventName}", ActivityKind.Consumer);
    }

    private IEvent GetDeserializedEvent(IEventSerializer serializer, byte[] body, Type eventType, string eventName)
    {
        var deserialized = serializer.Deserialize(body, eventType);
        if (deserialized is not IEvent stronglyTypedEvent)
        {
            throw new InvalidCastException($"Deserialized message payload for '{eventName}' is not of type {nameof(IEvent)}.");
        }
        return stronglyTypedEvent;
    }

    private async Task<bool> DispatchToSubscribersAsync(
        IServiceScope scope,
        IEvent stronglyTypedEvent,
        string eventName,
        string correlationId,
        IReadOnlyDictionary<string, object> headers)
    {
        var subscriberTypes = _subscriptionManager.GetHandlersForEvent(eventName);
        var context = new ConsumeContext
        {
            CorrelationId = correlationId,
            Headers = headers
        };

        foreach (var subType in subscriberTypes)
        {
            var subscriber = scope.ServiceProvider.GetRequiredService(subType);
            await InvokeSubscriberAsync(subscriber, stronglyTypedEvent, context, subType);
        }

        return true;
    }

    private async Task InvokeSubscriberAsync(
        object subscriber,
        IEvent stronglyTypedEvent,
        ConsumeContext context,
        Type subType)
    {
        var method = subType.GetMethod("ConsumeAsync");
        if (method == null)
        {
            throw new InvalidOperationException($"Subscriber type {subType.Name} does not implement ConsumeAsync.");
        }

        var task = (Task)method.Invoke(subscriber, [stronglyTypedEvent, context, CancellationToken.None])!;
        await task;
    }

    /// <inheritdoc />
    public async Task StopConsumingAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
        {
            await _channel.CloseAsync(cancellationToken);
        }

        if (_connection != null)
        {
            await _connection.CloseAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _channel?.Dispose();
        _connection?.Dispose();
        _isDisposed = true;
    }
}
