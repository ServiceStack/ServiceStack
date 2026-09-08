# CONTEXT: ServiceStack.Mvc

## Purpose

`ServiceStack.Mvc` provides integration adapters bridging ServiceStack with ASP.NET MVC and ASP.NET Core MVC / Razor Pages.

It enables applications to combine MVC controllers/views with ServiceStack's web services architecture seamlessly:
- **`ServiceStackController`**: Base MVC controller class wired to ServiceStack's IoC container, authentication sessions, caching, and service gateway.
- **Dependency Injection Integration**: `FunqControllerFactory` resolves MVC controllers through ServiceStack's Funq container.
- **Razor & Static Site Generation (SSG)**: `RazorPagesEngine`, `RazorFormat`, and `RazorSsg` for static prerendering and dynamic Razor view evaluation.
- **Unified Validation & Profiling**: FluentValidation adapters and MiniProfiler integration.

**Target frameworks**: `net472;net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack (Core)       ASP.NET MVC / Microsoft.AspNetCore.Mvc
         │                               │
         └───────────────┬───────────────┘
                         ▼
                 ServiceStack.Mvc
                         │
                         ▼
Hybrid Web Applications (ServiceStack APIs + MVC Views / Razor Pages)
```

- **Depends on**: `ServiceStack`, `ServiceStack.Interfaces`, `ServiceStack.Common`, `ServiceStack.Client`, `ServiceStack.Text`.
- **Depended on by**: Web applications hosting both ServiceStack web services and ASP.NET MVC controllers or Razor Pages in the same process.
- **Under `.NET Framework` (net472)**: Targets `Microsoft.AspNet.Mvc` (v5.3.0) and `ServiceStack.NetFramework`.
- **Under modern `.NET` (net6.0+)**: Targets ASP.NET Core MVC abstractions and Razor Pages engine.

---

## Key Functionality

### Primary Types
| Class | Description |
|---|---|
| `ServiceStackController` | Base MVC controller with built-in access to `IAuthSession`, `ICacheClient`, `IServiceGateway`, and `ServiceStackHost`. |
| `FunqControllerFactory` | Custom `IControllerFactory` resolving MVC controllers via ServiceStack's Funq container. |
| `ExecuteServiceStackFiltersAttribute` | Filter attribute applying ServiceStack request filters to MVC actions. |
| `RazorPagesEngine` | Renders Razor Pages within the ServiceStack execution pipeline. |
| `RazorSsg` | Static Site Generation engine for compiling Razor templates to static HTML files. |
| `MvcPageResult` | Custom `ActionResult` executing ServiceStack page templates. |

---

## Architecture & Design Patterns

### Shared Session and Host Context
`ServiceStackController` bridges the MVC action context with ServiceStack's `IRequest`, allowing MVC actions to read the exact same authenticated user session (`SessionAs<T>()`) and user credentials as ServiceStack services.

### Safe Assembly Scanning
When discovering controllers, `FunqControllerFactory` scans target assemblies safely by handling `ReflectionTypeLoadException` so that missing optional dependencies in referenced assemblies do not prevent controller discovery.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Follow these precautions:

### 1. Robust Redirect URL Formatting
- **Risk**: Constructing login redirects with hardcoded `"?redirect={0}"` produces malformed URLs when `UnauthorizedRedirectUrl` already contains query parameters (e.g. `"/auth/login?theme=dark?redirect=..."`).
- **Rule**: Use dynamic delimiter detection: `url.IndexOf('?') >= 0 ? "&" : "?"`. Provide fallback to `"/login"` if `AuthFeature` is unregistered.

### 2. Request URL Null Safety
- In `ServiceStackController.InvokeControllerDefaultAction`, use null-safe accessors: `httpContext.Request.Url?.OriginalString ?? httpContext.Request.RawUrl ?? ""`.

### 3. Response Streaming Guard in Custom Action Results
- **Risk**: Setting `ContentType` or headers after a streaming response has started throws `InvalidOperationException` in ASP.NET Core pipelines.
- **Rule**: Guard header modifications with `if (!response.HasStarted)` in `MvcPageResult` and custom action results.

### 4. Safe Type Resolution in Assembly Scanning
- Always wrap `assembly.GetTypes()` in try-catch handling `ReflectionTypeLoadException`, retrieving loaded types from `ex.Types.Where(t => t != null)`.

---

## Common Modification Scenarios

1. **Creating an Integrated MVC Controller**
   - Inherit from `ServiceStackController`.
   - Access typed sessions with `var session = SessionAs<CustomUserSession>();`.
   - Call services in-process using `Gateway.Send(new MyRequest())`.

2. **Customizing Razor Page Rendering or SSG**
   - Hook into `RazorPagesEngine` or `RazorSsg`.
   - Ensure reflection lookups are null-guarded and handle async completion cleanly.
