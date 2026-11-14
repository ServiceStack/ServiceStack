# ServiceStack.RabbitMq accepted feature improvements

RABBIT-002 was explicitly accepted by the user on 2026-11-14. Other proposals remain unaccepted.

See [proposals](FEATURE-PROPOSALS.md), [existing capabilities](../FEATURES.md) and [acceptance workflow](../../FEATURES.md#planning-workflow).

For each explicitly accepted proposal, move its full record here with its stable ID and add:

- Decision reference, accepting maintainer and date.
- Agreed customer outcome, scope and exclusions.
- Owning project and participating projects.
- Implementation plan, compatibility/migration decisions and acceptance criteria.
- Status: Accepted, In progress, Implemented or Released; do not equate these states.
- Validation evidence, documentation changes and implementation revision.
- Release version and release-note link when confirmed.

After implementation, update FEATURES.md to describe the actual supported behavior and constraints. Keep this decision/implementation record for history.

## RABBIT-002: RabbitMQ 4.3+ compatible output queue provisioning

- Status: Implemented in the working tree; release availability unconfirmed.
- Decision: User requested “please implement the recommendation” on 2026-11-14 in this chat.
- Agreed scope: Explicit startup provisioning switch, durable shared output queues by default, consistent factory/client/direct-extension configuration, operator-managed migration, regression tests and documentation.
- Type: Broker compatibility fix and topology configuration improvement.
- Initial disposition: Prioritize compatibility investigation; no release commitment.
- Customer evidence: The request supplied in this review reports startup failure on RabbitMQ 4.4.0 / OTP 28 with ServiceStack.RabbitMq 10.4.2 and 6.0.2, including when output publishing is disabled. These version-specific results have not been independently reproduced.
- Potential value: Restores startup against broker defaults for affected applications; broader adoption is unknown.
- Relative effort: Small for a startup-only skip; medium for consistent declaration options, migration guidance and integration coverage. Not a delivery estimate.
- Evidence confidence: High for the source-level cause and documented broker restriction; live compatibility and published-package behavior remain unverified.

### Existing baseline

`RabbitMqServer.Init()` calls `RegisterQueues(QueueNames)` for every registered type. Both `RegisterQueues` overloads declare `.outq` through `RegisterTopic`, which hardcodes `durable: false, exclusive: false, autoDelete: false`. `DisablePublishingToOutq` only sets the publishing whitelist; it does not suppress provisioning. `CreateTopicFilter` can modify arguments, not declaration flags. Public filter documentation illustrates TTL, not durability configuration.

`RabbitMqProducer.PublishMessage` and `GetMessage` also reach `RegisterTopic` through `RegisterQueueByName`, including standalone clients. Existing filters resolve the server through `HostContext`, so a new option must not depend exclusively on global AppHost resolution. Temporary reply queues are already exclusive and should retain their separate lifecycle.

RabbitMQ's queue guide says transient non-exclusive classic queues are denied by default starting in 4.3.0. Its deprecated-feature override is a temporary operational workaround. Adding TTL, precreating a durable queue, changing the .NET runtime or setting an argument named `durable` does not change the hardcoded declaration flags.

### Proposed increment and alternatives

1. Provide an explicit startup output-queue provisioning option. Alternatively make provisioning follow `PublishToOutqWhitelist`, including its empty value set by `DisablePublishingToOutq`, but assess the compatibility impact on externally used queues first. Avoid equating local publishing policy with all external topology requirements.
2. Provide durable, non-exclusive output queue declarations through narrowly scoped options, propagated through the message factory, startup, lazy publish/get and recovery paths, with explicit overloads for direct extension callers. Retain argument filters. Exact API names remain undecided.
3. Recommend durable output queues as the default in the compatibility release, subject to an explicit migration decision. An opt-in option alone leaves ordinary default startup broken on modern brokers; a startup skip alone only fixes applications disabling output publishing. If a maintenance release preserves the old durability default, document that it requires configuration and does not restore default compatibility.

Do not make shared, predictably named output queues exclusive: multiple connections and service instances use them. Do not claim durable notifications: `RabbitMqQueueClient.Notify` publishes without persistent message properties, and the topic exchange is currently non-durable. Assess binding recovery after broker restart separately from queue declaration compatibility. Queue TTL and message TTL/length limits may be useful optional retention controls; do not silently introduce them or change notification delivery guarantees. This proposal does not require quorum queues; coordinate any shared options with RABBIT-001.

### Compatibility and migration

Do not automatically delete or recreate queues on declaration mismatch. RabbitMQ rejects inequivalent declarations with 406; operators must coordinate all declarers. Stop old declarers, drain or deliberately discard output notifications, delete only the affected `.outq` queues and recreate them with matching options across applications. A new queue name or namespace is an alternative requiring coordinated publisher/consumer routing. Preserve request, priority and dead-letter queues. A skip-only rollout need not delete existing output queues, though operators should plan separate cleanup. Precreating durable queues is insufficient while unmodified code still redeclares them as transient.

### Validation before release

Run integration coverage on pinned RabbitMQ 4.4.0 with the deprecated feature denied and an older supported broker. Cover enabled and disabled output publishing, selective whitelist behavior, fresh topology, existing transient/durable queues, multiple connections/instances, AppHost and standalone factories, both extension overloads, lazy publish/get, polling/subscription workers, reconnect and broker restart, reply-to queues, and request/retry/DLQ regressions. Inspect actual queue flags and bindings; verify notification behavior after restart without promising persistence. Existing `Messages_with_null_responses_are_not_published_when_DisablePublishingToOutq` tests absence of a message by polling `.outq`, which itself lazily declares the queue; it does not prove that provisioning was skipped. Validate supported target frameworks and confirm package/release availability before announcing support.

### Evidence and ownership

- Owner: ServiceStack.RabbitMq; no cross-project feature acceptance implied.
- [RabbitMqServer.cs](../RabbitMqServer.cs), [RabbitMqExtensions.cs](../RabbitMqExtensions.cs), [RabbitMqMessageFactory.cs](../RabbitMqMessageFactory.cs), [RabbitMqProducer.cs](../RabbitMqProducer.cs), [RabbitMqQueueClient.cs](../RabbitMqQueueClient.cs).
- [RabbitMqServerTests.cs](../../../tests/ServiceStack.Server.Tests/Messaging/RabbitMqServerTests.cs), [RabbitMqTests.cs](../../../tests/ServiceStack.Common.Tests/Messaging/RabbitMqTests.cs).
- [RabbitMQ queue documentation](https://www.rabbitmq.com/docs/queues), checked 2026-11-14: deprecation, declaration property equivalence, exclusive connection ownership, and durability semantics. The version-specific 4.4 queue documentation was not accessible during this review; no live broker test was run.

### Implementation and validation (2026-11-14)

- `RabbitMqServer.DisableTopicQueues` skips output queue creation during startup independently of publishing policy. `DisablePublishingToOutq` retains its existing publishing-only meaning.
- `DurableTopicQueues` defaults to true on the message factory and is forwarded by the server. Startup and lazy publish/get/recovery paths use that factory setting; direct extensions retain original signatures and add explicit durability/provisioning overloads.
- Declaration caches are per client so one factory or broker does not suppress another client's declarations.
- No automatic deletion, exclusive output queues, TTL defaults, topic-exchange durability change, persistence guarantees or quorum preset introduced. See [upgrade guide](../RABBITMQ-4-UPGRADE.md).
- Tests: 28 targeted .NET 10 unit tests passed (10 new topology cases plus 18 existing RabbitMqUnitTests); four opt-in live startup/processing scenarios passed on each of RabbitMQ 4.4.0 (default configuration) and 3.13.7, covering output enabled/skipped and polling/subscription workers. Tests reside in `RabbitMqTopologyTests.cs` in ServiceStack.Common.Tests.
- Release builds passed for net472, net6.0, net8.0 and net10.0 using existing dependency outputs; existing package-pruning warnings remain.
- Live restart, mixed-version migration, full retry/DLQ regressions and .NET Framework runtime execution remain unverified. Durable output queue declarations do not guarantee notification persistence or binding recovery after restart. Package release availability is not established.
