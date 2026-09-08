# CONTEXT: ServiceStack.Server

## Purpose

`ServiceStack.Server` provides enterprise server-side integrations and advanced features that leverage **OrmLite** (RDBMS) and **Redis**. It is the home for ServiceStack's most powerful data-driven features:

- **AutoQuery & AutoCrud**: Instant, high-performance query and CRUD services automatically mapped to OrmLite data models.
- **RDBMS-backed Providers**: `OrmLiteAuthRepository` (user identity persistence), `OrmLiteCacheClient` (database caching), and `OrmLiteAppSettings` (database configuration).
- **Redis Server Integrations**: `RedisMqServer` (distributed message queue), `RedisServerEvents` (distributed Server-Sent Events fan-out), and `RedisRequestLogger`.
- **Database Logging & Task Management**: `DbRequestLogger` (request analytics and auditing) and `DbJobs` (RDBMS-backed background task queues).

**Target frameworks**: `net472;net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack (Core)    ServiceStack.OrmLite    ServiceStack.Redis
        │                       │                      │
        └───────────────────────┼──────────────────────┘
                                ▼
                       ServiceStack.Server
                                │
        ┌───────────────────────┴──────────────────────┐
        ▼                                              ▼
Full-Stack Web Applications                 Microservices & Gateways
(AutoQuery, Locode, Admin UI)               (Redis MQ, Distributed SSE)
```

- **Depends on**: `ServiceStack`, `ServiceStack.OrmLite`, `ServiceStack.Redis`, `ServiceStack.Common`, `ServiceStack.Client`, `ServiceStack.Interfaces`.
- **Depended on by**: Production server applications, AutoQuery/AutoCrud deployments, Locode, and distributed ServiceStack architectures.
- **Role**: Bridges ServiceStack's core hosting abstractions with OrmLite and Redis data infrastructure.

---

## Key Functionality

### 1. AutoQuery & AutoCrud (`AutoQueryFeature`)
- Generates fully implemented querying and CRUD services dynamically from declarative Request DTOs implementing `IQueryDb<From, Into>`, `ICreateDb<Table>`, `IUpdateDb<Table>`, `IPatchDb<Table>`, and `IDeleteDb<Table>`.
- Supports parameterized filters, joins, aggregates, row-level security, and batch mutations (`BatchCreateAsync`, `BatchUpdateAsync`, etc.).
- Event audit logging via `CrudEvents` / `ICrudEvents`.

### 2. OrmLite Authentication Repository (`OrmLiteAuthRepository`)
- Concrete implementation of `IUserAuthRepository`, `IUserAuthRepositoryAsync`, and `IQueryUserAuth`.
- Persists user credentials, hashed passwords, OAuth tokens (`UserAuthDetails`), and API keys in relational tables.

### 3. Distributed Redis Infrastructure
- **`RedisMqServer`**: Message Queue broker utilizing Redis lists and pub/sub for reliable out-of-process background request processing.
- **`RedisServerEvents`**: Distributed backplane for `ServerEventsFeature`, allowing server-sent events to fan out seamlessly across multi-server web farms.
- **`RedisRequestLogger`**: High-performance request analytics logging into Redis time-series/hashes.

### 4. Database Request Logging (`DbRequestLogger`)
- Captures inbound service requests, status codes, execution duration, user identity, IP address, and request/response DTOs into OrmLite tables for analytics and auditing.
- Supports monthly partitioning and reporting (`GetAnalyticInfo`, `GetIpAnalytics`).

### 5. RDBMS Jobs (`DbJobs`)
- Persistent, database-backed job and task scheduling system with configurable worker pools (`DbJobsWorker`).

---

## Architecture & Design Patterns

### Code-First Data Services
AutoQuery inspects DTO definitions at startup and compiles fast, cached expression-tree query builders (`genericAutoQueryCache`). Queries generate native, parameterized SQL queries via OrmLite's `SqlExpression<T>`.

### Standalone and Hosted Operation
AutoQuery and `DbJobs` support both full ServiceStack AppHost environments and standalone / in-process usage via custom `DbFactory` injection without requiring `HostContext.AppHost`.

### Modern Async Execution
Database operations provide end-to-end async implementations. Worker tasks use `Task.Factory.StartNew(RunAsync, ...).Unwrap()` to maintain true asynchronous worker lifecycles.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Always observe these constraints:

### 1. AutoQuery Cache Collision Prevention
- In `AutoQueryFeature`, dynamic service resolution caches must key on the **`requestDtoType`**, never the underlying table (`fromType`), preventing query collisions when multiple request DTOs target the same model.

### 2. Models Without Primary Keys
- Never assume models have primary keys. Use `ModelDef.FieldDefinitions.FirstOrDefault(x => x.IsPrimaryKey)` instead of direct access to `ModelDef.PrimaryKey` to avoid runtime exceptions on keyless views or tables.

### 3. Cross-Platform Threading in Workers
- Do not invoke `Thread.Abort()` on background workers in modern .NET (`NETCORE` / .NET 6+); it throws `PlatformNotSupportedException`. Use cooperative cancellation (`CancellationTokenSource`) instead.
- In `DbJobsWorker.Dispose`, ensure timeout parameters are in milliseconds (`bgTask?.Wait(timeoutMs)`), not raw seconds.

### 4. Concurrency Safety in Job Quotas
- Use thread-safe collections (`ConcurrentDictionary<Type, bool>`) rather than `HashSet<Type>` when validating unique command quotas across concurrent request threads in `DbJobs`.

### 5. Analytics & Logger Partitioning
- In `DbRequestLogger` and `DbJobsProvider`, use `q.SelectDistinct` when scanning month partitions to prevent massive duplicate record retrieval from large partitioned tables.

---

## Common Modification Scenarios

1. **Adding an AutoQuery Feature or Filter Hook**
   - Extend `AutoQueryFeature` or `AutoCrudMetadata`.
   - Ensure DTO-to-model mapping handles custom named connections and schemas via `CrudContext`.
   - Ensure batch methods (`BatchCreateAsync`, etc.) handle null request collections defensively.

2. **Enhancing OrmLite Authentication / API Keys**
   - Update `OrmLiteAuthRepository` and its async counterparts.
   - Maintain parity between sync and async methods without introducing sync-over-async blocking.

3. **Modifying Redis MQ or Server Events**
   - Preserve thread-safe disposal patterns and non-blocking reconnection loops.
   - Ensure message worker threads check cancellation tokens before reading queues.
