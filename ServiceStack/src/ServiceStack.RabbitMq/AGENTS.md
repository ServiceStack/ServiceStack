# CONTEXT: ServiceStack.RabbitMq

## Purpose

`ServiceStack.RabbitMq` provides RabbitMQ message broker integration for ServiceStack. It enables high-throughput, asynchronous, and reliable messaging architectures between distributed microservices using RabbitMQ.

Key capabilities include:
- **`RabbitMqServer` Host**: A message queue server host that continuously listens to RabbitMQ queues, dispatches inbound messages to ServiceStack services, and publishes responses to reply-to or notification queues.
- **Message Producer & Consumer Clients**: `RabbitMqProducer`, `RabbitMqQueueClient`, and `RabbitMqMessageFactory` implementing `IMessageService`, `IMessageProducer`, and `IMessageQueueClient`.
- **Automatic Exchange/Queue Provisioning**: Dynamic declaration and routing topology setup for standard request, response, and dead-letter (`.dlq`) queues.
- **Resilient Reconnection & Error Recovery**: Automatic recovery from broker disconnections, missing exchange errors (HTTP 404), and unacknowledged messages.

**Target frameworks**: `net472;net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack.Interfaces     ServiceStack.Common     ServiceStack (Core)
         │                          │                       │
         └──────────────────────────┼───────────────────────┘
                                    ▼
                         ServiceStack.RabbitMq
                                    │
                                    ▼
             Distributed Enterprise Messaging & Event Brokers
                      (RabbitMQ AMQP 0-9-1)
```

- **Depends on**: `ServiceStack`, `ServiceStack.Interfaces`, `ServiceStack.Common`, `ServiceStack.Client`, `ServiceStack.Text`, `RabbitMQ.Client` (v6.8.1).
- **Depended on by**: Distributed microservice systems leveraging message bus architectures for asynchronous RPC, fire-and-forget commands, and pub/sub event fan-out.
- **Alternative Message Brokers**: `RedisMqServer` (in `ServiceStack.Server`), `BackgroundMqService` (in-memory in `ServiceStack`).

---

## Key Functionality

### Primary Types
| Class | Role |
|---|---|
| `RabbitMqServer` | Core message queue service host. Registers message handlers for Request DTOs, creates worker threads (`RabbitMqWorker`), and supervises consumer connections. |
| `RabbitMqMessageFactory` | Implements `IMessageFactory` to create producers and queue clients. |
| `RabbitMqProducer` | Implements `IMessageProducer` to publish messages to RabbitMQ exchanges. |
| `RabbitMqQueueClient` | Implements `IMessageQueueClient` for programmatic queue polling, acknowledging (`Ack`), and rejecting (`Nak`). |
| `SharedQueue<T>` | Thread-safe, blocking in-memory buffer with monotonic timeout tracking. |

### Message Routing Conventions
- Requests: `mq:{RequestDto}.inq`
- Priority Requests: `mq:{RequestDto}.priorityq`
- Responses: `mq:{RequestDto}.outq`
- Dead-Letter Queue: `mq:{RequestDto}.dlq`

---

## Architecture & Design Patterns

### AMQP 0-9-1 Transport Abstraction
Messages sent through `IServiceGateway.SendOneWay` or `IMessageProducer.Publish` are wrapped into ServiceStack `IMessage<T>` envelopes containing metadata (MessageId, ReplyId, CreatedDate, Tag) and serialized into AMQP byte payloads.

### Worker Pool Isolation
Each registered request type can be configured with its own worker pool count, allowing high-priority or slow endpoints to scale independently without blocking other queues.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Always preserve these remediations:

### 1. Monotonic Timeouts Against Clock Drift (`SharedQueue`, `RabbitMqQueueClient`)
- **Risk**: Using `DateTime.Now` or `DateTime.UtcNow` arithmetic for timeouts causes premature timeouts or indefinite blocking when system clocks adjust (NTP syncs, DST).
- **Rule**: Always measure elapsed timeout durations using `Stopwatch.StartNew()` (`sw.Elapsed`).

### 2. Guid Parsing Resilience on AMQP Headers
- **Risk**: Calling `Guid.Parse(props.MessageId)` crashes with `FormatException` when receiving messages from non-.NET or external AMQP producers using alphanumeric or non-Guid message IDs.
- **Rule**: Use `Guid.TryParse`. On parse failure, default `Message.Id` to `Guid.Empty` while preserving raw string identifiers in `Message.Meta`.

### 3. Stream Cleanup in Deserialization
- In `ToMessage<T>`, always wrap `MemoryStreamFactory.GetStream(...)` in `using var ms = ...` to prevent stream leaks when custom content-type deserializers throw exceptions.

### 4. 404 Missing Exchange Recovery
- In `RabbitMqProducer.PublishMessage`, catching 404 exchange-not-found errors to declare missing exchanges and re-publish must include a `return;` statement upon success so the caught exception is not re-thrown to the caller.

### 5. Cross-Platform Worker Shutdown
- Do not call `Thread.Abort()` on background workers in modern .NET (`NETCORE` / .NET 6+); guard with `#if NETFRAMEWORK` to prevent `PlatformNotSupportedException`.

---

## Common Modification Scenarios

1. **Starting the RabbitMQ Message Host**
   ```csharp
   container.Register<IMessageService>(c => new RabbitMqServer("localhost:5672") {
       RetryCount = 2,
   });
   var mqServer = container.Resolve<IMessageService>();
   mqServer.RegisterHandler<MyRequest>(ExecuteMessage);
   mqServer.Start();
   ```

2. **Publishing Messages**
   - Inject `IMessageProducer` or call `Publish<T>(new MyRequest { ... })`.
   - Ensure channel disposal is handled cleanly under exceptions.
