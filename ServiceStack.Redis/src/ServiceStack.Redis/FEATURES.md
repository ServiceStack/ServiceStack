# ServiceStack.Redis features

Source-oriented inventory reviewed 2026-11-10 at `101038b4ba5a65650e02491e98c7c8a4e1d46f5f`. Entry points and related tests were inspected. The expiry/admission and named key commands below have targeted live validation in the working tree; exhaustive coverage and published-version availability were not verified.

## Package and ownership

The package targets `net472;net6.0;net8.0;net10.0`; see [project file](ServiceStack.Redis.csproj). It references ServiceStack.Interfaces, Common and Text. Its net10.0 target additionally references Microsoft caching and DI abstractions for IDistributedCache integration; older targets keep their existing dependencies. It implements pooled/exclusive Redis clients, not the Server package's Redis MQ, distributed SSE or request logger. Thread-safe managers should lease clients for exclusive use; do not share a borrowed client across arbitrary concurrent operations.

## Existing capabilities

| Capability | Customer outcome | Implementation |
| --- | --- | --- |
| Native and rich commands | Work with strings, collections, GEO, HyperLogLog, administration and binary values | [RedisNativeClient](RedisNativeClient.cs), [async native APIs](RedisNativeClient.Async.cs), [RedisClient](RedisClient.cs), [Commands](Commands.cs) |
| Pattern cleanup with background reclamation | Opt-in SCAN + UNLINK with bounded batches, cancellation and removal counts | [Implementation](RedisNativeClient.Patterns.cs), [tests](../../tests/ServiceStack.Redis.Tests/RedisPatternCleanupTests.cs); nine live cases passed on Redis 8.10.1, release unconfirmed |
| Atomic key commands | Read-and-refresh or read-and-delete string values; remove keys with background memory reclamation | [GETEX/GETDEL/UNLINK](RedisNativeClient.KeyCommands.cs), [native/rich extensions](RedisClientExtensions.KeyCommands.cs); optional command contracts preserve existing provider interfaces |
| Hash-field expiration | Expire, inspect and persist individual hash fields with binary-safe ordered results | [Native](RedisNativeClient.HashExpiry.cs), [extensions](RedisClientExtensions.HashExpiry.cs), [REDIS-001 scope](plan/FEATURE-IMPROVEMENTS.md#hash-field-expiration-decision-2026-11-11); requires Redis 7.4+ / compatible Valkey 9+, standalone only, release unconfirmed |
| Typed POCO persistence | Store typed entities with IDs and index-set conventions | [Typed implementation](Generic), [rich persistence helpers](RedisClient.cs) |
| Microsoft distributed cache integration (.NET 10 only) | Built-in IDistributedCache/DI adapter with binary data, atomic absolute/sliding expiry and HybridCache L2 composition | [Adapter](RedisDistributedCache.cs), [registration](RedisDistributedCacheServiceCollectionExtensions.cs), [canonical REDIS-006](plan/FEATURE-IMPROVEMENTS.md#redis-006-optional-microsoft-cache-and-di-integration); 34 cases pass on Redis 8.10/6.2 and Valkey 8.1, release unconfirmed |
| Cache contracts | Use ServiceStack ICacheClient/async wrappers, conditional relative TTL, atomic absolute expiry and pattern removal | [Cache implementation](RedisClient.ICacheClient.cs), [async cache](RedisClient.Async.cs), [atomic absolute expiry](RedisClient.CacheExpiry.cs), [manager cache](RedisClientManagerCacheClient.cs) |
| Connection management | Lease writer/replica clients with different pool policies; optionally cap RedisManagerPool admission and waiting | [Pooled manager](PooledRedisClientManager.cs), [async pool](PooledRedisClientManager.Async.cs), [lightweight pool](RedisManagerPool.cs), [basic manager](BasicRedisClientManager.cs) |
| Sentinel and client-side sharding | Discover primary/replica failover or use explicit consistent-hash distribution | [Sentinel](RedisSentinel.cs), [Sentinel resolver](RedisSentinelResolver.cs), [sharded manager](ShardedRedisClientManager.cs) |
| Async I/O, retries and cancellation | Execute async commands with tokens and configured timeouts/reconnection | [Async transport](RedisNativeClient_Utils.Async.cs), [transport](RedisNativeClient_Utils.cs); async DNS/TCP/TLS/initialization and default role verification share a setup deadline; legacy resolver/callback limits are recorded in [REDIS-002](plan/FEATURE-IMPROVEMENTS.md#redis-002-cancellable-pool-admission-and-optional-bounded-overflow) |
| Transactions, pipelines and Lua | Batch commands or execute server-side logic | [Transactions](Transaction), [Pipelines](Pipeline), [scripts](RedisScripts.cs) |
| Custom commands | Invoke server commands outside named APIs and parse nested results | [Raw commands](RedisNativeClient.cs), [Custom](RedisClient.cs), [complex replies](RedisNativeClient_Utils.cs) |
| Streams and consumer groups | Append/replay binary or typed JSON events; explicitly acknowledge, inspect and reclaim pending delivery | [Native](RedisNativeClient.Streams.cs), [extensions](RedisClientExtensions.Streams.cs), [typed JSON](RedisClientExtensions.TypedStreams.cs), [17 tests](../../tests/ServiceStack.Redis.Tests/RedisStreamsTests.cs); standalone structured APIs, release unconfirmed |
| Pub/Sub | Subscribe and operate reconnecting background listeners | [Subscription](RedisSubscription.cs), [Pub/Sub server](RedisPubSubServer.cs) |
| Distributed lock helpers | Coordinate using current timestamp/ownership protocols | [RedisLock](RedisLock.cs), [async lock](RedisLock.Async.cs), [other helpers](Support/Locking); no universal failover/exclusion guarantee is established here |
| Modern URI and IPv6 endpoints | Opt into ACL URI semantics/rediss and connect to IPv4/IPv6/DNS hosts | [URI parser](RedisEndpoint.Uri.cs), [sockets](RedisNativeClient_Utils.cs), [16 network/parser cases](../../tests/ServiceStack.Redis.Tests/RedisEndpointModernTests.cs); release unconfirmed |
| Authentication and TLS | Configure passwords/ACL usernames and encrypted connections | [Endpoint](RedisEndpoint.cs), [parser](RedisExtensions.cs), [connect/TLS](RedisNativeClient_Utils.cs) |
| Diagnostics and statistics | Consume command/connection/retry events and pool counters | [Diagnostics](RedisDiagnostics.cs), [stats](RedisStats.cs) |

## Documentation and test evidence

- [redis/client.md](../../../../docs.servicestack.net/MyApp/_pages/redis/client.md)
- [redis/async.md](../../../../docs.servicestack.net/MyApp/_pages/redis/async.md)
- [redis/client-managers.md](../../../../docs.servicestack.net/MyApp/_pages/redis/client-managers.md)
- [redis/typed-client.md](../../../../docs.servicestack.net/MyApp/_pages/redis/typed-client.md)
- [redis/transactions.md](../../../../docs.servicestack.net/MyApp/_pages/redis/transactions.md)
- [redis/custom-commands.md](../../../../docs.servicestack.net/MyApp/_pages/redis/custom-commands.md)
- [redis/lua.md](../../../../docs.servicestack.net/MyApp/_pages/redis/lua.md)
- [redis/pubsub.md](../../../../docs.servicestack.net/MyApp/_pages/redis/pubsub.md)
- [redis/sentinel.md](../../../../docs.servicestack.net/MyApp/_pages/redis/sentinel.md)
- [redis/distributed-locking.md](../../../../docs.servicestack.net/MyApp/_pages/redis/distributed-locking.md)
- [redis/profiling.md](../../../../docs.servicestack.net/MyApp/_pages/redis/profiling.md)
- [redis/automatic-retries.md](../../../../docs.servicestack.net/MyApp/_pages/redis/automatic-retries.md)
- [RedisReviewRegressionTests.cs](../../tests/ServiceStack.Redis.Tests/RedisReviewRegressionTests.cs)
- [ConfigTests.cs](../../tests/ServiceStack.Redis.Tests/ConfigTests.cs)
- [PooledRedisClientManagerTests.cs](../../tests/ServiceStack.Redis.Tests/PooledRedisClientManagerTests.cs)
- [RedisManagerPoolTests.cs](../../tests/ServiceStack.Redis.Tests/RedisManagerPoolTests.cs)
- [RedisClientTests.Async.cs](../../tests/ServiceStack.Redis.Tests/RedisClientTests.Async.cs)
- [CustomCommandTests.cs](../../tests/ServiceStack.Redis.Tests/CustomCommandTests.cs)

Most references identify relevant assertions rather than executed coverage. On 2026-11-10, 92 targeted net10.0 cache/pool/review cases passed on isolated Redis 8.10.1 and 16 applicable expiry cases passed on Redis 6.0.20; all four target frameworks build. See [accepted scope and verification limits](plan/FEATURE-IMPROVEMENTS.md). Keep existing regression protections for cloned TLS/ACL settings, pool accounting, async receive timeouts, control-message filtering and pattern-removal batches.

## Hash-field expiry validation

Implemented 2026-11-11 under [REDIS-001](plan/FEATURE-IMPROVEMENTS.md#hash-field-expiration-decision-2026-11-11). The [behavior/compatibility](../../tests/ServiceStack.Redis.Tests/RedisHashExpiryTests.cs) and [protocol](../../tests/ServiceStack.Redis.Tests/RedisHashExpiryProtocolTests.cs) cases validate Redis 7.4.12 / 8.10.1 and Valkey 9.1.3; Redis 6.2.24 / Valkey 8.1.11 validate unsupported-command propagation without mutation/emulation. Relative/absolute seconds/milliseconds, TTL/time/persist, per-field conditions/results, binary data and parent TTL isolation are supported across client targets. Calls are standalone; pipeline/transaction callbacks and HGETEX/HSETEX remain proposed. All target frameworks build; validation/release limits are in the owner record.

## Named key-command validation

The GETEX/GETDEL/UNLINK increment passed 36 new cases on Redis 6.2.24 and a combined 198-case run on Redis 8.10.1. Two older-server cases passed on Redis 6.0.20. Coverage includes binary/empty/missing values, expiry options, scripts-disabled ACLs, cancellation before dispatch, and rich commands in pipelines/transactions; see [tests](../../tests/ServiceStack.Redis.Tests/RedisKeyCommandsTests.cs) and [decision](plan/FEATURE-IMPROVEMENTS.md#command-group-decision-2026-11-10). That earlier command-group run did not establish GETEX/GETDEL behavior on Valkey; later cleanup/Streams validation is recorded below. No released package version is confirmed.

## Bounded manager admission validation

RedisManagerPool's optional bounded admission is implemented in [RedisManagerPool.Admission.cs](RedisManagerPool.Admission.cs), with configuration in [RedisManagerPool.cs](RedisManagerPool.cs). The [25 new cases](../../tests/ServiceStack.Redis.Tests/RedisManagerPoolAdmissionTests.cs) passed in a 223-case combined run on isolated Redis 8.10.1/net10.0, including 48 mixed sync/async borrowers and sampled connection counts. The manager builds for all four target frameworks. These are correctness/saturation checks, not performance benchmarks or a topology-discovery socket cap; see [scope and limits](plan/FEATURE-IMPROVEMENTS.md#bounded-admission-decision-2026-11-10).

## Cleanup, endpoint and Streams verification

The final targeted run passed 308 cases on isolated Redis 8.10.1/net10.0 with five skips: three endpoint fixture cases were exercised separately in an IPv6-enabled TLS container (all 16 endpoint tests passed), and two pre-existing configuration cases were skipped. Cleanup plus Streams passed all 26 cases on Redis 6.2.24 and Valkey 8.1.11. Endpoint and Streams code builds for all Redis targets; contract builds include netstandard2.0. These are source/working-tree capabilities, not confirmed release availability; full scope and limits are in [accepted improvements](plan/FEATURE-IMPROVEMENTS.md).

## Streams batching

[Primitive pipeline routing](RedisNativeClient.Streams.cs) and [binary/typed bulk helpers](RedisClientExtensions.StreamBatches.cs) are implemented in the working tree. AddStreamEntries/Async bounds commands per flush (default 100) and returns generated IDs in input order; it does not provide atomic batches, partial results on errors or automatic replay. All [24 new cases](../../tests/ServiceStack.Redis.Tests/RedisStreamBatchTests.cs) pass with the dedicated TLS fixture; broader regressions, server versions, supported command scope and limits are in the [REDIS-004 decision](plan/FEATURE-IMPROVEMENTS.md#streams-batching-decision-2026-11-10). Release availability remains unconfirmed.

## .NET 10 Microsoft cache integration

The [adapter](RedisDistributedCache.cs), [options](RedisDistributedCacheOptions.cs) and [DI registration](RedisDistributedCacheServiceCollectionExtensions.cs) are guarded by `NET10_0_OR_GREATER`. Matching net10.0-only package references live in [the project file](ServiceStack.Redis.csproj); net472/net6.0/net8.0 have neither these APIs nor the new Microsoft caching/DI references. No separate Redis.Extensions package is produced. [All 34 integration cases](../../tests/ServiceStack.Redis.Tests/RedisDistributedCacheTests.cs) run from the existing Redis test project, with its Microsoft/HybridCache test dependencies also limited to net10.0. Build, assembly metadata and NuGet inspection verify this target boundary; behavior and historical server evidence remain in [REDIS-006](plan/FEATURE-IMPROVEMENTS.md#redis-006-optional-microsoft-cache-and-di-integration). Release availability remains unconfirmed.

## Async connection setup validation

[Connection setup](RedisNativeClient.Connection.Async.cs), [optional resolver contract](IRedisResolverAsync.cs) and [default async resolution](RedisResolver.Async.cs) are implemented in the working tree. All [38 new cases](../../tests/ServiceStack.Redis.Tests/RedisAsyncConnectionTests.cs) pass on an isolated IPv6/TLS Redis 8.10.1 fixture; the combined host regression run passes 343 cases, with fixture/legacy skips recorded in the [decision](plan/FEATURE-IMPROVEMENTS.md#fully-async-connection-decision-2026-11-10). Connection/pool suites pass 74 cases each on Redis 6.2.24 and Valkey 8.1.11, and 34 cache-adapter cases pass after the change. All Redis target frameworks build; other target runtimes were not exercised. Release availability remains unconfirmed.

## Reliability review validation

The [2026-11-11 reliability pass](plan/FEATURE-IMPROVEMENTS.md#reliability-review-decision-2026-11-11) fixes failover reservation ownership/stale DB resets, synchronous reconnects from async EOF readers, legacy resolver override dispatch, pipeline I/O deadlines and empty queued key results. All [48 deterministic regression cases](../../tests/ServiceStack.Redis.Tests/RedisReliabilityTests.cs) pass on .NET 10/6/8 and Mono 6.12 with the net472 build. The broader net10.0 run passes 507 cases; the dedicated IPv6/TLS run passes 179; Redis 6.2.24 compatibility passes 263. Fixture skips and runtime/server limits are recorded in the accepted decision. Array-result pipeline expansion remains proposed; release availability remains unconfirmed.

## Code-review follow-up validation

[2026-11-11 follow-up](plan/FEATURE-IMPROVEMENTS.md#code-review-follow-up-decision-2026-11-11): sync pool waiters no longer deadlock a saturated thread pool, waiters are woken in FIFO order by [RedisPoolWaiters](RedisPoolWaiters.cs), and the IDistributedCache adapter uses EVALSHA (ACLs need EVALSHA as well as EVAL). Multi-address sync and async connects share ConnectTimeout across attempts. `RedisConfig.PipelineEmptyValuesAsNull` restores pre-v10.5 null results for empty queued reads. Hostnames try IPv4 addresses before IPv6. The full net10.0 suite passes with 0 failures on Redis 6.0–8.x and Valkey 8–9, and in a TLS/IPv6 fixture; see [release verification](plan/FEATURE-IMPROVEMENTS.md#release-verification-2026-11-11). Release availability remains unconfirmed.

## Working-tree compatibility notes

- XADD/XACK/XDEL/XTRIM/XLEN support sync/async pipelines. Structured/group commands still require standalone calls; Streams transactions are rejected. Binary/typed AddStreamEntries helpers append generated IDs in bounded, non-atomic batches without automatic replay; see [batching](plan/FEATURE-IMPROVEMENTS.md#streams-batching-decision-2026-11-10). Group operations need writers; blocking reads need dedicated exclusive leases. Server/version/ACL errors propagate, and processing/acknowledgment remain application-owned. Core commands require Redis 5+, XAUTOCLAIM 6.2+; tested baselines are listed above.
- GETEX/GETDEL require Redis 6.2+; UNLINK requires Redis 4.0+. All require writer leases. New optional interfaces describe client support; unsupported server commands/ACL errors are propagated without fallback. GETEX accepts one expiry option or preserves TTL when none is supplied.
- Absolute cache expiry uses atomic SET PXAT on supporting servers. Older-server clients use atomic Lua after a syntax rejection; this requires EVAL and PEXPIREAT. Deadlines at/before the Unix epoch also use Lua. Failed NX/XX preserves the old value and deadline. No restricted-ACL or Valkey matrix has been executed.
- Async connection setup uses DNS/TCP/TLS and Redis initialization without blocking in default paths. Positive ConnectTimeout covers setup/default role verification; PoolTimeout separately bounds capacity waiting. Sentinel/custom resolvers, synchronous hooks/factories and UseGetClientBlocking retain their existing behavior. See the [async decision](plan/FEATURE-IMPROVEMENTS.md#fully-async-connection-decision-2026-11-10). RedisManagerPool preserves default overflow and now supports opt-in MaxLiveConnections, MaxWaitingClients and PoolTimeout. Its admission limit counts owned client capacity, not resolver probes or independently created clients; see the [accepted scope](plan/FEATURE-IMPROVEMENTS.md#bounded-admission-decision-2026-11-10).
- New regression fixtures: [expiry](../../tests/ServiceStack.Redis.Tests/RedisExpiryRegressionTests.cs), [pool admission](../../tests/ServiceStack.Redis.Tests/RedisPoolAdmissionTests.cs). These changes are not confirmed released.

## Review limits and planning

- RESP3/push tracking and Cluster routing were not located in the inspected source/contracts. Microsoft IDistributedCache is supplied by ServiceStack.Redis on .NET 10 only, using guarded source and conditional Microsoft package references. Raw command support does not provide the higher-level lifecycle/routing semantics proposed for them.
- Server/Valkey/managed-service compatibility must be checked per command, option, topology and ACL; package target frameworks and a broad server-version label do not establish it.
- [Unaccepted proposals](plan/FEATURE-PROPOSALS.md), [accepted improvements](plan/FEATURE-IMPROVEMENTS.md), [existing idea history](../../FUTURE.md).
