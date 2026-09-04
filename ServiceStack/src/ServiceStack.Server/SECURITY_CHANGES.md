# Security and Hardening Improvements in ServiceStack.Server

## Summary
This document summarizes modernization, concurrency hardening, reliability, and bug fixes applied to `ServiceStack.Server`.

---

## 1. Concurrency and Thread Safety
- **`DbJobs.cs`**:
  - Replaced non-thread-safe `HashSet<Type> uniqueCommandTypes` with `ConcurrentDictionary<Type, bool>` to ensure thread-safe command quota validation across concurrent web request threads invoking `EnqueueCommand`.
- **`DbJobsWorker.cs`**:
  - Unwrapped async task invocation (`Task.Factory.StartNew(RunAsync, ...).Unwrap()`), ensuring `BackgroundTask` properly tracks the complete asynchronous lifecycle of worker execution rather than completing prematurely on the first `await`.
  - Fixed timeout wait in `Dispose(bool disposing)`: `bgTask?.Wait(timeoutMs)` instead of `bgTask?.Wait(defaultTimeOutSecs)`, correcting a unit conversion bug where disposal only waited milliseconds equal to seconds (e.g. 60ms vs 60,000ms).
  - Updated error log identifier to `"DbJobsWorker dispose error"`.

---

## 2. Month Partition Scanning & Query Deduplication
- **`DbJobsProvider.cs` & `DbLoggingProvider` (in `DbRequestLogger.cs`)**:
  - In `GetTableMonths(IDbConnection db)`, replaced `q.Select(...)` with `q.SelectDistinct(...)` to prevent redundant record fetching across large log and completed job tables.
  - Enhanced month parsing to support both partitioned/disk file table naming conventions (containing `_`) and standard `YYYY-MM` date format strings returned by `SqlDateFormat(...)` across RDBMS providers (SQLite, MySQL, SQL Server, PostgreSQL).
  - Added `.Distinct()` and `.OrderDescending()` on returned dates.

---

## 3. Database Request Logger Hardening & Analytics Fixes
- **`DbRequestLogger.cs`**:
  - Fixed analytics tab activation in `GetAnalyticInfo`: corrected checks for "API Keys" (`result.apiKeys == 1`) and "IP Addresses" (`result.ips == 1`), which were incorrectly checking `result.apis == 1`.
  - Fixed caching bug in `GetIpAnalytics`: corrected report cache persistence check to `if (ret.Ips?.Count > 0)` instead of `if (ret.ApiKeys?.Count > 0)`, ensuring IP analytics reports are properly cached for requests that did not supply an API key.
  - Guarded `ExcludeRequestDtoTypes` in `Register` with `(ExcludeRequestDtoTypes ?? []).Union(...)` to prevent `NullReferenceException` when `ExcludeRequestDtoTypes` is uninitialized.

---

## 4. Message Handler Worker Reliability & Cross-Platform Support
- **`MessageHandlerWorker.cs`**:
  - Guarded legacy `bgThread.Abort()` call with `#if !NETCORE` to prevent `PlatformNotSupportedException` at runtime in modern .NET (.NET 6+).
  - Added null check in `GetStatus()` (`bgThread?.ThreadState.ToString() ?? "None"`) to prevent `NullReferenceException` when query status is requested while the worker background thread is uninitialized or stopped.

---

## 5. API Keys and Auth Security
- **`ApiKeyCredentialsProvider.cs`**:
  - Added null guard when resolving `IApiKeySource` from `IRequest` (`if (source == null) return null;`) to avoid unhandled `NullReferenceException` when the provider is queried without an active API key source registered.

---

## 6. Async Caching Performance
- **`OrmLiteCacheClient.Async.cs`**:
  - Added non-blocking `VerifyAsync` overloads with `CancellationToken` support, performing asynchronous deletion (`DeleteAsync` / `DeleteByIdAsync`) rather than blocking the thread pool with synchronous calls inside `GetAsync`, `IncrementAsync`, `DecrementAsync`, and `GetAllAsync`.

