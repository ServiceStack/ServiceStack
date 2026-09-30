# FUTURE: ServiceStack.Redis Roadmap Ideas

Potential features and improvements that would add value to ServiceStack.Redis as a type-safe, high-performance Redis client for .NET.
Each idea keeps the client's existing design principles:

- **Typed first, RESP-friendly**: rich typed APIs (`IRedisClient`, `IRedisTypedClient<T>`) over a thin, predictable wire protocol, with `Custom()`/`RawCommand()` as the escape hatch.
- **Symmetric sync and async APIs** (`IRedisClient` / `IRedisClientAsync`).
- **Drop-in manager abstractions**: every feature works through `IRedisClientsManager` so apps can swap `RedisManagerPool`, `PooledRedisClientManager`, Sentinel, etc. without code changes.
- **Server compatible**: works with Redis, Valkey, and managed services, with graceful fallbacks (or clear `NotSupportedException`s) on older servers.

Effort: **S** = days, **M** = 1-2 weeks, **L** = multi-week.

The current command surface stops at roughly Redis 3.2 (GEO). Section 1 covers the most valuable gaps since then.

---

## 1. Modern Redis Command Coverage

### 1.1 Redis Streams with a typed consumer-group API (L)
Streams are the biggest missing data type. They give durable, replayable, consumer-group based messaging, which is a better fit than Lists or Pub/Sub for most queueing needs.
```csharp
var events = redis.As<OrderPlaced>().Streams["orders"];
var id = events.Add(new OrderPlaced { OrderId = 1 }, maxLen: 100_000, approximate: true); // XADD ... MAXLEN ~

events.CreateGroup("billing", startAt: StreamId.Beginning, mkStream: true);   // XGROUP CREATE
await foreach (var msg in events.ReadGroupAsync("billing", consumer: "worker-1", block: TimeSpan.FromSeconds(5)))
{
    await Handle(msg.Value);
    await events.AckAsync("billing", msg.Id);                                  // XACK
}

var stuck = events.AutoClaim("billing", "worker-2", minIdle: TimeSpan.FromMinutes(1)); // XAUTOCLAIM
```
- Native: `XADD`, `XRANGE`/`XREVRANGE`, `XREAD`, `XREADGROUP`, `XACK`, `XPENDING`, `XCLAIM`/`XAUTOCLAIM`, `XTRIM`, `XINFO`, `XDEL`, `XLEN`.
- A `RedisStreamMqServer` that implements `IMessageService` would give ServiceStack MQ at-least-once delivery, dead-lettering via `XPENDING` delivery counts, and replay.

### 1.2 Missing string/key commands (S)
| Command | API | Value |
|---|---|---|
| `UNLINK` | `RemoveEntryAsync(..., lazy: true)` | Frees large keys without blocking. Use it in `RemoveByPattern`/`DeleteAll<T>` when supported |
| `GETEX` / `GETDEL` | `GetAndExpire(key, ttl)`, `GetAndRemove(key)` | Atomic sliding-expiration caches and one-time tokens |
| `SET ... GET`, `KEEPTTL`, `PXAT` | `SetValue(key, v, keepTtl: true)` | Update a value without resetting its TTL, and set absolute expirations atomically |
| `COPY` | `CopyKey(src, dst, replace: true)` | Server-side key cloning |
| `EXPIRETIME` / `PEXPIRETIME` | `GetExpireAt(key)` | Absolute expiry lookup |
| `OBJECT ENCODING/FREQ`, `MEMORY USAGE` | `GetMemoryUsage(key)` | Diagnostics |
| `WAIT` / `WAITAOF` | `WaitForReplicas(n, timeout)` | Durability guarantees after critical writes |

### 1.3 Collections (S/M)
- Lists: `LMOVE`/`BLMOVE` (replaces the deprecated `RPOPLPUSH`), `LPOS`, `LMPOP`/`BLMPOP`.
- Sets: `SMISMEMBER`, `SINTERCARD`.
- Sorted sets: `ZADD GT|LT|CH`, `ZRANGE ... BYSCORE|BYLEX|REV LIMIT` (the unified 6.2 form), `ZRANGESTORE`, `ZMPOP`/`BZMPOP`, `ZRANDMEMBER`, `ZMSCORE`, `ZDIFF`/`ZINTER`/`ZUNION` (non-store variants).
- Hashes: `HRANDFIELD`, plus **per-field expiration** (`HEXPIRE`, `HPEXPIRE`, `HTTL`, `HPERSIST`, Redis 7.4+/Valkey 9). This enables typed "hash of sessions" or "hash of rate-limit windows" patterns:
```csharp
redis.As<UserSession>().GetHash<string>("sessions").SetEntry(sessionId, session, expireIn: TimeSpan.FromMinutes(20));
```

