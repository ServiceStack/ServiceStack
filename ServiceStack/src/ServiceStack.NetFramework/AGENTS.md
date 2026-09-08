# CONTEXT: ServiceStack.NetFramework

## Purpose

`ServiceStack.NetFramework` houses features and infrastructure tailored specifically for **classic .NET Framework 4.7.2** and `System.Web` hosting environments.

It contains:
- **Integrated MiniProfiler**: Embedded MiniProfiler engine, database profiling wrappers (`ProfiledDbConnection`), SQL formatting tools, and UI asset handlers for profiling ServiceStack requests and database calls under IIS / ASP.NET.
- **Classic Self-Hosting Infrastructure**: `AppHostHttpListenerSmartPoolBase` and `AppSelfHostBase` built on top of `System.Net.HttpListener` and `SmartThreadPool` for standalone Windows Services and console applications on .NET Framework.

**Target frameworks**: `net472`

---

## Role in ServiceStack Ecosystem

```
ServiceStack (Core)   ServiceStack.OrmLite   System.Web (.NET 4.7.2)
         │                     │                     │
         └─────────────────────┼─────────────────────┘
                               ▼
                   ServiceStack.NetFramework
                               │
         ┌─────────────────────┴─────────────────────┐
         ▼                                           ▼
Classic IIS / ASP.NET Hosts               Legacy Windows Service Self-Hosts
 (MiniProfiler UI, HttpHandlers)          (HttpListener + SmartThreadPool)
```

- **Depends on**: `ServiceStack`, `ServiceStack.Interfaces`, `ServiceStack.Common`, `ServiceStack.Client`, `ServiceStack.Text`, `ServiceStack.OrmLite`.
- **Depended on by**: Legacy .NET Framework applications and `ServiceStack.Mvc` (when compiled for `net472`).
- **Modern .NET Equivalent**: For modern .NET (.NET 6+), ServiceStack uses OpenTelemetry diagnostics and `ServiceStack.Kestrel` instead of `ServiceStack.NetFramework`.

---

## Key Functionality

### 1. MiniProfiler Subsystem
- **`MiniProfiler` / `IProfiler`**: Request timing, step nesting, and SQL execution capture.
- **`ProfiledDbConnection`**: Decorator over `IDbConnection` capturing executed SQL statements, parameters, and timings.
- **`MiniProfilerHandler`**: Serves embedded UI assets, CSS, JS, and JSON/HTML profiling results (`/mini-profiler-resources/*`).
- **SQL Formatters**: Formats SQL parameters inline for SQL Server, Oracle, and MySQL profiling outputs.

### 2. SmartThreadPool & Self-Hosting
- **`AppHostHttpListenerSmartPoolBase`**: Self-host utilizing `HttpListener` with thread-pool optimization via `SmartThreadPool`.
- **`SmartThreadPool`**: Specialized thread-pooling engine for high-concurrency request servicing on legacy Windows systems.

---

## Architecture & Design Patterns

### Embedded UI Resource Streaming
MiniProfiler UI assets (HTML, CSS, JS) are embedded within the assembly and served via `MiniProfilerHandler` through thread-safe caches (`ConcurrentDictionary`).

### Non-Invasive Database Profiling
Wraps existing ADO.NET connections and commands without requiring modification of application data logic.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Always enforce these rules:

### 1. Resource Cache Concurrency
- `MiniProfilerHandler._ResourceCache` must use `ConcurrentDictionary<string, string>` to prevent race conditions or hash table corruption under concurrent requests.

### 2. OS Handle Cleanup in Self-Hosts
- `AppHostHttpListenerSmartPoolBase` and `AppSelfHostBase` allocate unmanaged `AutoResetEvent` kernel handles (`listenForNextRequest`). Both classes must explicitly dispose these handles in `Dispose(bool disposing)`.

### 3. Parsing Without Exceptions
- Parse query IDs using `Guid.TryParse` rather than `try { new Guid(...) } catch`.
- Parse database types using `Enum.TryParse<DbType>` rather than exception-driven flow control.

### 4. XSS Prevention in Profiler Results
- Profiler names and session labels displayed in full HTML reports must be encoded using `HttpUtility.HtmlEncode`.

### 5. Regex Parameter Escaping
- In `InlineFormatter`, parameter names must be escaped via `Regex.Escape(name)` before string replacement to avoid regex syntax errors when parameters contain symbols like `?` or `$`.

---

## Common Modification Scenarios

1. **Extending MiniProfiler Reporting**
   - Hook into `MiniProfiler.Start()` or customize `IProfilerProvider`.
   - Ensure all timing hierarchy traversals guard against null `_root`.

2. **Customizing Embedded Asset Handling**
   - Assets served from `MiniProfiler/UI/` must return HTTP 404 cleanly when missing, without throwing `NullReferenceException`.
