# Security and Reliability Remediation: `ServiceStack.Mvc`

## Summary
Audit and hardening of `ServiceStack.Mvc`, resolving query-string formatting anomalies on authorization redirects, safeguarding against null dereferences in static site generation (SSG) and view data creation, guarding response stream headers in custom `ActionResult`s, and safely loading assemblies in `FunqControllerFactory`.

---

## Remediations

### 1. Robust Redirect URL Formatting
- **Issue**: In `ServiceStackController.cs`, `AuthenticationErrorResult` and `ForbiddenErrorResult` constructed redirect URLs using fixed `"?redirect={0}"`. If `UnauthorizedRedirectUrl` or `ForbiddenRedirectUrl` already contained query string parameters (e.g. `"/auth/login?theme=dark"`), this resulted in malformed redirect URLs containing duplicate `?` delimiters.
- **Fix**: Detected existing query parameter delimiters with `url.IndexOf('?') >= 0 ? "&" : "?"`, ensuring query parameters are properly chained. Also safeguarded against null return if `AuthFeature` is unregistered by falling back to `"/login"`.

### 2. Request URL Null Safety
- **Issue**: In `ServiceStackController.InvokeControllerDefaultAction`, `httpContext.Request.Url.OriginalString` was accessed directly without null-checking, which could throw `NullReferenceException` in mock/test contexts or unusual IIS hosting configurations.
- **Fix**: Updated to `httpContext.Request.Url?.OriginalString ?? httpContext.Request.RawUrl ?? ""`.

### 3. Response Streaming Guard in Custom Action Results
- **Issue**: In `ServiceStackJsonResult.ExecuteResultAsync` and `MvcPageResult.ExecuteResultAsync`, `ContentType` and headers were set directly on the ASP.NET Core `HttpResponse` without checking if the response had already started streaming.
- **Fix**: Added `if (!response.HasStarted)` / `if (!context.HttpContext.Response.HasStarted)` guards before modifying response headers or content types, avoiding secondary `InvalidOperationException` errors during pipeline processing.

### 4. Nullability & Reflection Hardening in Razor Pages and SSG
- **Issue**:
  - `RazorPagesEngine.PopulateRazorPageContext`: Optional parameter `ActionContext actionContext = null` lacked the nullable annotation `?`, producing `CS8625`.
  - `RazorPagesEngine.CreateViewData`: Return value of `invoker(model) as ViewDataDictionary` was returned directly without null checking (`CS8603`).
  - `RazorSsg.cs`: Reflection method lookup for `RenderStaticRazorPageAsync` and `jsResult?.ToString()` were dereferenced without checking for null (`CS8602`).
- **Fix**:
  - Marked `ActionContext? actionContext = null` and instantiated `PageContext` safely.
  - Added descriptive `InvalidOperationException` if casting fails in `CreateViewData`.
  - Added `method != null` guard and null-coalescing on `jsResult?.ToString() ?? ""`.

### 5. Assembly Type Loading Resilience in FunqControllerFactory
- **Issue**: `FunqControllerFactory` called `assembly.GetTypes()` without guarding against `ReflectionTypeLoadException`, causing runtime failures if an assembly contained types with missing dependency references. Additionally, `assemblies` was not guarded against null.
- **Fix**: Guarded `(assemblies ?? TypeConstants<Assembly>.EmptyArray)` and caught `ReflectionTypeLoadException`, safely retrieving non-null types from `ex.Types`.

### 6. Compiler Warning Suppression
- **Fix**: Added `<NoWarn>$(NoWarn);CS8002</NoWarn>` to the `net472` build to suppress strong-naming warnings from referenced assemblies, and `<SuppressTfmSupportBuildWarnings>true</SuppressTfmSupportBuildWarnings>` to `net6.0`.