---

## 7. Multi-Targeting & Diagnostics
- **`ServiceStack.Server.csproj`**:
  - Standardized target framework define constants (`NET6_0`, `NET8_0`, `NET10_0`).
- **`Properties/AssemblyInfo.cs`**:
  - Added `#if !NET472` conditional `InternalsVisibleTo("ServiceStack.Server.Tests")` and `InternalsVisibleTo("ServiceStack.WebHost.Endpoints.Tests")` to support strong-named builds under `net472` while enabling comprehensive unit testing on modern .NET targets.

---

## 8. AutoQuery and AutoCrud Reliability & Cache Hardening
- **`AutoQueryFeature.cs`**:
  - Aligned cache key in untyped `Execute` and `ExecuteAsync` (`genericAutoQueryCache.TryGetValue(requestDtoType, ...)` instead of `fromType`), eliminating cache misses and preventing mapping collisions across different query DTOs targeting the same source table.
  - Replaced unsafe reflection `.First(...)` in dynamic IL service generation (`GenerateMissingQueryServices`) with `.FirstOrDefault(...)` and descriptive `NotSupportedException` errors.
  - Added `DbFactory` property on `AutoQuery` with fallback in `GetDb` and `GetDbNamedConnection` to support standalone / in-process execution when `HostContext.AppHost` is null.
  - Guarded `AutoQueryServiceBase.Exec` and `ExecAsync` against null `Request` references, falling back cleanly to empty parameter dictionaries.
  - Guarded `AutoQueryExtensions.CreateQuery` against null `IRequest`, using null-coalescing empty dictionaries.
  - Guarded `Filter<From>` and `Filter` against null `IQueryDb dto` inputs.
  - Guarded `OrderByPrimaryKey` in `AppendLimits` against models without a defined primary key.
  - Guarded `BeforePluginsLoaded` and `Register` against null `appHost` and uninitialized container instances.
- **`AutoQueryFeature.AutoCrud.cs`**:
  - Made `CrudContext` properties read-write for flexible testing and customization.
  - Guarded `RequestIdGetter` against models without primary keys using `ModelDef?.FieldDefinitions.FirstOrDefault(x => x.IsPrimaryKey)` instead of accessing `ModelDef.PrimaryKey` (which throws when no primary key exists).
  - Guarded `NamedConnection` resolution in `CrudContext.Create` against unresolvable `IAutoQueryDb`.
  - Guarded `to.ModelDef` and `to.ModelType` in `AutoCrudMetadata.Create` for POCOs lacking OrmLite metadata.
  - Guarded primary key access in `GetAutoFilterExpressions`.
  - Added null guards in `BatchCreateAsync`, `BatchUpdateAsync`, `BatchPatchAsync`, `BatchDeleteAsync`, and `BatchSaveAsync` to return empty lists when `requests` is null.
- **`CrudUtils.cs`**:
  - Fixed argument forwarding in `GetTables`: preserved `namedConnection`, `schema`, `includeTables`, `excludeTables`, and `config` instead of discarding them as `null`.
  - Guarded `t.Columns?.Each(...)` against null column arrays when table introspection reports errors.
- **`CrudEvents.cs`**:
  - Guarded `ToEvent` against null `context.Request`, null `context.ModelType`, null `context.Dto`, and null `IpMask`.
  - Guarded `ShouldRecord` against null `context`.
  - Added null guards in `InitSchema` and `Clear` extension methods.
- **`GenerateCrudServices.cs`**:
  - Added null guard for `column` in `DefaultResolveColumnType`.
  - Replaced unsafe `First(x => x.IsKey)` with `FirstOrDefault(x => x.IsKey)` in `ResolveMetadataTypes`.
- **`AutoQueryScripts.cs`**:
  - Added null propagation for `appHost`, `Metadata`, and `Context?.ScriptMethods`.

