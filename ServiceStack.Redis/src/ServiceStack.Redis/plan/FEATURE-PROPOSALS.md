# ServiceStack.Redis feature proposals

Deep review: 2026-11-10. Source baseline `101038b4ba5a65650e02491e98c7c8a4e1d46f5f`; documentation baseline `e2c48458f2278cfc1825f7b945652ff3c4bd7501`. These records describe the inspected checkout, not verified release availability.

Three candidates remain entirely **proposed**; REDIS-004 and the URI/IPv6 phase of REDIS-007 have also been accepted. The cache-correctness and GETEX/GETDEL/UNLINK phases of REDIS-001 and PooledRedisClientManager cancellation and RedisManagerPool bounded-admission phases of REDIS-002 were accepted on 2026-11-10; their remaining scope is still proposed. Priorities reflect engineering fit and a bounded first deliverable; customer demand, performance and commercial return have not been measured. Relative effort is scope, not a schedule. Source, related test assertions and public documentation were inspected; the initial documentation review ran no server tests. Subsequent implementation validation is recorded in FEATURE-IMPROVEMENTS.md; performance/commercial value remains unmeasured.

See [existing features](../FEATURES.md), [accepted improvements](FEATURE-IMPROVEMENTS.md) and the suite's [decision workflow](../../../../ServiceStack/src/FEATURES.md#planning-workflow).

## Review findings

The existing [FUTURE.md](../../../FUTURE.md) is a useful source of ideas and remains intact. It is not an acceptance ledger or a guarantee that its examples exist. This document narrows selected ideas using actual implementation paths. Its blanket “roughly Redis 3.2” characterization is too coarse for capability decisions: ACL support and custom commands already exist, and individual command/option support must be checked.

| Existing behavior verified in source/tests | Proposal boundary |
| --- | --- |
| Native/rich/typed APIs, sync/async I/O, transactions, pipelines, Lua and custom nested commands | Do not propose async support or raw command access as new |
| Pooled manager has cancellable async waits; lightweight pool preserves default overflow and adds optional bounded admission | Async setup/deadline implemented; fairness and broader measurement remain proposed |
| Baseline async conditional TimeSpan expiry and split DateTime writes were reproduced and corrected in the working tree | Cache correctness and selected command group implemented; scan helpers remain proposed |
| RedisLock ownership is checked on release; expiration is encoded in its value | Propose an additive renewable lease, preserving legacy coordination |
| SCAN pattern removal has a 2,500-key regression case | Do not claim pattern removal currently uses KEYS or needs basic pagination |
| ACL Username participates in endpoint identity; cloning preserves TLS settings | Modernize configuration without undoing existing protections |
| DiagnosticListener command/connection/retry events, RedisStats and host profiling exist | Add standalone export/admission metrics, not a duplicate profiling system |
| Sentinel and client-side consistent hashing exist | Redis Cluster is a separate conditional project |

Thread-safe managers do not make a borrowed RedisClient safe for arbitrary concurrent commands. Stateful clients need exclusive use through their lease. RedisMqServer, RedisServerEvents and RedisRequestLogger are integrations owned by ServiceStack.Server.

## Recommended order

