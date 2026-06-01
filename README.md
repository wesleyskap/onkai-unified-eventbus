# Onkai.EventBus

Broker-agnostic event bus framework for .NET applications. Build highly decoupled, message-driven architectures that support swapping transport providers (e.g., RabbitMQ, Kafka, Azure Service Bus) with zero changes to your application domain services.

[![Build & Test](https://github.com/onkai/onkai-unified-bus/actions/workflows/dotnet.yml/badge.svg)](https://github.com/onkai/onkai-unified-bus)
[![NuGet Version](https://img.shields.io/nuget/v/Onkai.EventBus.Abstractions.svg)](https://www.nuget.org/packages/Onkai.EventBus.Abstractions)

---

## Features

- **Broker Agnostic**: Switch between RabbitMQ and future providers by changing a single line in your Composition Root.
- **CA**: Absolute decoupling of business logic from broker SDKs.
- **Zero Interface Slicing**: Dynamic runtime type serialization prevents property loss during inheritance and interface passing.
- **Built-in Correlation**: Automatic tracing with correlation IDs propagated inside message headers.
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
