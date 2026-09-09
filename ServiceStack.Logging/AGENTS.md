# CONTEXT: ServiceStack.Logging

## Purpose

`ServiceStack.Logging` is a collection of logging integration providers that adapt external .NET logging frameworks to ServiceStack's core logging abstractions (`ILogFactory` and `ILog`).

It enables ServiceStack applications to route internal framework logs and application diagnostic messages to their preferred logging destination:
- **Serilog (`ServiceStack.Logging.Serilog`)**: Structured logging adapter forwarding to Serilog sink pipelines.
- **NLog (`ServiceStack.Logging.NLog`)**: Adapter for NLog targets and layout renderers.
- **Log4Net (`ServiceStack.Logging.Log4Net`)**: Adapter for Apache log4net repositories.
- **Slack (`ServiceStack.Logging.Slack`)**: Webhook logger publishing error notifications directly to Slack channels.
- **EventLog (`ServiceStack.Logging.EventLog`)**: Windows Event Log integration.
- **ELMAH (`ServiceStack.Logging.Elmah`)**: Intercepting logger signaling errors into ELMAH pipelines.

**Target frameworks**: `net472;net6.0;net8.0;net10.0` (varying per provider package)

---

## Role in ServiceStack Ecosystem

```
ServiceStack.Interfaces (ILogFactory, ILog, LogManager)
                         │
                         ▼
               ServiceStack.Logging
                         │
    ┌────────────┬───────┴────┬───────────┬────────────┐
    ▼            ▼            ▼           ▼            ▼
 Serilog        NLog       Log4Net      Slack       EventLog
```

- **Depends on**: `ServiceStack.Interfaces`, external logging SDKs (`Serilog`, `NLog`, `log4net`, etc.).
- **Depended on by**: ServiceStack applications requiring external log aggregation and monitoring.
- **Registration**: Initialized early in application startup (before `AppHost.Init()`) by setting `LogManager.LogFactory`.

---

## Key Functionality

### Primary Provider Adapters
| Package | Factory Class | Description |
|---|---|---|
| `ServiceStack.Logging.Serilog` | `SerilogFactory` | Routes messages to Serilog `Log.ForContext`. |
| `ServiceStack.Logging.NLog` | `NLogFactory` | Wraps NLog `LogManager.GetLogger`. |
| `ServiceStack.Logging.Log4Net` | `Log4NetFactory` | Wraps Apache log4net repositories. |
| `ServiceStack.Logging.Slack` | `SlackLogFactory` | Dispatches errors to Slack incoming webhooks. |
| `ServiceStack.Logging.EventLog` | `EventLogFactory` | Writes to Windows Event Log sources. |
| `ServiceStack.Logging.Elmah` | `ElmahInterceptingLogger` | Dispatches errors to ELMAH error signals. |

---

## Architecture & Design Patterns

### Plug-and-Play Factory Pattern
Setting `LogManager.LogFactory = new SerilogFactory()` transparently redirects all ServiceStack framework logging across the entire process without modifying service code.

### Safe Fallback Logging
Logging adapters wrap external framework dispatches in defensive try/catch blocks so failures in logging destinations (e.g., Slack webhook downtime, ELMAH configuration issues) never crash the calling application or suppress secondary loggers.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Follow these precautions:

### 1. Slack Logging Webhook Security
- URLs pointing to `http://hooks.slack.com` must be automatically upgraded to `https://` to prevent plaintext transmission of secret webhook tokens over the wire.
- Fire-and-forget webhook tasks must observe faults via `.ContinueWith(..., TaskContinuationOptions.OnlyOnFaulted)` to prevent unobserved task exceptions.
- Limit inner exception traversal to a fixed depth (e.g. 20) to prevent infinite loops on circular exceptions.

### 2. Windows Event Log OS Handle and Buffer Management
- `EventLog` instances must be wrapped in `using` blocks to prevent unmanaged handle leaks.
- Windows Event Log entry text must be truncated to 31,839 characters to avoid buffer overflow `ArgumentException` crashes.
- Guard source verification calls against `SecurityException` on unprivileged accounts.

### 3. Serilog Category Resolution
- In `SerilogFactory.GetLogger(string typeName)`, fall back to `ForContext("SourceContext", typeName)` when `Type.GetType(typeName)` returns null. Handle null message payloads gracefully.

### 4. Entry Assembly Fallback in Log4Net
- Use `Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()` to avoid `ArgumentNullException` in web/worker hosts where `GetEntryAssembly()` is null.

---

## Common Modification Scenarios

1. **Configuring a Logging Provider at Startup**
   ```csharp
   LogManager.LogFactory = new SerilogFactory();
   ```

2. **Creating a Custom Log Provider**
   - Implement `ILogFactory` and `ILog`.
   - Ensure `ILog.Error` methods safely handle null messages and null exceptions.