### 1.4 Redis Functions (M)
`FUNCTION LOAD` / `FCALL` / `FCALL_RO` supersede `EVAL`/`EVALSHA` script caching. They're persisted and replicated, and read-only functions can be routed to replicas.
```csharp
redis.LoadFunctionLibrary(libSource, replace: true);
var n = redis.CallFunction<long>("mylib", "incr_if_below", keys: ["counter"], args: ["100"]);
```

### 1.5 Sharded Pub/Sub (S)
`SSUBSCRIBE`/`SPUBLISH` scale Pub/Sub horizontally in Cluster mode. They'd be exposed through the existing `IRedisSubscription` and `RedisPubSubServer` (`ShardChannels = [...]`).

---

## 2. Protocol & Connectivity

### 2.1 RESP3 via `HELLO` (M)
- `HELLO 3 AUTH user pass SETNAME name` replaces the separate `AUTH` + `CLIENT SETNAME` round trips on connect.
- RESP3 gives typed replies (maps, sets, doubles, booleans, big numbers, verbatim strings, attributes). That removes ad-hoc parsing like `ZSCORE` string→double and `HGETALL` pairwise flattening.
- Push messages let Pub/Sub and client-side cache invalidations share a normal connection.
- Negotiate automatically, with `RedisConfig.Protocol = RedisProtocol.Resp2` to opt out.

### 2.2 Client-side caching (`CLIENT TRACKING`) (L)
A near-cache that the server keeps coherent through invalidation pushes. It's a large read-latency win for hot, mostly-read keys (config, feature flags, sessions).
```csharp
container.Register<IRedisClientsManager>(c => new RedisManagerPool(connStr) {
    ClientSideCache = new RedisClientSideCacheOptions { MaxEntries = 10_000, Prefixes = ["config:", "flags:"] }
});
```
- Uses `CLIENT TRACKING ON BCAST PREFIX ...` (RESP3), or a dedicated `REDIRECT` connection on RESP2.
- Plugs into `ICacheClient.Get<T>` transparently. Entries are also bounded by TTL, and the cache is dropped on reconnect.

### 2.3 Redis Cluster support (L)
Currently only `ShardedRedisClientManager` (client-side consistent hashing) and Sentinel are supported.
- `RedisClusterManager` that discovers topology through `CLUSTER SHARDS`/`CLUSTER SLOTS`, routes by CRC16 hash slot, follows `MOVED`/`ASK` redirects, and refreshes topology on failover.
- Hash-tag helpers for typed keys (`UrnKey<T>` → `{Order}:urn:order:1`) so typed-client multi-key operations (`GetByIds`, `StoreAll`, related entities) stay in a single slot.
- Cross-slot `MGET`/`DEL` automatically split per node and run in parallel.

### 2.4 IPv6 / dual-stack sockets (S)
`Connect()` always creates an `AddressFamily.InterNetwork` socket, so IPv6-only hosts (some Kubernetes, Fly.io 6PN and cloud VPC setups) and literal `[::1]:6379` endpoints can't connect.
- Use `new Socket(SocketType.Stream, ProtocolType.Tcp)` (dual-mode) and resolve DNS explicitly, trying each address within `ConnectTimeout`.
- Parse bracketed IPv6 literals in `ToRedisEndpoint()`. Today `SplitOnLast(':')` misreads them.

### 2.5 Multiplexed connection mode (L)
An opt-in `RedisMultiplexedManager` that pipelines concurrent async commands from many callers over a few long-lived connections, with automatic pipelining and reply correlation by order. For high-concurrency async workloads it avoids pool exhaustion and cuts per-command socket syscalls, while `IRedisClient` usage stays unchanged. Blocking commands, transactions, and `WATCH` would transparently lease a dedicated connection.

### 2.6 Standard connection-string compatibility (S)
Support `rediss://` (TLS) and the de-facto `redis://user:pass@host:port/db` form: the path segment as the DB, and `user:pass` as the ACL username (see the review note on `user:pass@` currently mapping to `Client`). Also add `?protocol=3`, `?sni=`, and `?clientCert=` for mTLS.

