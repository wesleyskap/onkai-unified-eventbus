# Onkai.EventBus

Broker-agnostic event bus framework for .NET applications. Build highly decoupled, message-driven architectures that support swapping transport providers (e.g., RabbitMQ, Kafka, Azure Service Bus) with zero changes to your application domain services.

[![Build & Test](https://github.com/onkai/onkai-unified-bus/actions/workflows/dotnet.yml/badge.svg)](https://github.com/onkai/onkai-unified-bus)
[![NuGet Version](https://img.shields.io/nuget/v/Onkai.EventBus.Abstractions.svg)](https://www.nuget.org/packages/Onkai.EventBus.Abstractions)

---

## Features

- **Broker Agnostic**: Switch between RabbitMQ and future providers by changing a single line in your Composition Root.
- **Background Message Consumption**: Built-in `IHostedService` that boots up message listeners, handles automatic queue declaration & exchange binding (topology), manages message confirmation (ACK/NACK), and resolves consumers in isolated DI scopes with exponential backoff.
- **Transactional Outbox**: Guarantees At-Least-Once event delivery by persisting messages inside the local business transactions using `.UseOutbox<TStore>()`.
- **Resilient DLQ Routing**: Automatically configures RabbitMQ Dead Letter Exchanges (DLX) and routes failed messages to `{AppName}.Error` after local retries exhaustion.
- **Distributed Tracing**: Native OpenTelemetry trace propagation using `ActivitySource` (W3C `traceparent` headers).
- **Scheduled Messages**: Support for sending events with an optional delay (`TimeSpan`) using native TTL & dead-lettering queues (no broker plugins required).
- **Inbox Pattern**: Easy idempotency enforcement with `IdempotentConsumer<TEvent>` and customizable `IInboxStore` to prevent duplicate message handling.
- **High Performance / Native AOT**: Reflection-free dispatching using `IEventConsumerExecutor` direct casting and AOT source generator friendly `JsonSerializerOptions` injection.
- **Zero Interface Slicing**: Dynamic runtime type serialization prevents property loss during inheritance and interface passing.
- **Tests**: Comes with in-memory fakes to test publishing and subscription routing without spinning up brokers.
- **DI Integration**: Fluent APIs designed to fit seamlessly with standard `IServiceCollection` hosting.

---

## Packages & Versions

The framework is split into smaller packages to prevent dependency leakage:

| Package | NuGet Version | Description | Target Framework |
|---|---|---|---|
| **Onkai.EventBus.Abstractions** | `1.0.0` | Event marker interfaces, publishing contracts, and basic metadata. | `.NET 10.0` / `.NET 9.0` |
| **Onkai.EventBus.Core** | `1.0.0` | Base publisher implementation, serialization wrappers, and subscription registry. | `.NET 10.0` / `.NET 9.0` |
| **Onkai.EventBus.RabbitMQ** | `1.0.0` | RabbitMQ provider using asynchronous APIs of `RabbitMQ.Client` 7.x. | `.NET 10.0` / `.NET 9.0` |

---

## Installation

Install the packages using the .NET CLI:

```bash
# Core abstractions for your Domain/Application projects:
dotnet add package Onkai.EventBus.Abstractions

# Core execution logic for your Infrastructure project:
dotnet add package Onkai.EventBus.Core

# RabbitMQ provider for your Composition Root/Web API project:
dotnet add package Onkai.EventBus.RabbitMQ
```

---

## Quick Start

### 1. Define an Event

Events must be immutable records implementing the `IEvent` marker interface:

```csharp
using Onkai.EventBus.Abstractions;

public sealed record OrderCreatedEvent(Guid OrderId, decimal Amount, string CustomerEmail) : IEvent;
```

### 2. Configure Dependency Injection

Configure the EventBus and your chosen provider in your `Program.cs` or startup Composition Root:

```csharp
using Onkai.EventBus.Core.Extensions;
using Onkai.EventBus.RabbitMQ.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register Core EventBus and the RabbitMQ provider
builder.Services.AddEventBus()
                .UseRabbitMq(config =>
                {
                    config.HostName = "localhost";
                    config.UserName = "guest";
                    config.Password = "guest";
                });
```

### 3. Publish an Event

Inject `IEventPublisher` in your application services and call `PublishAsync`:

```csharp
using Onkai.EventBus.Abstractions;

public sealed class OrderService
{
    private readonly IEventPublisher _publisher;

    public OrderService(IEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task CreateOrderAsync(CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid();
        var orderEvent = new OrderCreatedEvent(orderId, 150.50m, "customer@example.com");

        // Publishes to the configured broker (e.g. RabbitMQ exchange "Onkai.EventBus")
        await _publisher.PublishAsync(orderEvent, cancellationToken: cancellationToken);
    }
}
```

---

## Advanced Usage

### Correlation & Custom Metadata Headers

Pass trace correlation IDs and custom headers through `PublishOptions`:

```csharp
var options = new PublishOptions
{
    CorrelationId = "my-custom-trace-id",
    RoutingKey = "orders.v1.created" // Optional routing key override
};

options.Headers.Add("TenantId", "tenant-123");

await _publisher.PublishAsync(orderEvent, options, cancellationToken);
```

### Scheduled / Delayed Messages

Send events to be delivered in the future using standard RabbitMQ TTL queues:

```csharp
var options = new PublishOptions
{
    Delay = TimeSpan.FromMinutes(10) // Delays execution by 10 minutes
};

await _publisher.PublishAsync(orderEvent, options, cancellationToken);
```

### Transactional Outbox Pattern

Guarantees message delivery consistency by writing events to a local database outbox store inside the business transaction:

```csharp
// Register in Composition Root:
builder.Services.AddEventBus()
                .UseRabbitMq(...)
                .UseOutbox<MyDatabaseOutboxStore>(); // Implement IOutboxStore interface
```

### Defining a Consumer

Implement `IEventConsumer<TEvent>` to handle incoming events:

```csharp
using Onkai.EventBus.Abstractions;

public sealed class OrderCreatedConsumer : IEventConsumer<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task ConsumeAsync(OrderCreatedEvent @event, ConsumeContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing order {OrderId} with TraceId {TraceId}", 
            @event.OrderId, 
            context.CorrelationId);

        return Task.CompletedTask;
    }
}
```

Register the consumer during DI configuration:

```csharp
builder.Services.AddEventBus()
                .UseRabbitMq(...)
                .AddConsumer<OrderCreatedEvent, OrderCreatedConsumer>();
```

### Idempotent Consumers (Inbox Pattern)

Inherit from `IdempotentConsumer<TEvent>` and supply a custom `IInboxStore` to prevent duplicate message handling:

```csharp
using Onkai.EventBus.Core.Inbox;

public sealed class OrderCreatedConsumer : IdempotentConsumer<OrderCreatedEvent>
{
    public OrderCreatedConsumer(IInboxStore store) : base(store) { }

    protected override Task ConsumeIdempotentAsync(
        OrderCreatedEvent @event, 
        ConsumeContext context, 
        CancellationToken cancellationToken)
    {
        // Business logic runs here exactly once
        return Task.CompletedTask;
    }
}
### Saga Orchestration (Sagas)

Coordinate distributed transaction steps and register automated compensation rollbacks using `SagaOrchestrator<TState>`:

```csharp
// 1. Register state & store in DI:
builder.Services.AddEventBus()
                .AddSaga<OrderSagaState, InMemorySagaStateStore<OrderSagaState>>();

// 2. Register step compensations and execute steps inside consumer:
public sealed class ReserveStockConsumer : IEventConsumer<ReserveStockEvent>
{
    private readonly SagaOrchestrator<OrderSagaState> _orchestrator;

    public ReserveStockConsumer(SagaOrchestrator<OrderSagaState> orchestrator)
    {
        _orchestrator = orchestrator;
        _orchestrator.RegisterCompensation(nameof(ReserveStockEvent), async (ctx, token) =>
        {
            // Compensation logic rolls back stock reservation
            ctx.Data.StockReserved = false;
        });
    }

    public Task ConsumeAsync(ReserveStockEvent @event, ConsumeContext context, CancellationToken cancellationToken)
    {
        return _orchestrator.ExecuteStepAsync(
            context.CorrelationId,
            @event,
            async (ctx, ev, token) =>
            {
                ctx.Data.StockReserved = true;
            },
            cancellationToken);
    }
}
```

---

## Running Tests

We implement unit tests using an in-memory `FakeTransport` to guarantee fast execution without needing a running broker. Run tests with:

```bash
dotnet test
```

---

## Extending the EventBus (SOLID Principles)

To add another message broker (such as Kafka):
1. Create a new library project `Onkai.EventBus.Kafka`.
2. Implement the `IMessageTransport` interface from `Onkai.EventBus.Core.Transport`.
3. Wrap your Kafka Producer SDK inside the implementation.
4. Expose a Composition Root extension method (e.g., `UseKafka(...)`) to wire it into the `EventBusBuilder.Services` collection.
