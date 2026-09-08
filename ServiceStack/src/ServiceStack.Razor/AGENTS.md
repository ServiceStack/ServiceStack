# CONTEXT: ServiceStack.Razor

## Purpose

`ServiceStack.Razor` provides a complete Razor-based HTML view engine plugin (`RazorFormat`) for ServiceStack applications. It turns ServiceStack into an integrated web framework capable of rendering dynamic HTML pages, partial views, cascading layouts, and markdown without requiring ASP.NET MVC.

Key features:
- **No-Ceremony Web Pages**: HTML views bound directly to ServiceStack Request/Response DTOs or dynamic models.
- **Runtime View Compilation & Hot Reloading**: Dynamic recompilation of modified `.cshtml` templates at runtime (`FileSystemWatcherLiveReload`).
- **Cascading Layout Templates**: Multi-level hierarchical layout and content pages.
- **Built-in Anti-XSRF Protection**: Anti-forgery tokens, form helpers, and cryptographic token verification.

**Target frameworks**: `net472`

---

## Role in ServiceStack Ecosystem

```
ServiceStack (Core)       Microsoft.AspNet.Razor
         │                               │
         └───────────────┬───────────────┘
                         ▼
                ServiceStack.Razor (RazorFormat)
                         │
                         ▼
Full-Stack Server-Rendered Web Applications & Portals
```

- **Depends on**: `ServiceStack`, `ServiceStack.Interfaces`, `ServiceStack.Common`, `ServiceStack.Client`, `ServiceStack.Text`, `ServiceStack.OrmLite`, `Microsoft.AspNet.Razor`.
- **Depended on by**: ServiceStack web applications rendering server-side Razor views on .NET Framework.
- **Modern .NET Alternative**: On modern .NET Core / .NET 8+, ServiceStack apps typically use `#Script`, ServiceStack.Mvc with Razor Pages, or modern clients (Vue/React/Blazor).

---

## Key Functionality

### Primary Types
| Class | Role |
|---|---|
| `RazorFormat` | The ServiceStack plugin (`IPlugin`, `IViewEngine`) registering the Razor view engine and content-type filters. |
| `ViewPageBase<TModel>` | Base class for typed Razor view pages providing access to `Request`, `Response`, session, and HTML helpers. |
| `RazorViewManager` | Manages discovery, caching, and compilation of `.cshtml` views. |
| `FileSystemWatcherLiveReload` | Watches physical disk files for changes and automatically invalidates cached views during development. |
| `AntiForgery` / `AntiForgeryToken` | Generates and validates cryptographic anti-CSRF request tokens in HTML forms. |

---

## Architecture & Design Patterns

### VFS Integration
Razor views are resolved through ServiceStack's Virtual File System (`IVirtualFiles`), allowing templates to be loaded from disk directories, embedded resources, or custom VFS sources.

### Cascading Layouts & View Models
Views resolve `@inherits ViewPage<T>` models automatically from service response DTOs or fall back to `DynamicRequestObject` for parameterless pages.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Always preserve these security fixes:

### 1. XSS Prevention in Error Rendering
- **Risk**: Concatenating error strings from `ResponseStatus` (`ErrorCode`, `Message`, `StackTrace`) directly into raw HTML in `ViewPageBase.GetErrorHtml` allows reflected/stored XSS attacks.
- **Rule**: Always HTML-encode error fields using `HttpUtility.HtmlEncode()` before rendering inside HTML templates.

### 2. Constant-Time Anti-XSRF Token Verification
- **Risk**: Byte array comparisons using standard branching loops (`areEqual &= (a[i] == b[i])`) leak timing information, vulnerable to side-channel timing attacks.
- **Rule**: Anti-XSRF token validation must use constant-time XOR comparison:
  ```csharp
  int diff = 0;
  for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
  return diff == 0;
  ```

### 3. Thread-Safe View Caching
- `RazorViewManager.Pages` and `ViewNamesMap` must be `ConcurrentDictionary` instances to avoid corruption when `FileSystemWatcher` invalidates templates concurrently with incoming web requests.

### 4. FileSystemWatcher and Stream Resource Cleanup
- `FileSystemWatcherLiveReload` must implement `IDisposable` to detach file system watchers and prevent directory handle leaks.
- `StreamWriter` allocations in `ExecuteRazorPage` must be scoped in `using` blocks with explicit flushing.

### 5. Assembly Scanning Resilience
- Always wrap `assembly.GetTypes()` in try-catch handling `ReflectionTypeLoadException` during view precompilation.

---

## Common Modification Scenarios

1. **Registering the Razor Plugin**
   - Register in `AppHost.Configure`: `Plugins.Add(new RazorFormat());`.
   - Configure precompilation or live-reloading settings via `RazorFormat`.

2. **Extending HTML Helpers**
   - Extend `HtmlHelper` or `ViewPageBase` with custom extension methods for formatting, navigation, or form rendering.
