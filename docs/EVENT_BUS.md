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
  - `Transport/`: Internal infrastructure contracts like `IMessageTransport` and the generic message envelope `TransportEnvelope`.
  - `Serialization/`: Serialization abstraction `IEventSerializer` and default `JsonEventSerializer`.
  - `Subscription/`: Subscription registry `SubscriptionManager` and DI registration helper `SubscriptionInfo`.
  - `Extensions/`: Fluent `EventBusBuilder` and helper extension methods to register services and consumers.
- **`src/EventBus.RabbitMQ/`**: Specific RabbitMQ provider module.
  - `RabbitMqTransport.cs`: Adapter wrapping the RabbitMQ SDK (`RabbitMQ.Client` 7.x) to publish messages.
  - `Extensions/RabbitMqExtensions.cs`: Setup extensions for registering the RabbitMQ transport during startup.
- **`tests/EventBus.Tests/`**: Unit test suite focusing on fundamental components validation.
  - `FakeTransport.cs`: In-memory fake transport for isolated unit testing (no active broker required).
  - `PublisherTests.cs`: Tests covering publishing, runtime type serialization, Correlation ID preservation, and subscription registrations.

## Integration Details & Flow

### Publishing Flow
1. The application injects `IEventPublisher` and calls `PublishAsync(@event, options)`.
2. The default publisher serializes the payload dynamically using its runtime type (`event.GetType()`) to avoid property truncation caused by interface/inheritance slicing.
3. A unique Event ID (Guid) and Correlation ID are generated for distributed tracing.
4. The payload and metadata are wrapped in a `TransportEnvelope`.
5. The envelope is passed to the active `IMessageTransport` implementation (e.g., `RabbitMqTransport`).
6. The transport declares the required infrastructure (e.g., exchange `Onkai.EventBus`) and publishes the message asynchronously.

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
*   **Third-party SDK Abstraction**: No class outside of `EventBus.RabbitMQ` references types from the `RabbitMQ.Client` library (such as `IChannel` or `IConnection`). All interactions with the broker SDK are encapsulated behind the `RabbitMqTransport` adapter.

### 5. Best Practices, Performance, and Resilience
*   **No Service Locator**: Dependencies must be resolved exclusively via constructor injection; avoid using `IServiceProvider.GetService(...)` inside EventBus core logic.
*   **Structured Logging**: Event logs must include metadata (like `CorrelationId` and `EventName`) in a structured format rather than plain string messages.
