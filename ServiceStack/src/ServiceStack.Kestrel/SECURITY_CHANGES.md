# Security and Reliability Remediation: `ServiceStack.Kestrel`

## Summary
Audit and hardening of `ServiceStack.Kestrel` for .NET Core / ASP.NET Core Kestrel self-hosting, resolving index bounds vulnerabilities in URL parsing, safe exception handling during request initialization failures, correct nullability contracts, and dependency cleanup.

---

## Remediations

### 1. Robust URL and PathBase Parsing
- **Issue**: `ParsePathBase(string urlBase)` used fixed-length index offset arithmetic (`urlBase.IndexOf('/', "https://".Length)`). When passed short URLs or malformed URLs (e.g. `"http://a"` or `"http://"`), it threw `ArgumentOutOfRangeException`.
- **Fix**: Replaced index offset arithmetic with safe scheme boundary detection (`urlBase.IndexOf("://", StringComparison.Ordinal)`) and checked `startIndex < urlBase.Length` prior to scanning for the root path separator.

### 2. Request Initialization Error Response Guard
- **Issue**: In `ProcessRequest`, when an exception occurred during request initialization, the fallback handler attempted to set `context.Response.ContentType` and write to the response stream without checking if the response had already started streaming (`context.Response.HasStarted`).
- **Fix**: Wrapped response write operations in `if (!context.Response.HasStarted)` to prevent secondary `InvalidOperationException` crashes in ASP.NET Core pipelines.

### 3. Null Safety and Contract Alignment in `TryGetCurrentRequest`
- **Issue**: `TryGetCurrentRequest()` directly accessed `app.ApplicationServices`, risking `NullReferenceException` if invoked before `Bind(app)` completed. Furthermore, it did not reflect the nullable return contract of base `ServiceStackHost.TryGetCurrentRequest()`.
- **Fix**: Updated signature to `public override IRequest? TryGetCurrentRequest()`, safely queried `app?.ApplicationServices.GetService<IHttpContextAccessor>()`, and guarded against uninitialized or unavailable accessor contexts.

### 4. Unnecessary Package Dependency Pruning
- **Issue**: `ServiceStack.Kestrel.csproj` had an explicit package reference to `System.Memory` (v4.6.3), causing NuGet pruning warning `NU1510` across modern .NET target frameworks.
- **Fix**: Removed explicit `System.Memory` package reference since it is an in-box framework assembly in `Microsoft.NETCore.App` / `Microsoft.AspNetCore.App` across .NET 6, 8, and 10. Added `<SuppressTfmSupportBuildWarnings>true</SuppressTfmSupportBuildWarnings>` for `net6.0` to eliminate transitive NuGet package target warnings.
