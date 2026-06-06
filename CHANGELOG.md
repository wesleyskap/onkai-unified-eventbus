# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.0] - 2026-06-06

### Added
- **Scheduled / Delayed Messages**: Support for delaying event delivery using TimeSpan delay parameter on `PublishOptions`, mapping to `DelayMs` header.
- **RabbitMQ Delayed Topology**: Native TTL and Dead Letter Exchange (DLX) queue-based delayed routing in `RabbitMqTransport` (does not require RabbitMQ delay plugin).
- **Inbox Pattern (Idempotency)**: Added `IInboxStore` interface and `IdempotentConsumer` abstract decorator class in `EventBus.Core` to avoid duplicate processing.
- **MessageId Propagation**: Added `MessageId` property to `ConsumeContext` and populated it in `RabbitMqConsumer` from the incoming message properties.
- **Fase 3 Unit Tests**: Added unit tests in `InboxAndDelayTests.cs` validating `IdempotentConsumer` validation and `DelayMs` propagation in publish options.

### Changed
- **OutboxPublisher**: Updated `CreateOutboxMessage` to support and propagate message delay configurations.

## [1.2.0] - 2026-06-04

### Added
- **Dead Letter Queue (DLQ)**: Automatic queue and binding topology for `Onkai.EventBus.Error` exchange and `{AppName}.Error` error queue in RabbitMQ, with NACK error routing logic.
- **Distributed Tracing (OpenTelemetry)**: Added support for `ActivitySource` in publishing and consuming pipelines to propagate W3C `traceparent` headers.
- **Transactional Outbox Pattern**: Added `OutboxMessage` entity, `IOutboxStore` abstraction, `OutboxPublisher` decorator, and `OutboxProcessor` background polling hosted service.
- **Outbox DI Extensions**: Added `.UseOutbox<TStore>()` fluent registration on `EventBusBuilder`.
- **Fase 2 Unit Tests**: Added unit tests in `OutboxAndTracingTests.cs` using `FakeOutboxStore` and `ActivityListener` mocks.

### Changed
- **EventPublisher**: Extracted envelope creation logic into a private helper and integrated publishing trace activities.
- **RabbitMqConsumer**: Refactored queue setup and processing into smaller SRP-compliant methods and integrated consumer trace activity scoping.

## [1.1.0] - 2026-06-04

### Added
- **Background Message Consumption**: Implemented `IMessageConsumer` and `EventBusHostedService` to run asynchronous message listening loops integrated with .NET `IHostedService` life cycle.
- **RabbitMQ Consumer**: Added `RabbitMqConsumer` implementing asynchronous queue listening using `RabbitMQ.Client` 7.x.
- **Automatic Topology Management**: Auto-declaration of queues (formatted as `{AppName}.{EventName}`) and automatic binding to exchange.
- **DI Scoped Execution**: Message delivery automatically spawns a scoped container (`CreateScope`) to resolve and execute transient consumers (`IEventConsumer<TEvent>`).
- **Resilience Policy**: Built-in retry mechanism with exponential backoff for transient consumer errors.
- **Consumer Unit Tests**: Added unit tests to verify the hosted service and core container registration logic.

### Changed
- **SubscriptionManager**: Updated constructor to accept DI-supplied `SubscriptionInfo` lists and automatically populate registered handlers on startup. Added `GetRegisteredEventNames()` helper method.
