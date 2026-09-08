# CONTEXT: ServiceStack.Caching.Memcached

## Purpose

Provides a Memcached-backed implementation of ServiceStack's `ICacheClient` interface, enabling ServiceStack applications to use Memcached as a distributed caching layer. This project acts as an adapter between ServiceStack's caching abstraction and the `EnyimMemcachedCore` client library.

The library handles:
- Translating ServiceStack cache operations into Enyim Memcached client calls
- Wrapping cached values with type metadata to support typed deserialization
- Bridging the Enyim logging interface with ServiceStack's logging infrastructure
- Parsing and validating Memcached host connection strings (including IPv6)

## Role in ServiceStack Ecosystem

### Position
`ServiceStack.Caching.Memcached` is one of several `ICacheClient` implementations in the ServiceStack ecosystem:

| Package | Backing Store |
|---|---|
| `ServiceStack.Caching.Memcached` | Memcached (this project) |
| `ServiceStack.Redis` | Redis |
| `ServiceStack.Server` | OrmLite (RDBMS) |
| `ServiceStack` | In-memory (`MemoryCacheClient`) |
| `ServiceStack.Azure` | Azure Cache |

### Dependencies
- **`ServiceStack`** (core): Consumes `ICacheClient`, `ICacheClientExtended`, `IRemoveByPattern`, `ILog`, `NullDebugLogger`, `JsonSerializer`, and related types.
- **`EnyimMemcachedCore`**: The underlying Memcached client library (`EnyimMemcached.IMemcachedClient`). All actual network communication with Memcached is delegated here.

### Consumers
Any ServiceStack application or plugin that registers `MemcachedClientCache` as its `ICacheClient` via the IoC container (e.g., `container.Register<ICacheClient>(new MemcachedClientCache(hosts))`).

### Target Frameworks
`net472`, `net6.0`, `net8.0`, `net10.0`

## Key Functionality

### `MemcachedClientCache`
The central class. Implements `ICacheClient` and `ICacheClientExtended`. Wraps an `EnyimMemcached.IMemcachedClient` instance and adapts all cache operations to it.

**Construction**: Accepts either a pre-built `IMemcachedClient` or a list of host strings in `"host:port"` format (including IPv6 with bracket notation, e.g. `[::1]:11211`). Host parsing validates that each entry has a resolvable host and a port in the valid range (1–65535).

**Operation execution pattern**: All cache operations are routed through an internal `Execute(Func<T>)` wrapper that:
- Times how long the operation takes
- Logs slow or failed operations via ServiceStack's `ILog`
- Swallows and logs exceptions rather than propagating them to callers (cache failures should be non-fatal)

**Key methods and their behaviours**:

| Method | Notes |
|---|---|
| `Get<T>(key)` | Retrieves and unwraps a `MemcachedValueWrapper`; deserializes JSON if `T` is not a primitive string |
| `Get(key, out ulong ucas)` | Returns `.Value` of the wrapper, **not** the wrapper itself |
| `Set<T>(key, value, [expiry])` | Wraps value in `MemcachedValueWrapper` before storing |
| `GetAll<T>(keys)` | Accepts null/empty collections gracefully; returns `Dictionary<string, T>` |
| `SetAll<T>(values)` | Guards against null input dictionary |
| `Remove(key)` / `RemoveAll(keys)` | `RemoveAll` guards against null collections |
| `Increment` / `Decrement` | Delegates to Enyim's atomic counter support |
| `FlushAll()` | Issues a full cache flush |
| `GetTimeToLive(key)` | Returns remaining TTL if the backing store exposes it |

### `MemcachedValueWrapper`
A serialization envelope stored alongside every cached value. Carries:
- `Type` — the fully-qualified CLR type name of the wrapped value
- `Value` — the actual cached object

**Purpose**: Enables typed round-tripping. When a `Get<T>` is issued, the wrapper's `Type` field drives JSON deserialization back to the correct type even when Memcached stores everything as raw bytes/strings.

**Nested wrapping prevention**: The constructor detects if the incoming value is already a `MemcachedValueWrapper` and unwraps it before re-wrapping, preventing double-envelope bugs.

**Deserialization**: Uses `JsonSerializer.DeserializeFromString`. Falls back gracefully (returns `null` or default) on corrupt or unparseable JSON payloads rather than throwing.

### `EnyimLoggerWarpper` / `EnyimLoggerWrapper`
Adapts the `EnyimMemcached.ILog` interface to ServiceStack's `ILog`. Both spellings of the class name exist for backward compatibility (`Warpper` is the historical typo; `Wrapper` is the corrected alias).

Defaults to `NullDebugLogger` when constructed with a `null` ServiceStack logger, preventing null-reference exceptions in environments where logging is not configured.

## Architecture & Design Patterns

