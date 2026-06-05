# Onkai Unified EventBus

## Overview
The **Onkai Unified EventBus** is a broker-agnostic messaging framework designed to decouple business logic from physical message broker implementations (such as RabbitMQ, Kafka, or Azure Service Bus). It enables applications to send and receive asynchronous events using strongly-typed abstractions, allowing transparent substitution of the transport middleware without impacting domain logic.

## Code Structure
Mapping of the created projects and their roles in the solution architecture:

- **`src/EventBus.Abstractions/`**: Stable public contracts and base types.
  - `IEvent.cs`: Marker interface for events (which should be immutable records).
  - `IEventPublisher.cs`: Contract for publishing events.
  - `IEventConsumer.cs`: Marker and generic (`IEventConsumer<TEvent>`) interfaces for event handlers.
  - `PublishOptions.cs` and `ConsumeContext.cs`: Metadata like Correlation ID and custom headers.
- **`src/EventBus.Core/`**: Broker-neutral core implementations and DI orchestration.
  - `Transport/`: Internal infrastructure contracts like `IMessageTransport`, `IMessageConsumer`, and the generic message envelope `TransportEnvelope`.
  - `Serialization/`: Serialization abstraction `IEventSerializer` and default `JsonEventSerializer`.
  - `Subscription/`: Subscription registry `SubscriptionManager` and DI registration helper `SubscriptionInfo`.
  - `Outbox/`: Interfaces and components for Transactional Outbox pattern (`OutboxMessage`, `IOutboxStore`, `OutboxPublisher`, `OutboxProcessor`).
  - `Extensions/`: Fluent `EventBusBuilder`, helper extension methods to register services, and the generic `EventBusHostedService` background worker.
- **`src/EventBus.RabbitMQ/`**: Specific RabbitMQ provider module.
  - `RabbitMqTransport.cs`: Adapter wrapping the RabbitMQ SDK (`RabbitMQ.Client` 7.x) to publish messages.
  - `RabbitMqConsumer.cs`: Adapter implementation of `IMessageConsumer` managing consumer connection, channel, topology building, and event dispatching.
  - `Extensions/RabbitMqExtensions.cs`: Setup extensions for registering the RabbitMQ transport and consumer during startup.
- **`tests/EventBus.Tests/`**: Unit test suite focusing on fundamental components validation.
  - `FakeTransport.cs`: In-memory fake transport for isolated unit testing (no active broker required).
  - `PublisherTests.cs`: Tests covering publishing, runtime type serialization, Correlation ID preservation, and subscription registrations.
  - `ConsumerHostedServiceTests.cs`: Tests validating hosted service orchestration and DI registration.

## Integration Details & Flow

### Publishing Flow
1. The application injects `IEventPublisher` and calls `PublishAsync(@event, options)`.
2. The default publisher serializes the payload dynamically using its runtime type (`event.GetType()`) to avoid property truncation caused by interface/inheritance slicing.
3. A unique Event ID (Guid) and Correlation ID are generated for distributed tracing.
4. The payload and metadata are wrapped in a `TransportEnvelope`.
5. The envelope is passed to the active `IMessageTransport` implementation (e.g., `RabbitMqTransport`).
6. The transport declares the required infrastructure (e.g., exchange `Onkai.EventBus`) and publishes the message asynchronously.

### Consuming Flow
1. On application startup, the generic `EventBusHostedService` invokes `StartConsumingAsync` on the registered `IMessageConsumer` (e.g., `RabbitMqConsumer`).
2. The consumer initializes a connection and channel to the broker, declares the `Onkai.EventBus` exchange, and builds topology.
3. **Topology Binding**: For each registered event name, the consumer automatically declares a queue (named `{AppName}.{EventName}`) and binds it to the exchange with the matching routing key.
4. An `AsyncEventingBasicConsumer` is registered on each queue to receive incoming messages.
5. On message delivery:
   - Correlation ID, headers, and event name are extracted from BasicProperties.
   - The message processing runs inside a retry loop (3 attempts with exponential backoff).
   - For each processing attempt, a new Dependency Injection scope (`CreateScope()`) is created.
   - The payload is deserialized to its target event type.
   - The transient consumer instance (`IEventConsumer<TEvent>`) is resolved from the scope and `ConsumeAsync` is executed.
   - If execution succeeds, the message is acknowledged (ACK); if all retry attempts fail, the message is rejected (NACK with requeue).

### Transactional Outbox Flow
1. The application registers Outbox via `.UseOutbox<TStore>()`.
2. When calling `PublishAsync`, `OutboxPublisher` serializes the event and writes an `OutboxMessage` to the registered `IOutboxStore` (using the same local database transaction).
3. The background service `OutboxProcessor` polls `IOutboxStore` for unpublished messages.
4. For each pending message, it sends the envelope through `IMessageTransport` and calls `MarkAsPublishedAsync` to finalize the delivery.

---

## How to Evolve / Extend the Feature (Best Practices & SOLID)

Software engineering guidelines to evolve and extend the EventBus framework securely:

### 1. Single Responsibility Principle (SRP)
*   **Infrastructure vs Domain Separation**: Application layers depend strictly on `IEventPublisher` and remain unaware of infrastructure or serialization details.
*   **Focused Methods**: Methods handling serialization, publishing, and dispatching perform a single action. Avoid mixing complex connection-retrying policies inside the same physical blocks publishing the messages.

### 2. Open/Closed Principle (OCP) & Liskov Substitution Principle (LSP)
*   **Broker Extensibility (Strategy/Adapter)**: To add support for new brokers (like Kafka or Azure Service Bus), simply create a new project implementing `IMessageTransport` and expose a corresponding builder extension method (e.g., `.UseKafka()`). The core EventBus projects remain unchanged.
*   **Transport Substitutability (LSP)**: The `FakeTransport` behaves identically to real transport implementations in tests, allowing seamless unit testing without introducing external dependencies.

### 3. Interface Segregation Principle (ISP)
*   **Segregated Interfaces**: Clients that only publish events depend exclusively on `IEventPublisher`. Registry and subscription concerns are isolated within `SubscriptionManager`, preventing unnecessary methods from leaking to application components.

### 4. Dependency Inversion Principle (DIP)
*   **Third-party SDK Abstraction**: No class outside of `EventBus.RabbitMQ` references types from the `RabbitMQ.Client` library (such as `IChannel` or `IConnection`). All interactions with the broker SDK are encapsulated behind the `RabbitMqTransport` and `RabbitMqConsumer` adapters.

### 5. Best Practices, Performance, and Resilience
*   **No Service Locator**: Dependencies must be resolved exclusively via constructor injection; avoid using `IServiceProvider.GetService(...)` inside EventBus core logic.
*   **Structured Logging**: Event logs must include metadata (like `CorrelationId` and `EventName`) in a structured format rather than plain string messages.
