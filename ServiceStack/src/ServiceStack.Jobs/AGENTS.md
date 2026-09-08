# CONTEXT: ServiceStack.Jobs

## Purpose

`ServiceStack.Jobs` provides a robust, lightweight background task and job scheduling framework built directly into ServiceStack using SQLite (via `ServiceStack.OrmLite.Sqlite.Data`).

It delivers:
- **Persistent Background Jobs**: Queued background execution of ServiceStack commands and requests with retry policies and state tracking.
- **Scheduled Tasks**: Recurring and cron-style scheduled job triggers.
- **SQLite Request Logging (`SqliteRequestLogger`)**: Fast, zero-maintenance local database logging for request metrics, API key analytics, and IP analytics with automatic monthly file partitioning.
- **Admin Management APIs (`AdminJobServices`)**: Built-in endpoints for querying job queues, inspecting logs, pausing, requeuing, and cancelling background jobs from admin dashboards or UI.

**Target frameworks**: `net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack (Core)   ServiceStack.OrmLite   ServiceStack.Server
         │                     │                      │
         └─────────────────────┼──────────────────────┘
                               ▼
                      ServiceStack.Jobs
                               │
           ┌───────────────────┴───────────────────┐
           ▼                                       ▼
Background Task Workers                SQLite Request Logging & Analytics
(Scheduled Commands, Job Queues)       (Admin UI, Metrics, Audit Trail)
```

- **Depends on**: `ServiceStack`, `ServiceStack.OrmLite`, `ServiceStack.OrmLite.Sqlite.Data`, `ServiceStack.Server`.
- **Depended on by**: ServiceStack applications needing reliable background job processing and request analytics without external broker infrastructure (like Redis or RabbitMQ).
- **Alternative / Sibling Providers**: `BackgroundMqService` (in-memory only), `RedisMqServer` (in `ServiceStack.Server`), `RabbitMqServer` (in `ServiceStack.RabbitMq`).

---

## Key Functionality

### 1. Job Management & Execution
| Type | Role |
|---|---|
| `BackgroundsJobFeature` | ServiceStack plugin registering job workers, scheduled tasks, and admin APIs. |
| `BackgroundJobs` | Core manager for enqueuing commands, scheduling recurring tasks, tracking job status, and managing worker pools. |
| `BackgroundJobsWorker` | Background worker processing job queues asynchronously with cancellation support. |
| `AdminJobServices` | AutoQuery / REST endpoints exposing job status, query history, and manual queue controls. |

### 2. Request Logging & Analytics (`SqliteRequestLogger`)
- Writes service request/response logs directly to SQLite databases.
- Automates disk-partitioned monthly log files (`GetTableMonths`).
- Computes analytics breakdowns by API Key, IP address, and User ID.

---

## Architecture & Design Patterns

### Task Unwrapping for Complete Async Tracking
Workers launch asynchronous processing loops via `Task.Factory.StartNew(RunAsync, ...).Unwrap()`. The `.Unwrap()` call ensures the outer tracking task waits for inner asynchronous job completion, preventing premature worker teardown.

### SQLite-Backed Persistence
Leverages OrmLite's SQLite provider for embedded zero-configuration storage. Database schema and tables are initialized automatically on startup.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Adhere to these principles:

### 1. Milliseconds vs. Seconds in Worker Disposal
- **Risk**: `Task.Wait(int millisecondsTimeout)` expects milliseconds. Passing seconds (e.g. `60`) causes workers to abort running jobs after only 60 milliseconds instead of 60 seconds.
- **Rule**: Always convert timeout seconds to milliseconds (`defaultTimeOutSecs * 1000`) before calling `bgTask.Wait(timeoutMs)`.

### 2. Thread-Safe Unique Command Tracking
- **Risk**: Using a standard `HashSet<Type>` for tracking registered command types across concurrent request threads causes race conditions and corruption under load.
- **Rule**: Use thread-safe collections (`ConcurrentDictionary<Type, bool>`) with atomic `TryAdd`.

### 3. IP Analytics Cache Key Correctness
- In `SqliteRequestLogger.GetIpAnalytics`, cache validation must check `ret.Ips?.Count > 0` (not `ApiKeys`), ensuring requests from non-authenticated IP addresses are properly cached.

### 4. Resilient Directory Checks
- In `BackgroundsJobFeature.GetTableMonths`, always verify directory existence (`dir.Exists`) before calling `dir.GetFiles()` to prevent `DirectoryNotFoundException` on fresh setups.

### 5. Startup Null Coalescing in Logger Registration
- `SqliteRequestLogger.Register` must null-coalesce `ExcludeRequestDtoTypes` when combining with `IgnoreRequestTypes` to prevent `ArgumentNullException`.

---

## Common Modification Scenarios

1. **Enqueuing Commands / Background Work**
   - Inject `BackgroundJobs` or call `HostContext.Resolve<BackgroundJobs>().EnqueueCommand(new MyCommand { ... })`.
   - Ensure commands are registered with the container.

2. **Registering a Scheduled Recurring Task**
   - Use `jobs.Schedule(cronExpression, () => ...)` or configure scheduled task attributes on Request DTOs.

3. **Customizing SQLite Log Storage**
   - Configure `SqliteRequestLogger.DbPath` or partition rollover strategies in `AppHost.Configure()`.
