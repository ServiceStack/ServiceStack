# RabbitMQ 4 output queue migration

These changes are implemented in the working tree; a released package version has not been confirmed.

RabbitMQ denies transient, non-exclusive classic queue declarations by default starting in 4.3.0. ServiceStack now declares `.outq` queues as **durable, non-exclusive** by default. Request, priority, dead-letter and exclusive temporary reply queue settings are unchanged.

## Applications without output notifications

Configure before registering handlers and starting the server:

```csharp
var mqServer = new RabbitMqServer(connectionString) {
    DisablePublishingToOutq = true,
    DisableTopicQueues = true,
};
mqServer.RegisterHandler<MyRequest>(HandleMessage);
mqServer.Start();
```

`DisableTopicQueues` skips `.outq` declarations during startup. It does not delete existing queues or disable explicit client operations. Polling or publishing to `.outq` can still create it. `DisablePublishingToOutq` retains its publishing-only behavior, allowing applications to provision queues used by external publishers.

## Applications using output notifications

Fresh installations use durable output queues automatically. For standalone clients the same default applies:

```csharp
var factory = new RabbitMqMessageFactory(connectionString) {
    DurableTopicQueues = true,
};
```

`RabbitMqServer.DurableTopicQueues` forwards this setting. Configure it before startup or creating clients. For direct `IModel` extensions, `RegisterTopic(queueName, durable: true)`, `RegisterQueueByName(queueName, durableTopics: true)` and both `RegisterQueues` overload families support explicit configuration. `RegisterQueues` overloads accepting `durableTopics` also accept `declareTopics: false`.

Argument customization through `CreateTopicFilter` is retained; it cannot override the durability flag. Existing filter resolution still requires an AppHost registration. The new durability option works without an AppHost.

## Existing transient output queues

RabbitMQ cannot change queue durability through redeclaration. Mismatched declarations fail; ServiceStack does not automatically delete or recreate queues.

1. Identify the affected `mq:*.outq` queues and all applications that declare them, including producers, polling clients and external consumers.
2. Stop those declarers and consumers. Drain queued notifications, or explicitly decide that they can be discarded.
3. Delete only the affected output queues. Do not use helpers that delete all queues for a message type: those also delete request and dead-letter queues.
4. Deploy consistent durability settings to all declarers and restart them to recreate queues and bindings. Old clients declaring transient queues cannot share the new durable queue declarations.

An alternative is a coordinated migration to a new queue namespace with updated publisher/consumer routing. Skipping startup output provisioning does not require deleting existing queues; arrange separate cleanup if they are unused.

For a temporary legacy rollout, configure `DurableTopicQueues = false` on the server and each standalone factory, and permit the deprecated feature on brokers requiring it:

```ini
deprecated_features.permit.transient_nonexcl_queues = true
```

This restores the old queue flags and is a temporary bridge. Precreating a durable queue while running transient declarers is insufficient. TTL arguments do not change durability.

## Delivery semantics and validation

Durable queue metadata does not imply persistent notifications. `Notify` continues to publish transient messages, and the topic exchange remains non-durable. Do not infer persistent notification delivery or binding recovery after a broker restart from this change. No automatic retention policy is added.

Topology unit tests cover defaults, skipping, factory isolation, lazy publishing/polling and temporary reply queues. To run opt-in integration tests, set `SERVICESTACK_RABBITMQ_TEST_HOST` to a **disposable broker**, then run ServiceStack.Common.Tests with `--filter FullyQualifiedName~RabbitMqTopologyTests`. These tests delete the test message type's queues. Startup and processing checks passed on RabbitMQ 4.4.0 with defaults and RabbitMQ 3.13.7, in polling and subscription modes, with and without output queues.

See [RabbitMQ queue documentation](https://www.rabbitmq.com/docs/queues) for deprecated queue combinations, property equivalence and queue/message durability.