### Adapter Pattern
The entire project is an adapter. `MemcachedClientCache` adapts `IMemcachedClient → ICacheClient`. `EnyimLoggerWarpper` adapts `ILog (Enyim) → ILog (ServiceStack)`.

### Value Object Envelope
`MemcachedValueWrapper` acts as a typed envelope around cached values. Because Memcached is type-unaware, all writes go through this envelope so reads can recover the original CLR type without relying on the generic type parameter alone.

### Fail-Silent Execution
Cache operations are best-effort. The `Execute(...)` helper catches all exceptions, logs them, and returns a default/null value. This prevents cache failures from cascading into application failures.

### Null-Guard Bulk Operations
All collection-based operations (`GetAll`, `SetAll`, `RemoveAll`) perform an early null/empty check and return immediately, matching the contract expected by the `ICacheClient` interface.

## Security & Reliability Considerations

> These reflect known issues and their required fixes. Ensure all are addressed when modifying this project.

### Data Leakage — CAS `Get` Returns Wrapper Object
**Risk**: `Get(string key, out ulong ucas)` previously returned the raw `MemcachedValueWrapper` internal object instead of its `.Value`. Callers would receive an opaque wrapper rather than their data, and the wrapper's type metadata could leak internal implementation details.  
**Fix**: Always return `result.Result.Value` (the unwrapped payload) from CAS-flavoured get operations.

### Denial of Service — Host String Parsing
**Risk**: Passing `null` as the hosts list, or hosts with malformed port strings (e.g. `"host:notaport"`), previously caused unhandled exceptions during construction, making the application fail to start.  
**Fix**:
- Guard against `null` hosts collection at constructor entry.
- Parse ports with `int.TryParse` (not `int.Parse`) and validate `1 ≤ port ≤ 65535`.
- Support IPv6 bracket notation: `[::1]:11211` — split on the last `:` only, not the first.

### InvalidCastException — CAS Value Retrieval
**Risk**: Direct casts (`(MemcachedValueWrapper)result`) throw `InvalidCastException` when the cached value is not of the expected type (e.g. raw strings, values written by other clients).  
**Fix**: Use pattern matching (`if (result is MemcachedValueWrapper wrapper)`) with a safe fallback for unexpected types.

### Nested Wrapper Creation
**Risk**: Storing a value that is already a `MemcachedValueWrapper` (e.g. due to a re-set on a just-fetched value) creates a double-wrapped payload. On retrieval, the outer deserialization returns a `MemcachedValueWrapper` object rather than the actual data.  
**Fix**: `MemcachedValueWrapper` constructor must detect and unwrap an already-wrapped input before creating the envelope.

### Null Collection Crashes
**Risk**: Passing `null` to `GetAll`, `SetAll`, or `RemoveAll` causes `NullReferenceException` inside iteration.  
**Fix**: Early return on null or empty input, consistent with `ICacheClient` expectations.

### Deserialization Errors on Corrupt Payloads
**Risk**: A corrupt or truncated JSON value stored in Memcached causes an unhandled exception during `Get<T>`, crashing the request.  
**Fix**: Wrap deserialization in try/catch; log the error and return `default(T)` so the caller can treat it as a cache miss.

### Logger Null Reference
**Risk**: `EnyimLoggerWarpper` constructed with a `null` ServiceStack `ILog` throws `NullReferenceException` on the first log call.  
**Fix**: Default to `new NullDebugLogger()` when the provided logger is `null`.

## Common Modification Scenarios

1. **Adding a new `ICacheClient` method**: Implement in `MemcachedClientCache`, wrap the Enyim call inside `Execute(...)`, guard null inputs, unwrap the `MemcachedValueWrapper` on reads.

2. **Updating the Enyim dependency**: Check for API changes in `IMemcachedClient` (e.g. method signatures, async variants). The `Execute` wrapper and host-parsing logic may need updating.

3. **Supporting async cache operations** (`ICacheClientAsync`): Would require adding async `Execute` overloads and mapping `IMemcachedClient` async methods. Currently all operations are synchronous.

4. **Changing serialization**: Serialization is centralised in `MemcachedValueWrapper`. Switching from `JsonSerializer` to another format only requires changes there, but must maintain backward compatibility with existing cached payloads.

5. **Adding new framework targets**: Update `<TargetFrameworks>` in the `.csproj`. Verify that the EnyimMemcachedCore NuGet package supports the new TFM; conditional `#if` blocks may be needed for framework-specific APIs.

6. **Fixing host-string parsing edge cases**: All host parsing lives in the `MemcachedClientCache` constructor. IPv6, Unix sockets, or DNS-only entries each need individual handling and test coverage.

7. **Adding instrumentation/metrics**: The `Execute(...)` helper is the single chokepoint for all cache calls and is the right place to add distributed tracing spans or metrics counters.
