# ServiceStack.Redis accepted feature improvements

Decision: on 2026-11-10, the repository maintainer requested “please start with that” following the recommendation to implement expiry correctness and cancellation while waiting for pooled clients. This accepts the bounded first phases below, not the complete original proposals. Owner: ServiceStack.Redis. That first phase changed no shared interfaces and added no package dependencies. A subsequent command-group decision is recorded under REDIS-001 below.

Status: **Implemented in the working tree; not confirmed released**. Source baseline before changes: `101038b4ba5a65650e02491e98c7c8a4e1d46f5f`. The original records are preserved below each scoped decision; their unselected extensions remain proposals.

See [remaining proposals](FEATURE-PROPOSALS.md) and [existing features](../FEATURES.md).

## REDIS-001: Atomic cache expiry and focused modern command coverage

### Accepted scope and implementation

- Correct existing async `ICacheClientAsync.AddAsync/ReplaceAsync(TimeSpan)` mappings to forward millisecond expiry.
- Make existing sync/async DateTime Set/Add/Replace atomic: `SET PXAT` with NX/XX where applicable; use atomic SET plus PEXPIREAT Lua only after a syntax rejection on older servers or for deadlines at/before the Unix epoch. Preserve binary serialization, local/UTC conversion and failed-condition semantics.
- Do not fall back after ACL/network/other uncertain failures. Capability evidence is local to a client, not the global server-version field.
- Existing rejection of these cache operations within transactions/pipelines remains in place. No public command/interface expansion.

Compatibility: `SET PXAT` requires Redis 6.2+. The older-server fallback requires Lua/EVAL and PEXPIREAT permission/support (tested on Redis 6.0.20). Services disabling scripts cannot use this fallback. Modern servers need no EVAL permission for positive epoch deadlines. A successful past-deadline write immediately disappears; failed NX/XX does not alter the key or its expiry. This replaces a two-command race with one atomic mutation, but does not guarantee delivery after an uncertain network failure.

Deferred by the original decision: command wrappers and the remaining extensions. GETEX/GETDEL/UNLINK and bounded SCAN+UNLINK helpers were subsequently accepted below. Endpoint-wide capability infrastructure, new cache-expiry transaction/pipeline support and broader expiry/server validation remain proposed.

### Validation of accepted scope

New expiry tests reproduce baseline failures, check relative TTL parity, exact absolute deadlines, a single successful wire mutation, byte payloads, local/UTC dates, immediate expiry, and failed conditions. Redis 8.10.1 passed all 26 expiry cases; Redis 6.0.20 passed the 16 applicable TTL/condition/fallback cases. Exact-deadline tests using PEXPIRETIME require a newer server and were excluded on Redis 6. The fallback's syntax rejection followed by EVAL is two requests on first use of an older-server client; only the EVAL mutates the key. No concurrent-writer stress test, restricted-ACL integration fixture or Valkey matrix was run.

### Command-group decision: 2026-11-10

The maintainer requested “great sounds good, please do as recommended” after the recommendation to start the next bounded increment with GETEX/GETDEL/UNLINK. **Accepted and implemented in the working tree; not confirmed released.** This does not accept the other suggested pool, endpoint or Streams projects.

Owner: ServiceStack.Redis. Participant: ServiceStack.Interfaces supplies additive `IRedisNativeKeyCommands`, `IRedisNativeKeyCommandsAsync` and `RedisGetExOptions` contracts; existing client interfaces gain no required members. No new dependencies. The optional interfaces describe client implementation support, not discovered server capability. Third-party providers may opt in; otherwise the new extensions throw NotSupportedException without sending commands.

Scope and API:

- Native byte-oriented GetEx/GetDel/Unlink and matching ValueTask async methods accepting cancellation tokens. Existing native-interface references use extensions dispatching to the optional contracts.
- Rich string-oriented GetAndExpireValue/GetAndDeleteValue/Unlink and matching async extensions on IRedisClient/IRedisClientAsync. Native reads preserve all payload bytes; rich reads decode UTF-8 without changing existing JSON/POCO storage conventions. No new generic persistence facade or type-index cleanup is implied.
- GETEX supports one of EX, PX, EXAT, PXAT or PERSIST. No option preserves TTL. Conflicting options, nonpositive numeric expiry, null keys, empty UNLINK key sets and null elements are rejected locally. Absolute options are positive Unix timestamps, not DateTime values. Missing reads return null; an empty stored value returns empty bytes/string. UNLINK returns the number of keys removed, not whether one key succeeded.
- All three operations use an explicitly leased writer connection, including GETEX without an expiry option. Existing reader-manager semantics do not change. They use the normal native bytes/integer command paths and support pipelines/transactions.
- GETEX/GETDEL require Redis 6.2+, UNLINK requires Redis 4.0+. Send exactly the selected server command; propagate unknown-command, ACL and other server errors. No split GET+EXPIRE/DEL, Lua emulation or silent UNLINK-to-DEL fallback. Existing transport cancellation/retry behavior applies; an interrupted write may have an unknown outcome and these APIs do not add replay guarantees.

Validation on isolated servers: 36 new cases passed on Redis 6.2.24; a 198-case combined run passed on Redis 8.10.1, covering the new cases and existing cache, pool, transaction, pipeline and custom-command regressions. Two Redis 6.0.20 cases verified unsupported GETEX/GETDEL leave the stored value unchanged and UNLINK works. The new cases cover every expiry mode, past deadlines, all 256 byte values, empty/missing values, wrong-type replies, Unicode strings in pipelines/transactions, pre-cancellation, local validation and restricted ACL users with scripts disabled. Redis builds for all four targets; additive contracts also build for netstandard2.0. No Valkey or mid-flight cancellation fault-injection matrix was run. No performance improvement is claimed.

Implementation: [native commands](../RedisNativeClient.KeyCommands.cs), [extensions](../RedisClientExtensions.KeyCommands.cs), [regression tests](../../../tests/ServiceStack.Redis.Tests/RedisKeyCommandsTests.cs). Public examples are in redis/client.md, redis/async.md and the upcoming v10.5 release notes. Mark Released only after publication is confirmed.

### Pattern-cleanup decision: 2026-11-10

The maintainer requested all three next recommendations: SCAN+UNLINK cleanup, IPv6/opt-in rediss endpoints and Streams, with release notes updated as each completes. Pattern cleanup is **implemented in the working tree; release unconfirmed**. Native and rich sync/async UnlinkByPattern use raw binary SCAN keys, a configurable positive COUNT hint, a hard positive UNLINK batch limit, and caller cancellation between scans/batches. Return the sum of server-reported removals, not a unique-key snapshot count. Empty pages continue until cursor zero. Existing RemoveByPattern stays unchanged; unsupported commands/ACL errors propagate without DEL fallback. Explicitly reject pipelines/transactions. SCAN is not a snapshot: concurrent writes and duplicate keys can affect results; cancellation/errors may leave partial cleanup. Nine new cases passed on isolated Redis 8.10.1, covering 2,501 matching keys including a binary key, unrelated keys, hard batches, empty pages, partial cancellation, ACL denial and validation. All Redis target frameworks build. No concurrent-writer stress or performance benchmark is claimed.

### Hash-field expiration decision: 2026-11-11

The maintainer requested “sounds good, please implement” following the hash-field expiration recommendation. Owner: ServiceStack.Redis, canonical REDIS-001. Participant: ServiceStack.Interfaces supplies optional contracts and the single-choice condition enum. **Implemented and validated in the working tree; not confirmed released.** Accepted scope is native field expiry, TTL/deadline inspection and persistence; HGETEX/HSETEX, deferred array pipeline/transaction callbacks and other command families remain proposed.

