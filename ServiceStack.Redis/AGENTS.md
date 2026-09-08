# CONTEXT: ServiceStack.Redis

## Purpose

`ServiceStack.Redis` is an ultra-fast, rich C# client for Redis. It provides comprehensive, thread-safe access to Redis data structures, caching, pub/sub messaging, transactions, distributed locks, and Lua scripting.

Key capabilities include:
- **Rich Client Surface (`IRedisClient`)**: Direct APIs for Strings, Hashes, Lists, Sets, Sorted Sets, Bitmaps, and HyperLogLogs.
- **Typed POCO Client (`IRedisTypedClient<T>`)**: Transparently store and query .NET POCOs as distinct Redis entities with auto-generated primary keys and indexes.
- **Distributed Locking (`RedisLock`)**: Robust, safe distributed mutual exclusion with token ownership validation.
- **Pub/Sub Messaging & Server Events**: Real-time event publishing, subscription listeners (`RedisPubSubServer`, `RedisSubscription`), and distributed SSE backplanes.
- **Connection Pooling**: `PooledRedisClientManager`, `RedisManagerPool`, and `BasicRedisClientManager` with automatic failover, read-replica routing, and Redis 6+ ACL support.

**Target frameworks**: `net472;net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack.Interfaces     ServiceStack.Common     ServiceStack.Text
         │                          │                       │
         └──────────────────────────┼───────────────────────┘
                                    ▼
                          ServiceStack.Redis
                                    │
           ┌────────────────────────┴────────────────────────┐
           ▼                                                 ▼
Distributed Caching & Sessions                  Enterprise Messaging & Real-Time
 (ICacheClient / AppHost Cache)                (RedisMqServer, RedisServerEvents)
```

- **Depends on**: `ServiceStack.Interfaces`, `ServiceStack.Common`, `ServiceStack.Text`.
- **Depended on by**:
  - `ServiceStack.Server` (uses it for `RedisMqServer`, `RedisServerEvents`, `RedisRequestLogger`).
  - Standalone .NET applications needing a high-performance Redis client.
- **Role**: Provides the primary distributed caching, session storage, and message queuing backbone for high-scale ServiceStack deployments.

---

## Key Functionality

### Primary Types
| Class / Interface | Role |
|---|---|
| `RedisClient` | Primary client implementing `IRedisClient` and `IRedisClientAsync`. |
| `RedisNativeClient` | Low-level RESP wire protocol client communicating over raw TCP sockets. |
| `PooledRedisClientManager` | Thread-safe connection pool for master/read-replica architectures. |
| `RedisManagerPool` | Lightweight, high-throughput connection pool with fixed pool sizes. |
| `RedisLock` | Distributed lock implementation ensuring mutual exclusion across servers. |
| `RedisPubSubServer` | Background pub/sub listener dispatching received Redis messages to managed handlers. |
| `IRedisTypedClient<T>` | Entity-focused client managing POCO persistence, auto-IDs, and secondary index sets. |

---

## Architecture & Design Patterns

### RESP Protocol & Socket Management
`RedisNativeClient` reads and writes the Redis Serialization Protocol (RESP) directly to buffered socket streams, minimizing heap allocations and encoding overhead.

### Master / Replica Separation
`BasicRedisClientManager` and `PooledRedisClientManager` route mutating operations (`GetClient()`, `GetCacheClient()`) to master nodes while distributing read-only operations (`GetReadOnlyClient()`, `GetReadOnlyCacheClient()`) across replica nodes.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Adhere to these principles:

### 1. Distributed Lock Mutual Exclusion Violation (`RedisLock`)
- **Risk**: Deleting lock keys unconditionally upon disposal allows a slow client (whose lock timed out) to delete a second client's newly acquired lock, breaking distributed mutual exclusion.
- **Rule**: `RedisLock.Dispose` and `DisposeAsync` must execute a check-and-delete transaction (via `WATCH`/`MULTI`) ensuring the lock is only deleted if the Redis key still contains the instance's unique acquisition token.

### 2. Elimination of Insecure `BinaryFormatter`
- Legacy `ObjectSerializer` and `OptimizedObjectSerializer` are deprecated due to `BinaryFormatter` vulnerabilities (CWE-502). All primary Redis serialization must use safe `ServiceStack.Text` JSON or JSV serializers.

### 3. Pub/Sub Internal Control Command Leaks
- In `RedisPubSubServer`, control messages (`CTRL:...`) must be verified at every byte index (`msg[0] == 'C' && msg[1] == 'T' && msg[2] == 'R' && msg[3] == 'L'`) to prevent internal heartbeat pulses from leaking into application handlers.

### 4. Redis 6+ ACL Identity & Hash Code Collision
- `RedisEndpoint.Equals` and `GetHashCode` must incorporate the `Username` property, preventing connection pools from reusing connections across distinct ACL user identities.

### 5. Mutating Cache Operations on Read Replicas
- In `BasicRedisClientManager.ICacheClient`, mutating operations like `Remove(key)` must route to `GetCacheClient()` (master), never `GetReadOnlyCacheClient()`.

---

## Common Modification Scenarios

1. **Configuring Redis in ServiceStack**
   ```csharp
   container.Register<IRedisClientsManager>(c => 
       new RedisManagerPool("redis://username:password@localhost:6379"));
   container.Register(c => c.Resolve<IRedisClientsManager>().GetCacheClient());
   ```

2. **Acquiring Distributed Locks Safely**
   ```csharp
   using var redis = redisManager.GetClient();
   using (redis.AcquireLock("my-lock-key", TimeSpan.FromSeconds(10)))
   {
       // Critical section protected by check-and-delete validation
   }
   ```

3. **Subscribing to Redis Pub/Sub Channels**
   - Use `RedisPubSubServer` or `redis.CreateSubscription()`.
   - Ensure message loops catch transient socket disconnects and reconnect gracefully.
