# ServiceStack.RabbitMq feature proposals

Status: proposed only; no entry here authorizes implementation. Reviewed 2026-10-31.

See [existing capabilities](../FEATURES.md), [suite priorities](../../plan/FEATURE-PROPOSALS.md), and [acceptance workflow](../../FEATURES.md#planning-workflow).

## RABBIT-001: Documented quorum queue configuration with topology validation

- Status: Proposed; acceptance not requested or granted.
- Type: Improvement to existing queue customization.
- Initial disposition: Investigate next.
- Potential value: High for affected deployments; an explicit customer request exists, but broader adoption is unknown.
- Relative effort: Small–medium for a recipe; medium for presets and broker integration tests; not a delivery estimate.
- Evidence confidence: High that declaration hooks exist; medium that a preset is needed; live compatibility not established.
- Customer evidence: One public request; see sources below. No adoption/revenue estimate.

### Existing baseline

RabbitMqServer.CreateQueueFilter and CreateTopicFilter already modify declaration arguments. RabbitMqExtensions separates durable request/DLQ queues from non-durable topic queues. The user request must not be treated as proof that quorum is impossible.

### Proposed increment

First validate a supported recipe using existing filters. If that is sufficient, ship documentation and integration coverage rather than a new API. Otherwise add explicit queue-role configuration/presets for durable request, priority and dead-letter queues while keeping temporary/topic semantics explicit. Document migration from already-declared queues.

### Validation before acceptance

Run against a pinned RabbitMQ version matrix, including broker restart, nack/retry, dead-letter routing, declaration mismatch and temporary reply queues. Verify publisher acknowledgement behavior before making durability claims.

### Constraints and risks

Queue type migration and broker-specific capabilities require explicit planning; do not silently redeclare existing queues with incompatible attributes or apply quorum arguments to every queue role.

### Evidence and ownership

- [ServiceStack.RabbitMq/RabbitMqServer.cs](../RabbitMqServer.cs)
- [ServiceStack.RabbitMq/RabbitMqExtensions.cs](../RabbitMqExtensions.cs)
- [ServiceStack.Common.Tests/Messaging/RabbitMqTests.cs](../../../tests/ServiceStack.Common.Tests/Messaging/RabbitMqTests.cs)
- [Customer request #149](https://github.com/ServiceStack/Discuss/discussions/149) requests quorum-queue migration; it does not establish current implementation limitations.
- [RabbitMQ quorum queue documentation](https://www.rabbitmq.com/docs/quorum-queues) describes replicated queues and the removal of classic mirroring in RabbitMQ 4.0. Pin broker versions when validating compatibility. Sources checked 2026-10-31.

## RABBIT-002: RabbitMQ 4.3+ compatible output queue provisioning

Accepted for implementation by the user on 2026-11-14; see the [canonical decision and implementation record](FEATURE-IMPROVEMENTS.md#rabbit-002-rabbitmq-43-compatible-output-queue-provisioning).

## Decision handling

When explicitly accepted, move the full proposal under the same stable ID into [FEATURE-IMPROVEMENTS.md](FEATURE-IMPROVEMENTS.md), record the decision/date and agreed scope, and leave a short link here. Rejected or deferred proposals remain here with their disposition. Prioritization is not acceptance.
