# ServiceStack.RabbitMq features

RabbitMQ-backed DTO messaging, producers, consumers and queue provisioning.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net472;net6.0;net8.0;net10.0`; see [ServiceStack.RabbitMq.csproj](ServiceStack.RabbitMq.csproj) for conditional compilation and dependencies.
- Queue declaration filters already permit argument customization. Queue durability, acknowledgement and publisher behavior must be examined before promising delivery guarantees. The client dependency is declared in the project file.
- Project dependencies: [ServiceStack.Text](../../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj); [ServiceStack.Interfaces](../ServiceStack.Interfaces/ServiceStack.Interfaces.csproj); [ServiceStack.Client](../ServiceStack.Client/ServiceStack.Client.csproj); [ServiceStack.Common](../ServiceStack.Common/ServiceStack.Common.csproj); [ServiceStack](../ServiceStack/ServiceStack.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| Message host and workers | Register request handlers and process broker messages with per-type workers. | [RabbitMqServer.cs](RabbitMqServer.cs), [RabbitMqWorker.cs](RabbitMqWorker.cs), [RabbitMqBasicConsumer.cs](RabbitMqBasicConsumer.cs) |
| Producers and queue clients | Publish DTO envelopes, receive messages, acknowledge and reject deliveries. | [RabbitMqProducer.cs](RabbitMqProducer.cs), [RabbitMqQueueClient.cs](RabbitMqQueueClient.cs), [RabbitMqMessageFactory.cs](RabbitMqMessageFactory.cs) |
| Queue topology and customization | Declare request, priority, topic and dead-letter queues and customize declaration arguments. | [RabbitMqExtensions.cs](RabbitMqExtensions.cs), [RabbitMqServer.cs](RabbitMqServer.cs) |

## RabbitMQ 4 compatibility (working tree, reviewed 2026-11-14)

- Output/topic queues now default to durable, non-exclusive declarations. `RabbitMqServer.DurableTopicQueues` forwards the message factory setting used by startup and standalone/lazy clients. Direct declaration extensions also accept explicit durability.
- `DisableTopicQueues` skips startup output queue provisioning. Use it with `DisablePublishingToOutq` when notifications are unused; explicit client reads/publishes can still declare output queues. Publishing policy alone continues to leave provisioning enabled.
- Existing transient output queues require an operator-managed migration, or `DurableTopicQueues = false` on a broker permitting the deprecated feature. No queues are automatically deleted. See [RabbitMQ 4 upgrade guide](RABBITMQ-4-UPGRADE.md).
- Validated on .NET 10 against RabbitMQ 4.4.0 defaults and 3.13.7 for startup and processing, polling/subscription and output enabled/skipped. Notification persistence, broker restart recovery and published release availability are not established by these checks.

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [rabbit-mq.md](../../../../docs.servicestack.net/MyApp/_pages/rabbit-mq.md)
- [messaging.md](../../../../docs.servicestack.net/MyApp/_pages/messaging.md)
- [background-mq.md](../../../../docs.servicestack.net/MyApp/_pages/background-mq.md)

## Test evidence and review limits

Related test files located (not run in this documentation pass; not proof that every capability or database dialect is covered):

- [ServiceStack.Common.Tests/Messaging/RabbitMqUnitTests.cs](../../tests/ServiceStack.Common.Tests/Messaging/RabbitMqUnitTests.cs)
- [ServiceStack.Common.Tests/Messaging/RabbitMqTests.cs](../../tests/ServiceStack.Common.Tests/Messaging/RabbitMqTests.cs)
- [ServiceStack.Server.Tests/Messaging/RabbitMqServerTests.cs](../../tests/ServiceStack.Server.Tests/Messaging/RabbitMqServerTests.cs)

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