| ID | Candidate | Disposition | Relative scope |
| --- | --- | --- | --- |
| [REDIS-001](#redis-001-atomic-cache-expiry-and-focused-modern-command-coverage) | Atomic cache expiry and focused modern command coverage | Cache correctness, command group and pattern cleanup implemented; hash-field expiry implemented | Small–medium for the first command group; broad command modernization is separate. |
| [REDIS-002](#redis-002-cancellable-pool-admission-and-optional-bounded-overflow) | Cancellable pool admission and optional bounded overflow | Pool cancellation, bounded admission and async connection setup implemented | Medium; preserve two different manager contracts. |
| [REDIS-007](#redis-007-explicit-modern-endpoint-parsing-and-per-endpoint-tls-policy) | Explicit modern endpoint parsing and per-endpoint TLS policy | URI/IPv6 phase implemented; TLS policy extensions still proposed | Medium; network setup is broader than parsing alone. |
| [REDIS-006](#redis-006-optional-microsoft-cache-and-di-integration) | Optional Microsoft cache and DI integration | IDistributedCache/DI implemented; HybridCache L2 validated | Small–medium for registration/raw cache; sliding expiration needs careful semantics. |
| [REDIS-005](#redis-005-redis-client-telemetry-and-pool-health-integration) | Redis client telemetry and pool health integration | Next candidate; reuse existing diagnostics | Medium; instrumentation first, optional host health adapter second. |
| [REDIS-003](#redis-003-explicit-renewable-leases-with-independent-wait-and-lease-timeouts) | Explicit renewable leases with independent wait and lease timeouts | Next candidate after API/migration design | Medium; mixed-version lock compatibility must be designed. |
| [REDIS-004](#redis-004-typed-streams-and-consumer-group-operations) | Typed Streams and consumer-group operations | Streams, typed/group APIs, primitive pipelines and bounded append implemented; structured batching/adapters still proposed | Large; split native operations, typed facade and any MQ adapter. |
| [REDIS-008](#redis-008-redis-cluster-routing-as-a-separate-manager-capability) | Redis Cluster routing as a separate manager capability | Conditional strategic work; validate deployment demand first | Large; a new routing/semantics layer, not a small extension of Sentinel. |

The first phases of REDIS-001 and REDIS-002 are implemented in the working tree; see the accepted ledger for their limits and test evidence. Add REDIS-007 when an IPv6/cloud configuration case is available. REDIS-006 is now implemented in the main Redis package for .NET 10, while REDIS-004 is the strongest new data-type investment if durable stream consumers are a demonstrated customer need. Avoid committing all candidates as one release.

## REDIS-001: Atomic cache expiry and focused modern command coverage

The cache-correctness and GETEX/GETDEL/UNLINK phases were accepted on 2026-11-10 and are implemented in the working tree. See the [full record, decision, remaining proposed scope and validation](FEATURE-IMPROVEMENTS.md#redis-001-atomic-cache-expiry-and-focused-modern-command-coverage). The SCAN+UNLINK helpers were subsequently accepted and implemented. Endpoint-wide capability infrastructure and broader compatibility validation remain proposed. These need a separate scope decision.

### Hash-field expiration

Accepted on 2026-11-11 following the maintainer's request to implement the recommended first scope. Implemented
and validated in the working tree; see the [decision, verification and original proposal](FEATURE-IMPROVEMENTS.md#hash-field-expiration-decision-2026-11-11).
Deferred integer-array pipeline/transaction support and Redis 8 HGETEX/HSETEX remain proposed.

The reliability review recommends shared array-result pipeline callbacks as the next bounded capability increment. Start with hash-field expiry's ordered long[] results, then reuse the infrastructure for structured Streams results. Define sync/async result typing, nil-versus-empty semantics, per-reply timeouts and malformed-response cleanup before implementation. Keep transaction support a separate scope decision; current standalone guards remain in force.

## REDIS-002: Cancellable pool admission and optional bounded overflow

PooledRedisClientManager cancellation and RedisManagerPool optional bounded admission were accepted on 2026-11-10 and are implemented in the working tree. See the [full record, decision, remaining proposed scope and validation](FEATURE-IMPROVEMENTS.md#redis-002-cancellable-pool-admission-and-optional-bounded-overflow). Fully async connection setup and its shared setup deadline were subsequently accepted and implemented; see the [decision](FEATURE-IMPROVEMENTS.md#fully-async-connection-decision-2026-11-10). Fairness/performance benchmarking and telemetry exports remain proposed.

## REDIS-007: Explicit modern endpoint parsing and per-endpoint TLS policy

Accepted in bounded scope on 2026-11-10 and implemented in the working tree under the maintainer’s request to implement all three next recommendations. See the [canonical decision, implementation status and original record](FEATURE-IMPROVEMENTS.md#redis-007-explicit-modern-endpoint-parsing-and-per-endpoint-tls-policy).

## REDIS-006: Optional Microsoft cache and DI integration

Accepted on 2026-11-10 following the request to implement IDistributedCache. Implemented in ServiceStack.Redis for .NET 10 only, following the maintainer’s packaging revision; see [decision, scope, evidence and original proposal](FEATURE-IMPROVEMENTS.md#redis-006-optional-microsoft-cache-and-di-integration). Release availability remains unconfirmed.

## REDIS-005: Redis client telemetry and pool health integration

- Recommendation: Next candidate; reuse existing diagnostics.
- Potential value: Medium–high for diagnosing saturation versus server/network latency.
- Relative effort: Medium; instrumentation first, optional host health adapter second.
- Evidence confidence: High existing DiagnosticListener events/stats are visible; no missing-telemetry claim for the wider ServiceStack host.
- Customer validation: not yet established; select a real application/workaround before acceptance.

### Existing behavior and gap

RedisDiagnostics already emits command/connection before/after/error/retry events initialized from Activity.Current. RedisStats and manager GetStats expose counters, and core ServiceStack Profiling correlates Redis events. No Redis-owned ActivitySource/Meter was found in this client. Thus the increment is standalone client export and live pool admission metrics, not the first Redis diagnostic facility.

### Proposed outcome and bounded approach

Optional, low-overhead ActivitySource/Meter instrumentation and a separate host health adapter that distinguishes pool exhaustion, connection failures and server role/readiness.

1. Instrument the native send/receive boundary and pool acquisition with a documented logical-operation versus attempt model. Treat pipelines/transactions as explicit batching cases, not an accidental span per parser callback.
2. Record bounded command categories, durations, retries, connection churn, live leases and pool wait/rejection counts. Keep values, raw keys, AUTH arguments, connection strings and user identity out of default export.
3. Emit no expensive diagnostic work without listeners; retain existing DiagnosticListener and stats APIs. Prevent duplicate spans when another Redis instrumentation integration is already enabled.
4. Put ASP.NET health-check dependencies in an optional integration package. PING is liveness, not proof that writes are accepted or application data is durable; distinguish read/write role checks and bounded admission health.

### Acceptance criteria if selected

- Listener fixtures assert one terminal outcome per operation, correct retry attempts, cancellation handling and parent context with no secret-bearing tags.
- Disabled instrumentation has measured negligible overhead; pipeline and failure paths are included.
- Pool saturation and unreachable server produce distinct metrics/health outcomes without the probe waiting indefinitely for an exhausted pool.
- Run existing DiagnosticTests and stats regression cases; document metric cardinality and exporter ownership.

### Alternatives and compatibility

Use existing ProfilingFeature/RedisStats or an application DiagnosticListener subscriber. Prefer a recipe/bridge if these satisfy standalone consumers. Do not require a collector, OpenTelemetry SDK or ASP.NET dependency in the base package.

Pinned semantic conventions may evolve; document instrument names/units and avoid raw command contents even if local diagnostics permit them. Metrics do not replace existing stats APIs, and a health probe must not amplify an outage or issue destructive commands.

### Evidence

- [RedisDiagnostics.cs](../RedisDiagnostics.cs)
- [RedisStats.cs](../RedisStats.cs)
- [PooledRedisClientManager.cs](../PooledRedisClientManager.cs)
- [RedisManagerPool.cs](../RedisManagerPool.cs)
- [RedisNativeClient_Utils.cs](../RedisNativeClient_Utils.cs)
- [RedisNativeClient_Utils.Async.cs](../RedisNativeClient_Utils.Async.cs)
- [DiagnosticTests.cs](../../../tests/ServiceStack.Redis.Tests/DiagnosticTests.cs)
- [RedisReviewRegressionTests.cs](../../../tests/ServiceStack.Redis.Tests/RedisReviewRegressionTests.cs)
- [redis/profiling.md](../../../../../docs.servicestack.net/MyApp/_pages/redis/profiling.md)
- [redis/stats.md](../../../../../docs.servicestack.net/MyApp/_pages/redis/stats.md)

## REDIS-003: Explicit renewable leases with independent wait and lease timeouts

- Recommendation: Next candidate after API/migration design.
- Potential value: High for callers whose critical work has a known bounded duration.
- Relative effort: Medium; mixed-version lock compatibility must be designed.
- Evidence confidence: High that current ownership checks exist and lease renewal is a distinct increment.
- Customer validation: not yet established; select a real application/workaround before acceptance.

### Existing behavior and gap

RedisLock already checks the stored acquisition value before deleting, using WATCH/transaction; do not propose that safeguard as absent. Its acquisition value is an encoded expiration timestamp and its timeout participates in both waiting and expiry calculation. Successful initial SETNX does not assign a Redis TTL. Recovery is interpreted by later contenders, rather than relying on key expiry. Async acquisition supports cancellation. Separate DistributedLock helpers also exist. Neither inspected lock class supplies a public renewable lease handle with an explicit loss signal.

### Proposed outcome and bounded approach

An additive lease API with distinct acquisition timeout and lease duration, random owner tokens, server TTL, owner-checked renewal/release and explicit loss state. Retain the current AcquireLock contract until migration semantics are agreed.

1. Acquire with conditional SET plus TTL and a unique random ownership value. Use atomic compare-and-renew/release operations on the same key, with script-cache recovery only where replay is known safe.
2. Expose manual extension first; optional heartbeat renewal must stop on cancellation/disposal and report a lost lease without hiding uncertainty.
3. Use a new key namespace/value-format boundary for mixed old/new users. Old code parsing timestamp strings cannot safely coordinate with opaque random tokens on the same key.
4. Keep application fencing a separate documented requirement when a downstream database/resource must reject stale owners. An ownership token in Redis alone does not fence external writes.

### Acceptance criteria if selected

- Expire holder A, acquire B, then dispose/renew A: B's lock remains untouched in sync and async paths.
- Crash a holder and show actual server TTL expiry; test simultaneous acquisition, renewal races, cancellation and unknown outcomes.
- Demonstrate what happens on replication failover, long process pauses and network partitions; describe the chosen failure model rather than promising universal exclusion.
- Migration examples cannot mix timestamp and token protocols on the same key. Existing lock tests continue to describe the legacy API.

### Alternatives and compatibility

Keep current locks for compatible existing workloads, or use a dedicated coordination service when strong exclusion across partitions is required. A recipe using existing SET NX PX and Lua may establish demand before committing a new public type.

A TTL can expire while application code keeps running. A loss token is advisory unless callers cooperate; it cannot undo side effects. Do not label this Redlock, linearizable coordination or exactly-once execution without implementing and validating the relevant algorithm/assumptions.

### Evidence

- [RedisLock.cs](../RedisLock.cs)
- [RedisLock.Async.cs](../RedisLock.Async.cs)
- [Support/Locking/DistributedLock.cs](../Support/Locking/DistributedLock.cs)
- [Support/Locking/DistributedLock.Async.cs](../Support/Locking/DistributedLock.Async.cs)
- [RedisClient.cs](../RedisClient.cs)
- [RedisClientTests.Async.cs](../../../tests/ServiceStack.Redis.Tests/RedisClientTests.Async.cs)
- [redis/distributed-locking.md](../../../../../docs.servicestack.net/MyApp/_pages/redis/distributed-locking.md)

[Redis SET documentation](https://redis.io/docs/latest/commands/set/) documents conditional writes and key expiry options. They are building blocks, not proof that an application's distributed critical section is safe across every failure. Sources checked 2026-11-10.

## REDIS-004: Typed Streams and consumer-group operations

Primitive pipeline support and bounded bulk append were subsequently accepted and implemented; see the [batching decision](FEATURE-IMPROVEMENTS.md#streams-batching-decision-2026-11-10). Structured result pipelines, blocking batching, Streams transactions and adapters remain proposed.

Accepted in bounded scope on 2026-11-10 and implemented in the working tree under the maintainer’s request to implement all three next recommendations. See the [canonical decision, implementation status and original record](FEATURE-IMPROVEMENTS.md#redis-004-typed-streams-and-consumer-group-operations).

## REDIS-008: Redis Cluster routing as a separate manager capability

- Recommendation: Conditional strategic work; validate deployment demand first.
- Potential value: Potentially high for cluster-enabled managed Redis deployments.
- Relative effort: Large; a new routing/semantics layer, not a small extension of Sentinel.
- Evidence confidence: High architectural gap in inspected code; market demand and supported command subset undecided.
- Customer validation: not yet established; select a real application/workaround before acceptance.

### Existing behavior and gap

Sentinel/resolvers support primary/replica discovery and failover. ShardedRedisClientManager uses client-side consistent hashing, which is not Redis Cluster hash-slot routing. The inspected command paths/managers did not contain MOVED/ASK handling or cluster topology routing. Existing multi-key, typed-entity, transaction and Pub/Sub behavior assumes different placement/connection rules.

### Proposed outcome and bounded approach

A separate opt-in cluster manager with an explicit supported command matrix and topology-aware execution. Start with single-key operations; do not promise drop-in support for every IRedisClient operation.

1. Prototype topology discovery, CRC16/hash tags, MOVED/ASK, bounded redirect loops and topology refresh using RESP2. RESP3 is not a prerequisite.
2. Make key extraction and routing metadata explicit. Same-slot operations may be supported; cross-slot atomic operations should fail clearly. Any scatter/gather helper has separate result-order/partial-failure semantics.
3. Design dedicated connection ownership for WATCH/MULTI, scripts, blocking commands and Pub/Sub. Map Sentinel, replica reads and cluster-managed failover as separate capabilities.
4. Typed ids:{Type}, URNs and related-object keys need an explicit migration/placement policy. Never change existing key hashes or add hash tags automatically to a live store.

### Acceptance criteria if selected

- A pinned multi-node fixture covers topology bootstrap, resharding, failover, stale slots, ASK/MOVED and redirect exhaustion.
- Same-slot transactions/scripts work only within their documented supported scope; cross-slot failures cannot be hidden as partial success.
- Authentication/TLS, endpoint remapping and cluster DB restrictions are validated. Unknown-write outcomes do not trigger unsafe blanket retries.
- Publish which typed/client/queue operations remain unsupported before advertising cluster support; compare real customer topology requirements.

### Alternatives and compatibility

Keep standalone/Sentinel deployments, use a proxy with understood semantics, or a dedicated cluster client. Prototype against a named deployment need before investing; do not market existing client-side sharding as cluster compatibility.

Distributed topology behavior affects almost every convenience API. Automatic cross-node MGET/DEL splitting is not equivalent to a same-node atomic operation. Cluster support does not make existing RedisServerEvents or Redis MQ cluster-ready without a separately owned Server review.

### Evidence

- [ShardedRedisClientManager.cs](../ShardedRedisClientManager.cs)
- [RedisSentinel.cs](../RedisSentinel.cs)
- [RedisSentinelResolver.cs](../RedisSentinelResolver.cs)
- [RedisResolver.cs](../RedisResolver.cs)
- [RedisNativeClient_Utils.cs](../RedisNativeClient_Utils.cs)
- [CustomCommandTests.cs](../../../tests/ServiceStack.Redis.Tests/CustomCommandTests.cs)
- [RedisSentinelFailOverTests.cs](../../../tests/ServiceStack.Redis.Tests.Sentinel/RedisSentinelFailOverTests.cs)
- [redis/sentinel.md](../../../../../docs.servicestack.net/MyApp/_pages/redis/sentinel.md)

The [Redis Cluster specification](https://redis.io/docs/latest/operate/oss_and_stack/reference/cluster-spec/) describes hash-slot routing and redirection. Validate against a pinned implementation/version and selected command subset; client-side consistent hashing is a distinct mechanism. Sources checked 2026-11-10.

## Deferred ideas from FUTURE.md

| Idea family | Reason to defer / evidence needed |
| --- | --- |
| RESP3 and server-assisted client caching | Needs full reply/push demultiplexing, reconnect invalidation and memory bounds; not required for basic Streams or Cluster routing |
| Multiplexed transport | First measure REDIS-002; WATCH, transactions, Pub/Sub and blocking operations need exclusive connection semantics |
| Search/JSON/vector features | Validate the exact server product/module and use case; custom commands already provide access, and Redis/Valkey feature availability is not interchangeable |
| Functions and broad modern collection/hash commands | Select actual workflows and individual version/ACL requirements instead of importing every latest command |
| Typed secondary indexes, expiry pruning, optimistic versions, alternate serializers/AOT | Valuable separate persistence designs, but require explicit key-layout migration, concurrent update and serialization compatibility work |
| Rate limiters, semaphores, leader election, idempotency | Start with a named application workflow; coordinate existing framework rate limiting/jobs and the Core request-idempotency proposal; do not add generic coordination claims |
| Parser allocation rewrite / consistent-hash replacement | Benchmark before prioritizing; changing consistent hashing can move existing keys and is not a transparent optimization |
| Testcontainers and version matrix | Enabling infrastructure for selected features, not a standalone customer capability; tests must use isolated databases/keys and pinned images |

## Ownership and decision handling

- This package owns Redis command/protocol/client/manager behavior. Changes to shared interfaces belong in ServiceStack.Interfaces with sync/async and third-party implementation compatibility considered.
- Any Streams MQ or cluster-aware Server Events adapter needs its own ServiceStack.Server proposal and delivery/topology review. Existing Redis MQ and Pub/Sub semantics remain distinct.
- Optional host integrations should avoid forcing ASP.NET/OpenTelemetry dependencies into this base package.
- Accept by stable REDIS ID and agreed scope, then move the complete record into FEATURE-IMPROVEMENTS.md with the accepting maintainer, date, decision, implementation plan and validation criteria. Leave a link here; implementation and confirmed release are later states.
- FUTURE.md remains idea history. Its illustrative APIs, effort estimates and broad server-version claims are not adopted as approved designs by this review.