### 2.7 TLS hardening options (S)
- Per-endpoint `CertificateValidationCallback` and `SslClientAuthenticationOptions` (SNI host, client certificates for mTLS, cipher policy) instead of only the global `RedisConfig` callbacks.
- `checkCertificateRevocation` is only enabled when `SslProtocols` is set. Make it consistent and configurable.

---

## 3. Typed Client (`IRedisTypedClient<T>`)

### 3.1 Declarative secondary indexes (M)
The typed client stores entities and an `ids:{Type}` set, but querying by anything other than Id needs hand-rolled sets. Attributes could maintain indexes atomically within the same `MULTI` as `Store()`:
```csharp
public class Customer
{
    public long Id { get; set; }
    [RedisIndex] public string Email { get; set; }                 // unique -> hash  idx:Customer:Email
    [RedisIndex] public string Country { get; set; }               // non-unique -> set idx:Customer:Country:{value}
    [RedisSortedIndex] public DateTime CreatedDate { get; set; }   // sorted set by score
}

var c = redis.As<Customer>().GetByIndex(x => x.Email, "a@b.com");
var uk = redis.As<Customer>().GetAllByIndex(x => x.Country, "UK");
var recent = redis.As<Customer>().GetRangeByIndex(x => x.CreatedDate, from: DateTime.UtcNow.AddDays(-7));
```
`DeleteById`/`DeleteAll` would clean up indexes, and updates would remove stale index entries.

### 3.2 Optimistic concurrency (S)
A `[RowVersion]`-style property checked atomically with a small Lua script (or `WATCH`) on `Store()`, throwing an `OptimisticConcurrencyException` on conflict. This mirrors OrmLite's `RowVersion` support.

### 3.3 Per-type and per-entity expiration (S)
```csharp
[RedisExpireIn(Minutes = 30)] public class CartSession { ... }
redis.As<CartSession>().Store(cart, expireIn: TimeSpan.FromMinutes(5));
```
This would also make the `ids:{Type}` set self-cleaning, via a sorted set keyed by expiry or lazy pruning on `GetAll()`.

### 3.4 Pluggable serializers & compression (M)
- An `IRedisSerializer` abstraction per client or per type, with ServiceStack.Text JSON as the default. Options would include `System.Text.Json` (with source-generated contexts for Native AOT), MessagePack, and protobuf-net.
- Opt-in compression above a size threshold (Brotli/LZ4/Zstd) with a header marker, so mixed compressed and uncompressed values stay readable.
- This also gives a safe replacement for the obsolete `BinaryFormatter`-based `ObjectSerializer`/`SerializingRedisClient` queue helpers.

### 3.5 Typed Pub/Sub (S)
```csharp
using var sub = redis.As<OrderPlaced>().Subscribe("orders", msg => Handle(msg));
redis.As<OrderPlaced>().Publish("orders", new OrderPlaced { ... });
```

### 3.6 Native AOT / trimming friendliness (M)
Audit reflection use (`IdUtils`, `CreateUrn`, `ModelConfig`) and add `[DynamicallyAccessedMembers]` annotations. Optionally add a source generator that emits Id accessors and URN builders for types used with `As<T>()`.

### 3.7 RedisJSON & RediSearch / Valkey-Search integration (L)
Redis 8 bundles JSON, Query Engine, and vector sets, and Valkey has `valkey-search`/`valkey-json` modules. A typed layer could:
- Store `T` as JSON documents (`JSON.SET`/`JSON.GET` with path updates: `redis.As<Order>().Json.Set(id, x => x.Status, "Shipped")`).
- Create indexes from attributes (`FT.CREATE ... ON JSON`) and translate LINQ-like predicates to query syntax: `redis.As<Product>().Search(x => x.Price < 100 && x.Tags.Contains("sale")).OrderBy(x => x.Price).Take(20)`.
- Vector similarity search (`FT.SEARCH ... KNN` / `VADD`/`VSIM`) for AI/RAG workloads, pairing with ServiceStack AI features.

---

## 4. Higher-Level Distributed Primitives

### 4.1 Next-generation `RedisLock` (S)
- Acquire with `SET key token NX PX ttl`, so crashed holders expire automatically and no `WATCH`/`MULTI` retry dance is needed. Release with an atomic compare-and-delete Lua script: 1 round trip instead of 5 (`WATCH`/`GET`/`MULTI`/`DEL`/`EXEC`).
- Use a unique random token (not the expiry timestamp, which can collide for two holders that acquire in the same millisecond with the same timeout).
- `ExtendAsync(ttl)` for long-running work, and a `LockLost` cancellation token raised when an extension fails.
- Keep the current value format readable during a transition release for mixed-version deployments.