- Native binary-key/field sync/async HExpire/HPExpire/HExpireAt/HPExpireAt, HTtl/HPTtl, HExpireTime/HPExpireTime and HPersist return long[] in requested order, preserving duplicates and all server statuses. Rich string and native-string-key/binary-field extensions dispatch to additive IRedisHashExpiryCommands/Async capabilities; existing provider interfaces gain no required members. No package/dependency changes.
- RedisHashExpiryCondition accepts None or one NX/XX/GT/LT condition. Explicit enum values make combinations invalid; undefined/combined values, negative expiration and invalid field lists are rejected locally. Zero and past nonnegative deadlines retain immediate-deletion semantics. Absolute values are Unix timestamps; native numeric limits remain server-enforced. Query and result parsing use invariant numbers.
- Require native support, documented from Redis 7.4 / Valkey 9. No static global version guess, Lua fallback, alternate-key layout or whole-key expiration emulation. Propagate ACL/unsupported/wrong-type errors. Mutations need writer leases; read-only TTL queries retain replica consistency limitations. Parent key TTL, existing POCO hash conventions and the private IDistributedCache format are unchanged.
- Standalone calls only. Reject pipelines/transactions before sending these commands until integer-array callback handling is separately accepted and tested. Preserve complete per-field statuses rather than coercing results to a boolean or unordered map.
- Strict RESP array count/integer/CRLF parsing detects invalid/truncated replies without the legacy synchronous EOF reconnect. Cancellation interrupts reads via captured-socket closure, retains caller-token identity and closes/reset buffers before reuse. Malformed/truncated replies are not replayed as a parser fallback; standard transport retry/unknown-write limitations remain. A fault regression caught and fixed residual send-buffer data being carried onto the next connection.

Verification: 62 new cases include one TLS-only case and nine unsupported-server cases (each exercises sync and async variants). All 53 applicable cases pass in an isolated TLS fixture; the normal supported-server runs pass 52 with fixture/unsupported cases skipped. Redis 7.4.12, Redis 8.10.1 and Valkey 9.1.3 pass the supported matrix; Redis 6.2.24 and Valkey 8.1.11 pass all 23 applicable unsupported/validation/protocol cases. The wider regression run passes 451 cases, with connection/TLS/configuration and unsupported-server skips recorded separately. All four Redis and five Interfaces target frameworks build; runtime tests use net10.0. Coverage includes all units/conditions, real expiry, exact parent/field deadlines, missing/persistent/duplicate/binary/Unicode/empty fields, HSET/increment TTL behavior, ACLs with scripts denied, precancellation, batching rejection, malformed/truncated replies, cancellation, clean reconnection and TLS. No performance benchmark or exhaustive managed-service/failover matrix is claimed.

Implementation: [native APIs/parser](../RedisNativeClient.HashExpiry.cs), [extensions](../RedisClientExtensions.HashExpiry.cs), [behavior/compatibility tests](../../../tests/ServiceStack.Redis.Tests/RedisHashExpiryTests.cs), [protocol fault tests](../../../tests/ServiceStack.Redis.Tests/RedisHashExpiryProtocolTests.cs), and redis/hash-field-expiration.md. Confirm released availability only after publication.

#### Original hash-field expiry proposal (2026-11-10; preserved)

Recommendation after the maintainer excluded telemetry/health checks and renewable leases: prioritize native hash-field expiration as a bounded continuation of REDIS-001. **Proposed only; not accepted or implemented.** Expected scope: small–medium for the basic command group; pipeline result handling and optional newer atomic commands require their own tests. This is an engineering value/scope judgment; customer demand has not been measured.

Existing hash APIs and whole-key expiry are present. A targeted search of Redis source/tests and shared Redis contracts found no named HEXPIRE/HPEXPIRE/HTTL/HPTTL/HPERSIST/HGETEX/HSETEX wrappers; Custom/RawCommand can already issue server-supported commands. The customer benefit is independently expiring related values in application-owned hashes, such as cached lookups, temporary user attributes or time-bucket counters.

Proposed first scope:

- Native binary and rich sync/async wrappers for relative/absolute hash-field expiry in seconds/milliseconds, TTL inspection and removing field expiry. Preserve per-field results for missing fields, existing expiry and failed NX/XX/GT/LT conditions. Validate conflicting options and field lists locally.
- Reuse optional contracts without adding required members to existing client/provider interfaces. Route expiry changes to writers. Plan deferred integer-array reply handling explicitly before claiming pipeline/transaction compatibility.
- Test parent-key TTL isolation, field replacement semantics, missing/empty/binary data, multi-field result order, conditional updates, cancellation and restricted ACLs. Do not change existing typed POCO hash storage or the private IDistributedCache hash format.
- Require actual native server support and propagate unsupported-command/ACL failures; no silent key-expiry, alternate-key or Lua emulation. The basic expiry family starts in Redis 7.4; Valkey documents HEXPIRE from 9.0, so the earlier Redis 6.2/Valkey 8.1 fixtures are unsupported-server checks for this feature.

A subsequent, separately accepted scope could add Redis 8.0 HGETEX/HSETEX for atomic hash read/write plus expiry. Do not emulate these using split reads/writes and expiration commands. This follows the completed string GETEX work without requiring a new topology or push-message transport.

