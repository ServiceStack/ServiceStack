# Security and Reliability Remediation: `ServiceStack.NetFramework`

## Summary
Audit and hardening of `ServiceStack.NetFramework` (targeting .NET Framework 4.7.2), fixing thread-safety race conditions in MiniProfiler resource caching, preventing unmanaged OS handle leakage, eliminating exception-driven flow control, safeguarding against null dereferences across profiler data and timing hierarchies, escaping regex special characters in SQL formatters, and mitigating potential XSS in HTML report generation.

---

## Remediations

### 1. Concurrency & Thread-Safety in MiniProfilerHandler Resource Cache
- **Issue**: `_ResourceCache` was implemented using a non-thread-safe `Dictionary<string, string>`. Concurrent web requests reading and adding embedded resources could trigger dictionary corruption or infinite spin-loops.
- **Fix**: Replaced with `ConcurrentDictionary<string, string>`, safely populating it after checking that `stream != null`. Missing embedded resources cleanly return 404 instead of throwing `NullReferenceException`.

### 2. OS Synchronization Handle Leak in Self Hosts
- **Issue**: `AppHostHttpListenerSmartPoolBase` and `ServiceStack.SmartThreadPool.AppSelfHostBase` created an unmanaged kernel `AutoResetEvent` (`listenForNextRequest`). Neither class disposed the event in `Dispose(bool disposing)`, causing OS handle leaks on host disposal.
- **Fix**: Added `listenForNextRequest.Dispose()` to `Dispose(bool disposing)` in both host base classes.

### 3. Exception-Free Flow Control for IDs and DbTypes
- **Issue**:
  - `MiniProfilerHandler.Results`: Parsed query string IDs using `try { new Guid(...) } catch {}`, throwing and catching exceptions on malformed requests.
  - `SqlServerFormatter.FormatSql`: Resolved `DbType` enums using `try { parsed = p.DbType.ToEnum<DbType>(); } catch {}`, throwing exceptions on unrecognized or database-specific type names.
- **Fix**:
  - Converted to `Guid.TryParse(...)`.
  - Converted to `Enum.TryParse<DbType>(p.DbType, true, out var parsed)`.

### 4. Null Dereference Defenses
- **`ProfiledDbConnection.GetDbProfiler`**: Guarded against null `IProfiler` parameter before invoking `.GetMiniProfiler()`, preventing NRE when wrapping connections with no profiler.
- **`MiniProfiler.GetTimingHierarchy`**: Guarded with `if (_root == null) yield break;` to prevent pushing and popping null roots on uninitialized or deserialized profilers.
- **`MiniProfiler.Root` & `DurationMilliseconds`**: Checked `_root != null` before inspecting `_root.HasChildren` and accessed duration via `_root?.DurationMilliseconds`.
- **`MiniProfiler.IDbProfiler.AddSqlTiming`**: Coalesced `stats.CommandString ?? ""` to prevent `ArgumentNullException` when querying `_sqlExecutionCounts`.
- **`WebRequestProfilerProvider.Start`**: Guarded against short or empty `AppRelativeCurrentExecutionFilePath` before calling `Substring(1)` and guarded `context.Request.Url`.
- **`StackTraceSnippet.Get`**: Guarded against null `t.GetMethod()` and assembly metadata from dynamic/emitted frames.

### 5. State Isolation & Regex Safety in InlineFormatter
- **Issue**: `_includeTypeInfo` was declared as `static bool`, causing separate `InlineFormatter` instances to overwrite each other's configuration. In addition, parameter names were passed directly into `Regex.Replace` without escaping, causing regex syntax errors if parameters used symbols like `?`.
- **Fix**: Made `_includeTypeInfo` an instance field (`private readonly bool _includeTypeInfo;`), and escaped parameter names with `Regex.Escape(name)`.

### 6. XSS Mitigation in Full Page Profiler Results
- **Issue**: `MiniProfilerHandler.ResultsFullPage` embedded `profiler.Name` directly into the `<title>` tag without HTML encoding.
- **Fix**: Applied `HttpUtility.HtmlEncode(profiler.Name)` when rendering page title output.