### 4.2 Rate limiting (S)
Fixed-window, sliding-window (sorted set), and token-bucket (Lua) limiters behind a common `IRateLimiter` that integrates with ASP.NET Core's `System.Threading.RateLimiting` and ServiceStack request filters.

### 4.3 Distributed semaphore, leader election, and idempotency keys (S/M)
- `redis.AcquireSemaphore("reports", maxCount: 5, ttl)` using a sorted set of holders.
- `LeaderElection` with a lease key, renewal, and `OnElected`/`OnDemoted` callbacks, useful for single-instance background jobs.
- `redis.TryBeginIdempotent(key, ttl)` / `CompleteIdempotent(key, response)` to deduplicate retried API requests.

### 4.4 Keyspace notifications helper (S)
Typed subscription to `__keyevent@{db}__:expired` and similar channels (with automatic `CONFIG SET notify-keyspace-events` when permitted), e.g. to react to session expiry.

---

## 5. Observability & Operations

### 5.1 OpenTelemetry tracing & metrics (M)
`RedisDiagnostics` currently publishes `DiagnosticListener` events. Add:
- An `ActivitySource` (`ServiceStack.Redis`) emitting spans with semantic conventions (`db.system=redis`, `db.operation.name`, `server.address`, `db.namespace` for the DB index). The statement is off by default so values aren't leaked.
- A `Meter` with command duration histograms, pool in-use/idle gauges, pool wait time, retries, failovers, and reconnects. That replaces polling `RedisStats`/`GetStats()`.

### 5.2 ASP.NET Core health checks (S)
`services.AddHealthChecks().AddServiceStackRedis()`, which checks `PING`, replication role, and pool saturation.

### 5.3 Pool improvements (M)
- A min-idle / warm-up option so the first requests after start or failover don't pay connection plus TLS handshake costs.
- A background idle-connection validator instead of validating on checkout (`IdleTimeOutSecs` + `Poll`).
- `GetClientAsync` that waits without polling (`SemaphoreSlim`-based) for `PooledRedisClientManager`.
- Expose pool wait-time statistics.

### 5.4 Typed ACL & admin APIs (S)
`ACL SETUSER/GETUSER/LIST/WHOAMI/LOG`, `CLIENT NO-EVICT`, `CLIENT NO-TOUCH`, `LATENCY`, and `SLOWLOG` as typed results.

---

## 6. Performance Opportunities

- **Allocation-free reply parsing** (M): `ReadLine()` builds a `string` for every RESP header (`$123`, `*10`, `:1`), and bulk lengths are parsed with `int.TryParse` on substrings. Parsing prefixes and lengths directly from the `BufferedReader` byte buffer (`Utf8Parser`/`Span<byte>`) would remove one string allocation per reply element, which is significant for `MGET`, `HGETALL`, and `LRANGE`.
- **`ArrayPool`/`IBufferWriter` for command building** (S): `GetCmdBytes` allocates small arrays for every argument header. Writing headers directly into the pooled send buffer avoids them.
- **Reply-to-POCO streaming** (M): deserialize large `GetAll<T>`/`MGET` results directly from pooled buffers instead of `byte[]` → `string` → `T`.
- **Lua scripts for multi-round-trip helpers** (S): `RedisLock.Dispose`, `DistributedLock.Unlock`, and `Add/Set/Replace(key, value, DateTime expiresAt)` currently need 2-5 round trips and aren't atomic. The `DateTime` overloads can use `SET ... PXAT` or a computed `PX`.
- **Replace MD5 in `ConsistentHash`** (S): use a fast non-cryptographic hash (xxHash3). Keep MD5 as an opt-in for existing shard layouts, since changing the hash re-shards keys.

---

## 7. Developer Experience

- **`IServiceCollection` extensions** (S): `services.AddRedis(connStr, o => { ... })` registers `IRedisClientsManager`, `IRedisClientsManagerAsync`, `ICacheClient`, and `ICacheClientAsync`.
- **`IDistributedCache` adapter** (S) for ASP.NET Core components that only know `Microsoft.Extensions.Caching.Distributed`.
- **`HybridCache` L2 backend** (S) for .NET 9+ `HybridCache`.
- **Testcontainers-based test fixtures** (S) so the test suite starts its own Redis/Valkey (standalone, Sentinel, Cluster) instead of relying on hard-coded hosts such as `10.0.0.121`.
