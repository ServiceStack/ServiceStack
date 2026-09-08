# CONTEXT: ServiceStack.Kestrel

## Purpose

`ServiceStack.Kestrel` provides standalone, self-hosted web server capabilities (`AppSelfHostBase`) for modern .NET applications using ASP.NET Core's **Kestrel** HTTP server engine.

It enables developers to build lightweight console applications, worker services, Windows services, daemon processes, and containerized microservices that self-host ServiceStack endpoints directly on Kestrel without requiring full ASP.NET Core web host boilerplate.

**Target frameworks**: `net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack.Interfaces     ServiceStack.Common     ServiceStack (Core)
         │                          │                       │
         └──────────────────────────┼───────────────────────┘
                                    ▼
                         ServiceStack.Kestrel
                                    │
                                    ▼
         Self-Hosted Microservices, Daemons & Console Apps
                 (Powered directly by Kestrel)
```

- **Depends on**: `ServiceStack`, `ServiceStack.Interfaces`, `ServiceStack.Common`, `ServiceStack.Client`, `ServiceStack.Text`, `Microsoft.AspNetCore.App`.
- **Depended on by**: Applications choosing self-hosting via `AppSelfHostBase` rather than embedding ServiceStack via ASP.NET Core `WebApplication.CreateBuilder()`.
- **Comparison**: In typical ASP.NET Core apps, ServiceStack is registered as middleware via `app.UseServiceStack()`. `ServiceStack.Kestrel` encapsulates Kestrel configuration and startup directly inside an `AppSelfHostBase.Init().Start("http://*:5000")` pattern.

---

## Key Functionality

### Primary Classes
| Class | Role |
|---|---|
| `AppSelfHostBase` | Base class for Kestrel self-hosted applications. Configures the underlying `IWebHostBuilder` / `WebApplication`, binds request pipelines, and manages server lifecycle (`Start`, `Stop`). |
| `AppHostHttpListenerBase` | Backwards-compatibility shim mapping legacy `HttpListener` self-host semantics to modern Kestrel. |

### Lifecycle Operations
- `Init()`: Initializes the ServiceStack AppHost, loads plugins, and prepares dependency injection.
- `Start(params string[] urlBases)`: Starts listening on specified URLs/ports via Kestrel.
- `ProcessRequest`: Translates ASP.NET Core `HttpContext` into ServiceStack `IRequest` and dispatches it through the service pipeline.

---

## Architecture & Design Patterns

### Encapsulated Kestrel WebHost
`AppSelfHostBase` internally creates and configures an ASP.NET Core `IWebHost` / `WebApplication` instance with Kestrel server options, injecting ServiceStack as the terminal request delegate.

### Native ASP.NET Core Pipeline Interop
Integrates with `IHttpContextAccessor` to bridge ambient request access and forwards cancellation tokens and response streams directly to Kestrel.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Preserve these mitigations:

### 1. Robust URL and PathBase Parsing
- **Risk**: Parsing base URLs using hardcoded index arithmetic (e.g. `urlBase.IndexOf('/', "https://".Length)`) throws `ArgumentOutOfRangeException` on short or malformed URLs (such as `"http://a"` or `"http://"`).
- **Rule**: Use scheme boundary detection (`urlBase.IndexOf("://", StringComparison.Ordinal)`) and check `startIndex < urlBase.Length` before searching for path separators.

### 2. Request Initialization Error Response Guard
- **Risk**: Writing error responses when request initialization fails throws secondary `InvalidOperationException` crashes if Kestrel has already started streaming the response.
- **Rule**: Always verify `if (!context.Response.HasStarted)` before modifying headers, setting status codes, or writing to the response body.

### 3. Null-Safe Current Request Access
- In `TryGetCurrentRequest()`, safely query `app?.ApplicationServices.GetService<IHttpContextAccessor>()` and handle null returns defensively, returning `null` rather than throwing if called before host startup.

---

## Common Modification Scenarios

1. **Configuring Kestrel Endpoints or TLS / HTTPS**
   - Override `Configure(IWebHostBuilder builder)` or `Configure(KestrelServerOptions options)` in your `AppSelfHostBase` implementation.
   - Configure HTTPS certificates, HTTP/2 or HTTP/3 protocols, and connection limits.

2. **Embedding Custom ASP.NET Core Middleware**
   - Hook into `Configure(IApplicationBuilder app)` within `AppSelfHostBase`.
   - Ensure middleware executes in the expected order relative to ServiceStack request dispatching.