Primary references checked 2026-11-10: [Redis HEXPIRE](https://redis.io/docs/latest/commands/hexpire/), [HPTTL](https://redis.io/docs/latest/commands/hpttl/), [HPERSIST](https://redis.io/docs/latest/commands/hpersist/), [HGETEX](https://redis.io/docs/latest/commands/hgetex/), [HSETEX](https://redis.io/docs/latest/commands/hsetex/), and [Valkey HEXPIRE](https://valkey.io/commands/hexpire/). These establish individual command availability/semantics, not a tested client compatibility claim.

### Original proposal (preserved; acceptance is limited above)

- Original recommendation: Investigate next.
- Potential value: High: improves the existing cache surface for common callers.
- Relative effort: Small–medium for the first command group; broad command modernization is separate.
- Evidence confidence: High for the inspected overloads and missing named methods; capability/ACL compatibility needs validation.
- Customer validation: not yet established; select a real application/workaround before acceptance.

### Existing behavior and gap

Synchronous relative-expiry Set/Add/Replace and async Set already use SET with expiry in supported paths. The inspected async ICacheClient Add/Replace overloads taking TimeSpan do not forward expiresIn to native SET; this source-level mismatch needs a focused TTL regression check before any feature expansion. In contrast, the DateTime Add/Set/Replace overloads in RedisClient.ICacheClient.cs issue a value mutation followed by ExpireEntryAt. Failure or another writer between these commands can leave unexpected expiry behavior. SCAN-based RemoveByPattern already exists, including a regression test across 2,500 matching keys. Custom/RawCommand can issue newer server commands, so lack of named APIs is not inability to invoke them. Targeted source/contract searches did not locate named GETEX, GETDEL or UNLINK wrappers.

### Proposed outcome and bounded approach

First establish sync/async TTL parity, including the observed conditional async expiry mismatch, and make absolute-expiry cache mutations a single atomic operation. Then add a small symmetric native/rich API group for GETEX, GETDEL and UNLINK, with explicit option/result contracts and capability handling.

1. Verify and correct the async conditional relative-expiry mapping as a bounded reliability change if reproduced. Specify UTC conversion, past deadlines, millisecond precision and condition-failure behavior for existing DateTime overloads. Use SET with the required condition and expiry, or an atomic Lua fallback that preserves the agreed deadline semantics.
2. Add optional methods/capability interfaces without forcing third-party IRedisClient implementations to immediately implement new required members. Preserve raw binary values and existing POCO serialization.
3. Treat GETEX as a write and route it to a writer. Add bounded SCAN+UNLINK removal as an opt-in where supported; keep documented DEL behavior/fallback explicit.
4. Cache capability evidence per endpoint/server identity and invalidate it on topology change. The existing static ServerVersionNumber alone is insufficient evidence for a heterogeneous fleet. Distinguish unknown-command errors from ACL denial; never retry an already-executed mutation as a probe.

### Acceptance criteria if selected

- A concurrent replacement cannot receive another writer's expiration through a split mutation/expiry sequence.
- Test relative and absolute TTL parity, including async Add/Replace(TimeSpan), past/future deadlines, UTC/local inputs, failed NX/XX, null/missing values and all byte values.
- Verify wire commands, cancellation, pipeline/transaction support and exact fallback semantics against pinned server versions, including a restricted ACL user.
- Existing pattern-removal regression cases still pass; document SCAN's weak snapshot guarantees and ensure cleanup is bounded rather than claiming atomic bulk deletion.

### Alternatives and compatibility

Custom commands and application Lua already cover uncommon operations. Start with the cache correctness/semantics increment and add only command wrappers with demonstrated usage. Do not turn this into “support every command in the latest server.”

Older servers and managed services have different command availability. Lua fallback itself requires permission. UNLINK removes keys promptly while reclamation happens separately; it does not make a large scan free. Keep payload serialization and conditional-write return values compatible.

### Evidence

- [RedisClient.ICacheClient.cs](../RedisClient.ICacheClient.cs)
- [RedisClient.Async.cs](../RedisClient.Async.cs)
- [RedisClient.cs](../RedisClient.cs)
- [RedisNativeClient.cs](../RedisNativeClient.cs)
- [RedisNativeClient.Async.cs](../RedisNativeClient.Async.cs)
- [RedisReviewRegressionTests.cs](../../../tests/ServiceStack.Redis.Tests/RedisReviewRegressionTests.cs)
- [RedisCacheClientTests.Async.cs](../../../tests/ServiceStack.Redis.Tests/RedisCacheClientTests.Async.cs)
- [redis/custom-commands.md](../../../../../docs.servicestack.net/MyApp/_pages/redis/custom-commands.md)

The official [GETEX](https://redis.io/docs/latest/commands/getex/), [SET options](https://redis.io/docs/latest/commands/set/) and [UNLINK](https://redis.io/docs/latest/commands/unlink/) references establish command semantics and individual version requirements. Pin the supported option subset; latest documentation can include options beyond the intended baseline. Sources checked 2026-11-10.


## REDIS-002: Cancellable pool admission and optional bounded overflow

### Accepted scope and implementation

- Forward caller tokens through `PooledRedisClientManager` async reader/writer acquisition, reject precancelled calls before reserving slots, and observe cancellation at initialization/hand-off boundaries.
- Use one monotonic pool-wait deadline across repeated wakeups. Capture the wait signal under the pool lock to avoid losing a return notification.
- Return reservations/leases on acquisition failure, detach/dispose newly created clients on cancellation, wake waiters on return/failover/disposal, and reject disposed managers with ObjectDisposedException.
- Preserve exclusive client leases, existing lightweight-manager overflow behavior, and sync interface contracts.

Compatibility/limits: the default async admission path is cancellable. `UseGetClientBlocking=true` retains legacy blocking behavior and only checks precancellation. At this initial phase, resolver/connect/TLS initialization still contained synchronous work; the subsequent fully async connection decision below supersedes that limit for default async paths. PoolTimeout bounds waiting for an available slot; it is not an end-to-end connect/TLS deadline. No FIFO guarantee, bounded waiter count or new telemetry contract is introduced.

Deferred by the initial decision: RedisManagerPool bounded admission and the remaining transport/measurement work. Bounded admission was subsequently accepted below. Fully async setup and a connection deadline were subsequently accepted below. Fairness/performance benchmarking and telemetry exports remain proposed.

### Validation of accepted scope

Fourteen reader/writer cases cover pending cancellation (including the caller token), precancellation, cancellation during client creation with disposal/reservation cleanup, client return, repeated broadcasts without extending PoolTimeout, unbounded waits ended by disposal, and failover wakeups. These are targeted deterministic cases, not an exhaustive concurrent failover/socket stress test.

### Bounded-admission decision: 2026-11-10

The maintainer requested “sounds great, please implement” after the recommendation to implement optional RedisManagerPool connection limits next. **Accepted and implemented in the working tree; not confirmed released.** Owner: ServiceStack.Redis; no shared-interface or added dependency changes. This decision does not accept SCAN+UNLINK helpers, endpoint modernization or Streams.

Scope and configuration:

- Add constructor-snapshotted RedisPoolConfig options MaxLiveConnections (default null), MaxWaitingClients (default 0) and PoolTimeout (default 30000 milliseconds). A configured positive cap enables bounded admission; zero waiting capacity gives fail-fast behavior. Null/-1 timeout permits indefinite waits. Existing MaxPoolSize continues to mean retained capacity. Retained size may exceed the live limit without creating that many clients.
- Share the cap across synchronous/async clients and the read-only aliases. Sync waiters block; async waiters honor caller cancellation. A full waiting queue throws InvalidOperationException, deadline expiry throws TimeoutException, and manager disposal throws ObjectDisposedException. One monotonic admission deadline spans wakeups; no FIFO fairness guarantee.
- Track retained and overflow clients, in-flight creation reservations and closing clients. Keep capacity until close completes. On failover close idle clients, retain accounting for old active leases until return, and discard creations from a superseded generation. Disposal wakes waiters and closes tracked clients; pending synchronous creation is rejected/closed at its next boundary. Failed creation/cancellation releases reservations.
- Preserve default unmanaged overflow behavior. Independently improve default-mode async acquisition to reject precancelled calls and return a lease if cancellation is observed after initialization.
- Add bounded-mode GetStats counters: clientsLive, clientsCreating, clientsWaiting, clientsMaxLive and clientsMaxWaiting. Existing retained-pool counters retain their meanings. No OpenTelemetry/export package is introduced.

Limit boundaries: MaxLiveConnections bounds the manager's admitted client capacity, including idle clients and creation/closing reservations, not all sockets on a Redis server. Internal resolver discovery probes, custom factory internals and independently created clients are outside that accounting. Synchronous resolver/connect/TLS work is not interrupted by PoolTimeout or cancellation; tokens are checked at initialization boundaries. Applications must return leases; failover cannot create replacements beyond the cap while old leases remain outstanding. Existing transport cancellation/protocol cleanup rules are unchanged.

Validation: 25 new admission cases and 198 existing command/cache/pool/pipeline/transaction/custom-command cases passed together (223 total) on isolated Redis 8.10.1/net10.0. The new cases cover fail-fast defaults, bounded waiters, sync/async wakeups, token identity, pre-cancellation, creation failure/cancellation/disposal/failover races, repeated wakeups, shutdown, idle and active failover, double return, delayed overflow close, and 100 cancellation/return races. Saturation used 48 mixed sync/async borrowers over 144 lease cycles with retained size 2/live cap 4, asserting at most four tracked clients and sampling named server sockets at or below four, followed by complete disposal. These tests cover ordinary standalone resolution; they do not establish a socket cap during resolver topology discovery. All four Redis target frameworks build with existing warnings. No comparative throughput/allocation benchmark, TLS fault-injection or Valkey matrix was run.

Implementation: [admission lifecycle](../RedisManagerPool.Admission.cs), [manager/configuration](../RedisManagerPool.cs), [async entry points](../RedisManagerPool.Async.cs), [tests](../../../tests/ServiceStack.Redis.Tests/RedisManagerPoolAdmissionTests.cs). User-facing configuration and exception contracts are documented in redis/client-managers.md and the upcoming v10.5 notes. Release availability remains unconfirmed.

### Fully async connection decision: 2026-11-10

The maintainer requested “Can you implement Fully async connection setup”. This accepts the transport/default-manager phase of REDIS-002, linked from REDIS-007. Owner: ServiceStack.Redis. **Implemented and validated in the working tree; not confirmed released.** It does not accept telemetry, renewable leases, per-endpoint certificate policy or additional Streams work. No dependencies or required shared-interface members were added.

- Async commands now await DNS, per-address TCP connection, TLS authentication, AUTH/ACL, SELECT, CLIENT SETNAME and initial INFO discovery. Concrete RedisNativeClient/RedisClient expose optional ConnectAsync for eager setup. A separate handshake buffer preserves pipeline/application commands. Certificate/hostname validation and configured global callbacks/protocols are preserved.
- A positive ConnectTimeout creates one deadline across all resolved addresses, TLS and initialization, including the INFO/proxy compatibility reconnect. Default resolver cold master verification shares that budget through ROLE. Zero/negative leaves setup unbounded except caller/command timeouts. A shorter command SendTimeout may win. PoolTimeout still covers capacity waiting independently. Setup TimeoutException does not restart a fresh connection budget through resolver retry.
- Default RedisResolver uses async role verification, async topology probes and cancellable retry delays; BasicRedisResolver remains lazy without network I/O. PooledRedisClientManager, RedisManagerPool (bounded/default overflow), and BasicRedisClientManager async paths use the optional IRedisResolverAsync contract. Configure manager timeouts, hooks and lazy DB before verification. Reused pooled DB resets await SELECT outside pool locks and recheck disposal/cancellation before hand-off. Retain exclusive leases and close/dispose incomplete creations before releasing reservations.
- ConnectionFilterAsync on concrete clients and all managers runs after existing OnConnected/ConnectionFilter only for connections opened asynchronously. It is awaited under the setup token; hooks should honor that token. Existing synchronous hooks and factories remain synchronous. No Task.Run wrapping or sync-over-async network setup was introduced in the default async path. Synchronous acquisition behavior remains intact.
- Caller cancellation preserves the caller token in OperationCanceledException. Deadline expiry throws TimeoutException. Close captured attempt sockets to interrupt legacy connect/TLS/read APIs; close/reset all client transport state on failure. A cancelled INFO probe never marks the server unsupported or silently retries. Version-dependent rich APIs and transaction creation now fetch server metadata asynchronously instead of accidentally opening synchronous connections.

Compatibility limits: custom resolvers without IRedisResolverAsync, RedisSentinelResolver topology/role discovery and UseGetClientBlocking=true retain synchronous resolution/acquisition behavior. Custom resolver implementations can opt in. The subsequent reliability pass preserves legacy virtual CreateRedisClient overrides for inherited async acquisition; explicit async overrides take precedence. Synchronous user callbacks/factories can still block and are not forcibly interrupted. On net472/net6, cancellation stops awaiting noncancellable DNS and observes late failures; the platform lookup may finish without owning sockets. The existing static ServerVersionNumber heuristic remains unchanged and is not per-endpoint capability discovery. Unknown write outcomes retain normal transport semantics. This is not a single deadline across pool waiting, topology changes and all command retries.

Verification: all 38 new connection cases pass in an isolated IPv6-enabled Redis 8.10.1/TLS fixture, including actual certificate/hostname rejection, ACL identity, database/client-name setup and cloning. The host combined regression run passes 343 cases with eight skips (six IPv6/TLS fixture cases covered separately; two pre-existing configuration skips). Connection/pool suites pass 74 cases with three fixture skips separately on Redis 6.2.24 and Valkey 8.1.11. All 34 IDistributedCache/HybridCache cases also pass after the transport change. All four Redis target frameworks build; runtime execution was net10.0. Fault fixtures cover stalled TLS/AUTH/SELECT/CLIENT/INFO/ROLE, a shared multi-stage deadline, caller-token identity, failed DNS/refused sockets, INFO-incompatible proxies, cancellation cleanup, async DB resets and cold version-dependent APIs. That initial run did not exercise net472/net6/net8 runtimes; subsequent reliability validation is recorded below. Deterministic slow-DNS/SYN injection, exhaustive Sentinel/failover stress and performance were not validated.

Implementation: [async connections](../RedisNativeClient.Connection.Async.cs), [async resolver contract](../IRedisResolverAsync.cs), [default async resolver](../RedisResolver.Async.cs), [connection tests](../../../tests/ServiceStack.Redis.Tests/RedisAsyncConnectionTests.cs). Usage and release notes: redis/async.md and releases/v10_05.md.

### Reliability review decision: 2026-11-11

The maintainer requested “please do as recommended” after the review recommended a focused reliability pass and deterministic regressions. This accepts corrections to existing REDIS-001/REDIS-002/REDIS-004 behavior, owned here under REDIS-002. **Implemented and validated in the working tree; not confirmed released.** Array-result pipeline/transaction expansion remains proposed under REDIS-001/REDIS-004; no new shared-interface requirements or package dependencies were added.

- Sync and async pooled acquisition share unique slot reservations and topology-generation checks. Failed/stale creation cannot publish an old-host client or release another acquisition's reservation. Invalid-client replacement reserves its slot exclusively. Default RedisManagerPool overflow remains unbounded; bounded admission retains its existing contract.
- Reused pooled clients reset their database outside pool locks and recheck topology before hand-off. If failover disposes a client during SELECT, acquisition retries against replacement hosts. Disposed clients are terminal transport failures and never enter the retry loop.
- Async RESP EOF paths close/reset the connection and throw to their owning async operation; parsers never reconnect synchronously. Async response lines require complete CRLF; malformed/truncated bulk responses are transport errors. Connection setup, caller cancellation and normal command retry ownership remain separate. Pipelines/bulk appends do not automatically replay unknown writes.
- RedisResolver subclasses overriding the legacy virtual CreateRedisClient retain that synchronous policy in async acquisition, including credentials/TLS/routing. Explicit async overrides retain precedence. Unrelated overloads do not confuse dispatch, and cancellation/configuration failures dispose the newly created client. Legacy synchronous user code remains non-interruptible.
- Async pipeline flushing honors SendTimeout for the flush and ReceiveTimeout separately for each queued reply, including when an error callback swallows an exception. Configured timeout throws TimeoutException; caller cancellation retains its original token. Interrupted or malformed pipeline state is closed/reset. Bulk Streams append inherits those limits without replay.
- Sync/async pipelines and transactions preserve empty byte/string GETEX/GETDEL results separately from nil/missing values. The shared queued byte reader no longer collapses empty payloads to null.

Verification: all 48 new [reliability cases](../../../tests/ServiceStack.Redis.Tests/RedisReliabilityTests.cs) pass on .NET 10, .NET 6, .NET 8 and Mono 6.12 executing the net472 build, against isolated Redis 8.10.1 and scripted local TCP fault servers. The broader net10.0 run passes 507 cases with 20 skips; a separate IPv6/TLS fixture passes 179 cases with nine unsupported-server skips and exercises the TLS/IPv6 cases skipped on the host. All four Redis targets build. The existing 64-borrower test now waits for workers before disposing its manager; the earlier test incorrectly outlived its pool. Windows .NET Framework, older-runtime TLS, slow DNS/SYN injection, exhaustive Sentinel/failover stress and performance were not validated. The final Redis 6.2.24/net10.0 compatibility run passes 263 cases with 44 skips for unsupported hash-field expiry and dedicated network fixtures; this includes all 48 new reliability cases.

### Code-review follow-up decision: 2026-11-11

The maintainer requested "can you implement your recommendations" after a code review of the working tree. Owner: REDIS-002 (pool/connection behavior) with REDIS-006 for the cache adapter. **Implemented and validated in the working tree; not confirmed released.**

- Sync pool waiting no longer blocks on an incomplete async acquisition. Sync callers in PooledRedisClientManager and bounded RedisManagerPool now wait on their own signal, so a release wakes them without a free thread-pool thread. Before the fix, a capped thread pool with 8 sync waiters deadlocked; 10.4.2 served them in 501ms, and the fix serves them in 502ms. PoolTimeout now fires on a capped pool (5002ms for a 5000ms timeout).
- [RedisPoolWaiters](../RedisPoolWaiters.cs) replaces the broadcast events with a FIFO queue: a returned client or freed slot wakes one waiter, failover and disposal wake all, and a woken waiter that fails or is cancelled before acquiring passes its wakeup on. Barging by new arrivals is still possible.
- RedisDistributedCache runs its scripts with EVALSHA and falls back to EVAL only on NOSCRIPT, which means the script did not run. ACLs now need EVALSHA as well as EVAL. IBufferDistributedCache was not added: the native client already returns `byte[]`, so it would add a copy rather than remove one.
- Async connects to multi-address hosts give each attempt except the last an equal share of the remaining ConnectTimeout. Async pipeline flushes reuse one linked token source per flush. The legacy CreateRedisClient override check is cached per type. Role probes apply HostLookupTimeoutMs after manager configuration. ParseUri accepts the legacy `idletimeout` alias and rejects passing both spellings.

Verification: 9 new [waiter tests](../../../tests/ServiceStack.Redis.Tests/RedisPoolWaiterTests.cs) cover FIFO hand-off, cancellation racing a wakeup (passing 15 of 15 repeated runs), sync waiters on dedicated threads and the URI alias. 2 new cache tests cover the NOSCRIPT fallback, and the script-denial test now denies EVAL and EVALSHA. The full net10.0 suite passes 1,748 tests with 153 skipped; 3 failures (`ManagerPool_`/`Pool_can_support_64_threads...` and async `Can_change_db_at_runtime`) also fail with the pre-follow-up sources. All four Redis targets build.

#### Second follow-up: 2026-11-11

The maintainer approved the follow-up recommendations ("sounds good, please do as recommended").

- Test fixes: `RunSimultaneously` now waits for its workers inside the manager's `using`, so the pooled managers are no longer disposed mid-test and `ObjectDisposedException` is gone. The async `Can_change_db_at_runtime` no longer uses its client after disposing it.
- Regression test: `Sync_waiters_are_served_when_every_thread_pool_thread_is_blocked` caps the thread pool in-process at the processor count, fills it with sync waiters, then restores the pool. With the pre-fix pool sources, 16 of 16 waiters deadlocked and the test failed; it passes with the fix.
- `RedisConfig.PipelineEmptyValuesAsNull` (default false, cleared by `Reset()`) restores null results for empty queued reads. Queued int callbacks now read an empty value as 0. Before this fix, removing the empty-to-null conversion made them throw `FormatException`.
- Sync connects with a positive ConnectTimeout resolve DNS once, still bounded by ConnectTimeout. They then try each address on its own socket, with each attempt except the last getting an equal share of the remaining budget. Normal, unreachable and refused connects behave the same as in 10.4.2. Falling through from IPv6 to IPv4 was not exercised, because no dual-stack host was available.

Verification: the full net10.0 suite passes 1,762 tests with 0 failures and 153 skipped. All four Redis targets build.

#### Release verification: 2026-11-11

The maintainer asked "is there anything you can do?" before release.

- Fixed a regression vs 10.4.2. Hostnames used to connect over IPv4 only, and the dual-stack change made them follow the OS address order, which prefers IPv6. With the default ConnectTimeout (-1), a host whose IPv6 address drops packets hung for over 60s, where 10.4.2 connected in 33ms. Sync and async connects now try IPv4 addresses before IPv6 (`PreferIPv4`), and sync connects without a ConnectTimeout also resolve once and try each address. With a blackholed `::1`, both connect via 127.0.0.1 in 33ms (sync) and 8ms (async), matching 10.4.2. With a blackholed IPv4 and ConnectTimeout=2000, both fall through to `::1` in about 1s; 10.4.2 fails.
- Benchmark: sync and async GetClient+Dispose on an available client takes 461–514ns with 0 bytes allocated, vs 484–528ns and 0 bytes in 10.4.2. No regression.
- Server matrix (full net10.0 suite): Redis 6.0.20, 6.2.24, 7.4.12 and 8.x and Valkey 8.1.11 and 9.1.3 all pass with 0 failures. Test fixes: the expiry tests used PEXPIRETIME (7.0+) and expected a single command on servers older than 6.2. Three tests lacked their 6.2+ version guard for GETEX/GETDEL/XAUTOCLAIM.
- TLS/IPv6 fixture: a Redis TLS server in an IPv6-enabled container namespace, with a certificate valid for `::1` only. The full suite passes 1,771 tests with 0 failures.
- Mono 6.12 (net472 build, NUnitLite) runs 308 pool, connection, pipeline, expiry, Streams and endpoint cases: 295 pass and 8 skip. The 5 failures also fail with the pre-follow-up sources, so they are Mono-specific. Four time out waiting for a cancelled blocking read to stop, and one is the port-0 URI check under Mono's System.Uri. Windows .NET Framework remains unverified.

### Original proposal (preserved; acceptance is limited above)

- Original recommendation: Investigate next.
- Potential value: High for applications under concurrency spikes and request cancellation.
- Relative effort: Medium; preserve two different manager contracts.
- Evidence confidence: High direct source evidence; fairness/performance still require measurement.
- Customer validation: not yet established; select a real application/workaround before acceptance.

### Existing behavior and gap

PooledRedisClientManager already has a genuine async acquisition path using AsyncManualResetEvent; it is not simply a blocking-only pool. However, its IRedisClientsManagerAsync methods receive a CancellationToken and call private acquisition methods without forwarding it. WaitForWriter/Reader use timeout-only cancellation. RedisManagerPool intentionally creates clients outside the retained pool when saturated; its async interface wraps GetClient(true), and connection initialization can perform synchronous work. Existing tests explicitly distinguish the nonblocking RedisManagerPool from the blocking Pooled manager.

### Proposed outcome and bounded approach

Honor caller cancellation and a total acquisition deadline in the bounded pool; offer explicitly selected overflow/admission policy for the lightweight manager. Expose pool waiting separately from Redis command duration.

1. Thread the caller token through reader/writer waits and initialization boundaries. Measure one monotonic acquisition deadline across wakeups, rather than granting a new full timeout after every signal.
2. Define cancellation/return/failover/disposal races and release any reserved slot before surfacing failure. Avoid synchronous network initialization on a purported fully asynchronous path where the platform supports async connect/TLS.
3. For RedisManagerPool retain current overflow behavior by default; separately design optional maximum live connections, bounded waiters and fail-fast behavior. Pool retention size and total connection limit are different settings.
4. Prefer targeted evolution of existing managers before considering connection multiplexing. Preserve exclusive leases for a client; do not enable concurrent operations on the same stateful socket.

### Acceptance criteria if selected

- Cancel while waiting and prove no later client lease/slot leak; test both reader and writer paths, pre-cancelled calls and cancellation during connection setup.
- Repeated wakeups cannot extend the total deadline. Disposal/failover wakes waiters with defined outcomes.
- Saturation tests measure live sockets, queue wait, throughput and allocation under current and new policies; existing default overflow tests keep their behavior.
- Test transactions, WATCH, pipelines and blocking operations on exclusive leases; no protocol bytes are drained/reused after an unknown cancellation state.

### Alternatives and compatibility

Applications can put their own concurrency limiter around existing managers. A small documented helper may suffice for overflow control, but it cannot repair cancellation that is dropped inside pool admission. Avoid building a new multiplexed manager before measuring the pool bottleneck.

Strict FIFO fairness is an explicit design choice, not an automatic consequence of SemaphoreSlim. Preserve read-replica routing, manager disposal ownership and TLS settings on replacement clients. A cancellation error is not authority to replay an uncertain command.

### Evidence

- [PooledRedisClientManager.Async.cs](../PooledRedisClientManager.Async.cs)
- [PooledRedisClientManager.cs](../PooledRedisClientManager.cs)
- [RedisManagerPool.Async.cs](../RedisManagerPool.Async.cs)
- [RedisManagerPool.cs](../RedisManagerPool.cs)
- [RedisNativeClient_Utils.Async.cs](../RedisNativeClient_Utils.Async.cs)
- [PooledRedisClientManagerTests.Async.cs](../../../tests/ServiceStack.Redis.Tests/PooledRedisClientManagerTests.Async.cs)
- [PooledRedisClientManagerTests.cs](../../../tests/ServiceStack.Redis.Tests/PooledRedisClientManagerTests.cs)
- [RedisManagerPoolTests.cs](../../../tests/ServiceStack.Redis.Tests/RedisManagerPoolTests.cs)
- [redis/client-managers.md](../../../../../docs.servicestack.net/MyApp/_pages/redis/client-managers.md)


## Shared verification evidence

Validation on 2026-11-10 used isolated ephemeral Redis containers, not the developer's existing Redis service. The combined net10.0 run passed 92 tests: 40 new expiry/admission cases and 52 existing cache/pool/review regressions. The 16 older-server expiry cases passed separately. The project builds for net472, net6.0, net8.0 and net10.0; builds report existing warnings. Record a published version only after release availability is confirmed.

## REDIS-007: Explicit modern endpoint parsing and per-endpoint TLS policy

Decision: 2026-11-10, maintainer request “please do all of these recommendations and update the Release Notes as they're done.” Owner: ServiceStack.Redis; this URI/IPv6 increment adds no shared-interface contracts. Accepted: add IPv6 socket connections, bracketed host round trips, an explicit URI parser with ACL credentials/DB paths/escaping, and automatic rediss:// handling. Preserve legacy redis:// client-name/password interpretation. Per-endpoint certificate-policy/rotation APIs remain proposed; async connect/TLS is accepted under REDIS-002 above. Status: implemented in the working tree; release unconfirmed. Sixteen modern endpoint cases passed in an isolated IPv6-enabled container, including IPv6 sync/timed connections, DNS IPv4 fallback, pool failover, round trips, escaped ACL identities, TLS cloning, mismatched hostnames and untrusted-certificate rejection. Host-side modern/legacy configuration tests passed 59 cases (five fixture/legacy skips); all Redis targets build. TLS tests explicitly pin a temporary certificate while still requiring matching names; production validation defaults are unchanged. Parser uses default ports 6379/6380, rejects duplicate/unknown query options and SSL/auth/DB query overrides, and sanitizes URI failures. Legacy redis:// still uses client-name/password userinfo; RedisEndpoint.ParseUri explicitly opts into ACL URI semantics for redis://, while rediss:// automatically opts in. IPv6 endpoint/log formatting uses brackets and round-trips through the legacy parser.

### Original proposal (preserved; acceptance is limited above)

- Recommendation: Investigate next compatibility design.
- Potential value: High for cloud configuration, IPv6-only networks and multiple ACL identities.
- Relative effort: Medium; network setup is broader than parsing alone.
- Evidence confidence: High direct parsing/socket evidence; individual deployment failures were not reproduced.
- Customer validation: not yet established; select a real application/workaround before acceptance.

### Existing behavior and gap

RedisExtensions strips redis:// and parses host/auth with string splits. ConfigTests deliberately interpret user:pass@host as Client plus Password; ACL Username is a query option. Connect creates an IPv4 InterNetwork socket, and TLS authentication/callback settings are principally global/client fields. RedisEndpoint identity already includes ACL Username, and clone regression tests already preserve TLS settings. These fixes must not be proposed as absent.

### Proposed outcome and bounded approach

An opt-in standards-oriented URI/options path with rediss://, bracketed IPv6, explicit ACL user/database selection and per-endpoint TLS authentication policy, while preserving legacy connection-string meanings.

1. Add a separate parser/versioned mode or strongly typed options builder. Do not silently reinterpret legacy clientid:password as ACL username. Define escaping, port/database validation and conflicting URI/query options.
2. Resolve IPv4/IPv6 addresses and connect within one total deadline. Parsing an IPv6 literal is insufficient while sockets remain IPv4-only.
3. Add endpoint-specific TLS server name/certificate selection/validation settings and preserve them through cloning, Sentinel discovery and pooled replacements. Never default to accepting invalid certificates.
4. Include all security-relevant configuration in endpoint/pool isolation semantics. Prefer an immutable credential/policy identity and explicit rotation/drain over mutable secrets on a shared pool.

### Acceptance criteria if selected

- Existing ConfigTests retain their expected Client/Username behavior; new-mode tests cover URI-escaped credentials, DB paths, rediss, bracketed IPv6 and malformed options.
- Connect to pinned IPv4/IPv6 and TLS test endpoints; verify SNI, hostname failure, certificate rotation and cancellation/total deadline.
- Clone/failover preserve TLS and ACL identity; two different security policies cannot accidentally reuse the same connection.
- Logs never print passwords, auth arguments or full connection strings. Document an explicit legacy migration path.

### Alternatives and compatibility

Use the existing query-string ACL/TLS form where sufficient. Ship IPv6 connection support independently if that is the concrete need; do not bundle every TLS option with a parser rewrite unnecessarily.

Framework TLS APIs differ, especially net472. Delegates and certificate rotation complicate equality/caching. Endpoint compatibility must include Sentinel and replicas, not just direct RedisClient construction. No changes to persisted namespace/hash placement are implied.

### Evidence

- [RedisExtensions.cs](../RedisExtensions.cs)
- [RedisEndpoint.cs](../RedisEndpoint.cs)
- [RedisNativeClient_Utils.cs](../RedisNativeClient_Utils.cs)
- [RedisClient.cs](../RedisClient.cs)
- [RedisSentinel.cs](../RedisSentinel.cs)
- [ConfigTests.cs](../../../tests/ServiceStack.Redis.Tests/ConfigTests.cs)
- [SslTests.cs](../../../tests/ServiceStack.Redis.Tests/SslTests.cs)
- [RedisPasswordTests.cs](../../../tests/ServiceStack.Redis.Tests/RedisPasswordTests.cs)
- [RedisReviewRegressionTests.cs](../../../tests/ServiceStack.Redis.Tests/RedisReviewRegressionTests.cs)
- [redis/client-managers.md](../../../../../docs.servicestack.net/MyApp/_pages/redis/client-managers.md)


## REDIS-004: Typed Streams and consumer-group operations

Decision: 2026-11-10, maintainer request “please do all of these recommendations and update the Release Notes as they're done.” Owner: ServiceStack.Redis; ServiceStack.Interfaces participates only for additive contracts/models. Accepted: binary-safe native sync/async append/range/length/read/trim/delete commands, consumer groups, explicit acknowledgment, pending inspection/claim/recovery, and a JSON typed facade. Preserve lossless IDs and deleted-payload distinctions; no automatic acknowledgment, retry loop, MQ adapter or exactly-once claim. Status: implemented in the working tree; release unconfirmed.

### Streams implementation and verification

- Add optional IRedisStreamCommands/Async contracts and DTOs in ServiceStack.Interfaces, native/rich extensions and a typed JSON facade in ServiceStack.Redis. Existing client interfaces gain no required members. No dependencies added. Core commands target Redis 5+, XAUTOCLAIM 6.2+; unsupported-command/ACL errors propagate. No static global version probing or emulation.
- Implement XADD/XRANGE/XREVRANGE/XLEN/XREAD/XTRIM(MAXLEN)/XDEL, XGROUP CREATE/DESTROY, XREADGROUP/XACK/XPENDING (summary/detail), XCLAIM and cursor-based XAUTOCLAIM. Binary field pairs and string IDs remain lossless. RedisData gains additive IsNull metadata; existing Data/Children shapes are preserved. Redis 6.2 nil claim results retain null IDs/payloads rather than invented IDs; Redis 7+ deleted IDs are exposed separately.
- Typed helpers write one JSON field named json, retain unknown raw fields and distinguish explicit JSON null, missing JSON and deleted payloads. Malformed/duplicate JSON fields fail without automatic acknowledgment. No generic entity-index maintenance or automatic schema migration.
- The initial phase rejected pipelines/transactions; the batching decision below adds five primitive pipeline commands while preserving standalone structured operations and rejecting Streams transactions. Blocking reads require an exclusive dedicated lease; group operations require writers. Caller cancellation is normalized after the transport closes uncertain protocol state, with no added replay or rollback guarantee. Existing retry behavior remains; applications need idempotency for uncertain outcomes. Receive timeouts still apply to blocking reads.
- Seventeen Streams cases and nine cleanup cases pass on Redis 8.10.1, Redis 6.2.24 and Valkey 8.1.11. They cover all 256 payload bytes and binary field names, empty values, large IDs, multi-stream reads, reverse ranges, trimming, nil metadata, explicit pending/ack, claim/continuation recovery after reader disconnect, deleted pending payloads, typed nulls/unknown fields, ACL denial and cancellation of reads confirmed blocked at the server followed by clean pool reuse. The wider Redis run passed 308 cases with five skips (three endpoint fixture cases separately passed in the IPv6/TLS container, two existing configuration skips). All sixteen modern endpoint cases pass in that network fixture. No server-kill durability test, performance benchmark or exactly-once guarantee is claimed.

See [native Streams](../RedisNativeClient.Streams.cs), [extensions](../RedisClientExtensions.Streams.cs), [typed facade](../RedisClientExtensions.TypedStreams.cs), [tests](../../../tests/ServiceStack.Redis.Tests/RedisStreamsTests.cs) and redis/streams.md. Full asynchronous connect/TLS, additional server flags, background consumers, retry/dead-letter automation, MQ adapters and Cluster routing remain outside this increment.

### Streams batching decision: 2026-11-10

The maintainer requested “sounds good, please implement” after the recommendation for primitive command pipelines and a bounded bulk-append helper. Owner: ServiceStack.Redis, canonical REDIS-004. **Implemented in the working tree; not confirmed released.** This accepts XADD/XACK/XDEL/XTRIM/XLEN pipelines and binary/typed append helpers; structured result pipelines, blocking batches, Streams transactions, MQ adapters and automatic consumers remain outside this phase.

- Route the five primitive sync/async commands through existing string/integer deferred pipeline readers. Single-entry typed AddStreamEntry uses XADD and inherits this support. Other command guards remain intact. Existing optional command contracts gain no required members; only their capability documentation changes.
- Add AddStreamEntries/AddStreamEntriesAsync extensions on IRedisClient/IRedisClientAsync, with binary IEnumerable<RedisStreamField[]> and generic JSON overloads. Require an exclusive concrete ServiceStack RedisClient writer lease; no extra leased connections. Use generated IDs, return them in input order on success, and apply optional exact/approximate MAXLEN to each append. Default positive batchSize=100 bounds XADD command count per flush; it is not a byte or whole-result memory cap. Inputs must not be mutated during the operation.
- Enumerate/serialize incrementally, validate every entry in the current batch before sending it, reject nested pipelines/transactions, and open before queue delegates execute. Empty input opens no connection. No new package or shared-interface methods were introduced.
- Stop after a failed batch, drain ordinary server error replies from that already-sent batch, then propagate the first error. Stop immediately on transport/parser/cancellation failures and close uncertain protocol state. Earlier completed batches and some/all entries in an uncertain batch may remain; no partial ID result is returned on failure and no automatic batch replay is attempted. Explicit pipeline replay or application retry with XADD * can create duplicates. Applications own idempotency.
- Complete short gather/socket writes in sync/async send loops; preserve TLS and verbose logging. Async writes/flushes close captured sockets on cancellation, including older APIs. Cover failed sends inside the pipeline cleanup boundary and abort without synchronous QUIT or reading raw bytes from an uncertain/TLS connection. Existing standalone command retry policy is unchanged.

Verification: 24 new batch cases include two TLS-only fixture cases; all pass in an isolated IPv6/TLS Redis 8.10.1 fixture. A broader host run passes 399 cases (eight connection/configuration fixture skips, with fixture coverage recorded separately). Streams/batch/pipeline/transaction suites pass 103 applicable cases separately on Redis 6.2.24 and Valkey 8.1.11. Tests cover binary/empty fields, ordered generated IDs and primitive callbacks, mixed ordinary commands, exact retention, typed nulls, incremental hard batch counts, invalid later batches, mid-operation ACL denial, large payloads with small socket send buffers, dropped replies without replay, cancellation before enumeration/between batches/during a pooled flush, clean client/pool reuse and retained standalone/transaction guards. All four Redis targets and five Interfaces targets build; runtime validation is net10.0. No throughput benchmark, exhaustive failover/durability matrix or exactly-once guarantee is claimed.

Implementation: [primitive command routing](../RedisNativeClient.Streams.cs), [bulk helpers](../RedisClientExtensions.StreamBatches.cs), [tests](../../../tests/ServiceStack.Redis.Tests/RedisStreamBatchTests.cs), and redis/streams.md. Record released availability only after publication.

### Original proposal (preserved; acceptance is limited above)

- Recommendation: Strategic feature; prototype a bounded command set.
- Potential value: High new capability for retained event feeds and recoverable consumers.
- Relative effort: Large; split native operations, typed facade and any MQ adapter.
- Evidence confidence: High that named Streams APIs were not found in the inspected surface; customer demand unvalidated.
- Customer validation: not yet established; select a real application/workaround before acceptance.

### Existing behavior and gap

The library already supports lists, Pub/Sub, raw nested command results, Lua, pipelines and typed POCOs. Those escape hatches can invoke Streams commands, but applications must construct/parse results and manage pending delivery/recovery themselves. Commands/native/rich interfaces inspected here did not expose named XADD/XREADGROUP APIs. RedisMqServer and RedisServerEvents belong to ServiceStack.Server and use different delivery models.

### Proposed outcome and bounded approach

A supported RESP2 Streams command surface and typed consumer-group facade, with explicit acknowledgment, pending entries, reclaiming and retention semantics. Keep a new MQ transport as a later independently scoped integration.

1. Start with stream IDs represented losslessly, binary-safe field pairs, XADD/XRANGE/XREAD and a capability/version matrix. Do not convert IDs to DateTime or floating-point values.
2. Add XGROUP/XREADGROUP/XACK/XPENDING and claim/reclaim methods supported by the selected baseline. Use dedicated leased connections for blocking reads and bounded cancellation.
3. Make acknowledgment explicit after successful handling. Preserve unknown fields, deleted/trimmed entries and pending-entry state rather than mapping every reply to a happy-path message.
4. Add a POCO serialization convention with schema/version guidance. Only after failure recovery is demonstrated consider a Server-owned Streams MQ adapter; a dead-letter policy, delivery attempts and shutdown draining are additional design work.

### Acceptance criteria if selected

- Integration fixtures cover append, replay range, empty reads, consumer restart, pending entries, explicit ack, claim cursor and trimmed/deleted payloads.
- Kill a consumer after a side effect but before ack; redelivery is visible and requires application deduplication. No exactly-once claim.
- Cancellation of a blocking read does not return a socket with unread replies to a general pool. Mixed binary values and missing fields round-trip.
- Validate pinned Redis and Valkey images separately. Gate newer claim/retention options instead of copying the latest command syntax wholesale.

### Alternatives and compatibility

Document Custom/RawCommand recipes first, or use a dedicated broker/client where existing application requirements already justify one. Streams is useful when retained delivery is required; ordinary Pub/Sub remains appropriate for live-only events.

RESP3 is not required for the initial command set. Redis persistence/replication policy still determines durability. Consumer groups do not automatically implement retries, poison handling or business idempotency. Do not silently migrate existing Redis MQ keys or queues.

### Evidence

- [Commands.cs](../Commands.cs)
- [RedisNativeClient.cs](../RedisNativeClient.cs)
- [RedisNativeClient_Utils.cs](../RedisNativeClient_Utils.cs)
- [RedisClient.cs](../RedisClient.cs)
- [CustomCommandTests.cs](../../../tests/ServiceStack.Redis.Tests/CustomCommandTests.cs)
- [CustomCommandTests.Async.cs](../../../tests/ServiceStack.Redis.Tests/CustomCommandTests.Async.cs)
- [RedisPubSubServerTests.cs](../../../tests/ServiceStack.Redis.Tests/RedisPubSubServerTests.cs)
- [redis/custom-commands.md](../../../../../docs.servicestack.net/MyApp/_pages/redis/custom-commands.md)
- [redis/pubsub.md](../../../../../docs.servicestack.net/MyApp/_pages/redis/pubsub.md)

The [Redis XREADGROUP reference](https://redis.io/docs/latest/commands/xreadgroup/) and [Valkey XREADGROUP reference](https://valkey.io/commands/xreadgroup/) describe consumer groups and explicit acknowledgment. Their current option sets differ; establish a tested common baseline and optional extensions. Sources checked 2026-11-10.


## REDIS-006: Optional Microsoft cache and DI integration

Decision: on 2026-11-10 the maintainer requested “please implement support for IDistributedCache” following the optional package recommendation. Owner: ServiceStack.Redis (canonical REDIS-006). **Implemented in the working tree; not confirmed released.** No shared Redis interface changes were added.

Packaging revision: on 2026-11-10 the maintainer requested `#if NET10_0_OR_GREATER` and an MSBuild guard so this feature is available only on .NET 10. This supersedes the initial optional-package implementation: the adapter, options and DI extensions now live in ServiceStack.Redis, with matching .NET 10-only package references. The separate source/test projects and their build/pack entries were removed; the existing Redis test project owns the guarded tests.

### Accepted implementation and compatibility

- RedisDistributedCache, RedisDistributedCacheOptions and RedisDistributedCacheServiceCollectionExtensions are compiled into the net10.0 ServiceStack.Redis assembly under `#if NET10_0_OR_GREATER`. The project references Microsoft.Extensions.Caching.Abstractions 10.0.13 and DependencyInjection.Abstractions 10.0.13 only when TargetFramework is net10.0. Older targets expose no adapter APIs and gain no Microsoft cache/DI dependencies. Existing native binary Lua APIs and exclusive writer leases remain the implementation foundation.
- RedisDistributedCache implements sync/async Get, Set, Refresh and Remove. Data stays byte-for-byte binary, including empty values. Set stores complete expiry metadata atomically in one private hash. Get/Refresh atomically respect the absolute cap and sliding inactivity duration; refresh does not recreate missing data or load payload bytes unnecessarily. No-expiry replacements clear old TTLs. Absolute timestamps are UTC milliseconds with positive values rounded up; relative absolute expiry takes precedence when both absolute options are supplied. A future absolute deadline that passes while waiting is enforced at execution.
- KeyPrefix is nonempty, snapshotted and defaults to ss:cache:v1:. It owns a separate versioned hash namespace and does not inherit manager NamespacePrefix. No migration/interoperability with another provider's stored format is implied. Existing unprefixed ServiceStack keys are left separate.
- AddServiceStackRedisCache uses TryAdd singleton registration and preserves existing IDistributedCache providers. Connection-string/new-manager factory registrations create lazily and own the new manager. The overload resolving an existing DI manager and direct adapter construction keep external ownership unless ownsManager is explicitly true. DI factories dispose newly created managers if adapter construction fails; direct-constructor ownership transfers on successful construction. Disposing the adapter is idempotent.
- Async calls pass tokens to acquisition and commands, normalize caller cancellation from transport wrappers and always release leases. The subsequent REDIS-002 async-setup phase provides cancellable default connection setup; legacy/custom synchronous resolution limits still apply. Uncertain writes retain normal transport semantics; no rollback guarantee. Requires a manager supporting sync/async acquisition and native binary client leases.
- Requires Redis 5+ and EVAL/TIME/HGET/HEXISTS/HMGET/HMSET/PEXPIRE/PERSIST/DEL permission plus connection/auth commands. ACL/script/format errors propagate without non-atomic fallback. The adapter is standalone and does not provide batching or a near-cache engine.
- HybridCache remains a consuming-application dependency. Tested composition uses HybridCache 10.11.0 / Microsoft.Extensions runtime abstractions 10.0.13, with independent DI providers loading the same L2 after publication. Local caches and stampede coordination retain Microsoft's scope; no distributed invalidation/backplane guarantee is introduced.

### Verification

All 34 net10.0 integration cases initially passed separately on isolated Redis 8.10.1, Redis 6.2.24 and Valkey 8.1.11; the relocated tests pass again from the main Redis test project on isolated Redis 8.10.1. Coverage includes every byte and empty/missing values, actual expiry, sliding refresh, hard absolute caps, UTC offsets, replacement to eternal values, large intervals, 60 refresh/replacement races, namespace isolation, writer-only routing, ACL denial, precancellation, cancelled pool waiting, delayed expiration, DI registration/ownership and real HybridCache L2 reuse. ServiceStack.Redis builds and packs all four targets locally; it was not published. Assembly metadata and NuGet dependency inspection confirm the adapter APIs and Microsoft cache/DI dependencies exist only in net10.0. The earlier multi-target optional package was superseded before publication. No comparative benchmark or net472/net6/net8 runtime matrix is claimed.

Implementation and usage: [adapter](../RedisDistributedCache.cs), [options](../RedisDistributedCacheOptions.cs), [DI extensions](../RedisDistributedCacheServiceCollectionExtensions.cs), [tests](../../../tests/ServiceStack.Redis.Tests/RedisDistributedCacheTests.cs), [inventory](../FEATURES.md), and redis/distributed-cache.md. Record a confirmed released version only after publication.

### Original proposal (preserved)

- Recommendation: Next candidate after an integration spike.
- Potential value: Medium–high for applications standardizing on IDistributedCache.
- Relative effort: Small–medium for registration/raw cache; sliding expiration needs careful semantics.
- Evidence confidence: High ICacheClient exists; no IDistributedCache/HybridCache implementation located in searched client/Extensions code.
- Customer validation: not yet established; select a real application/workaround before acceptance.

### Existing behavior and gap

ServiceStack has ICacheClient/ICacheClientAsync and manager-based cache wrappers. ASP.NET Core components consuming IDistributedCache cannot use those interfaces directly. Targeted searches did not locate a first-party adapter in Redis or ServiceStack.Extensions. This does not exclude third-party adapters or applications already using another Redis cache provider.

### Proposed outcome and bounded approach

An optional package registering existing managers/cache clients and implementing binary IDistributedCache with absolute/sliding expiry semantics. Demonstrate composition with the host's existing hybrid cache rather than adding another near-cache engine.

1. Start with DI lifetime/disposal ownership and the raw byte[] contract. Do not persist IDistributedCache payloads through an accidental JSON envelope.
2. Define an isolated key namespace and versioned metadata layout if sliding/absolute-expiry bookkeeping needs extra fields. Refresh must not extend an absolute deadline.
3. Use atomic scripts/commands for read-refresh/update races where required, with ACL requirements documented. Reuse REDIS-001 primitives if appropriate; GETEX alone cannot remember a sliding interval plus absolute cap.
4. Provide an opt-in registration method that does not replace an already configured IDistributedCache unexpectedly. Verify optional host hybrid-cache composition; do not promise cross-node stampede suppression or distributed invalidation that the host does not supply.

### Acceptance criteria if selected

- Byte arrays, empty values, misses, remove and refresh match the Microsoft interface contract in sync/async paths.
- Test absolute, relative and sliding expiration combinations, concurrent refresh/replacement and cancellation.
- Existing ServiceStack keys are not reinterpreted as adapter metadata; pooled connections are returned and externally owned managers are not disposed by the adapter.
- Demonstrate one framework consumer and a hybrid-cache L2 example against pinned package versions.

### Alternatives and compatibility

Use Microsoft's existing Redis cache provider or a small application adapter. The new package is worthwhile if sharing ServiceStack's manager configuration/operations saves meaningful duplication. Registration helpers alone may be the useful first deliverable.

Keep Microsoft.Extensions dependencies outside the base package where feasible. Consumers must choose byte format and namespace deliberately. No automatic migration of existing cache data or forced near-cache consistency guarantee.

### Evidence

- [RedisClient.ICacheClient.cs](../RedisClient.ICacheClient.cs)
- [RedisClient.Async.cs](../RedisClient.Async.cs)
- [RedisClientManagerCacheClient.cs](../RedisClientManagerCacheClient.cs)
- [BasicRedisClientManager.ICacheClient.cs](../BasicRedisClientManager.ICacheClient.cs)
- [CacheClientTests.cs](../../../tests/ServiceStack.Redis.Tests/CacheClientTests.cs)
- [CacheClientTests.Async.cs](../../../tests/ServiceStack.Redis.Tests/CacheClientTests.Async.cs)
- [caching.md](../../../../../docs.servicestack.net/MyApp/_pages/caching.md)

[Microsoft's distributed caching documentation](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-10.0) identifies IDistributedCache as the host integration contract and lists an existing Redis provider. Evaluate interoperability and alternatives before creating a competing integration. Sources checked 2026-11-10.

