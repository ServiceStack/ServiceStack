# Security and Hardening Improvements in ServiceStack

## Summary
This document summarizes modernization, null-safety, reliability, and bug fixes applied to the core `ServiceStack` framework library (`ServiceStack.csproj`).

---

## 1. Request Pipeline & Resolver Safety
- **`RequestExtensions.cs`**:
  - **`TryResolveInternal<T>`**: Guarded against null resolvers when `request is IHasResolver hasResolver` has a null `Resolver` or when `Service.GlobalResolver` is null, ensuring `TryResolve` returns `default` without throwing `NullReferenceException`.
  - **`GetRuntimeConfig<T>`**: Added `HostContext.AppHost != null` check so it safely returns `defaultValue` when AppHost is uninitialized.
  - **`RegisterForDispose`**: Safely verified `request.OriginalRequest is Microsoft.AspNetCore.Http.HttpRequest` before casting, falling back cleanly to `request.SetItem` rather than throwing `InvalidCastException` for mock, basic, or non-ASP.NET Core requests.

---

## 2. Host Context & AppHost Lifecycle
- **`HostContext.cs`**:
  - Added backing field `testMode` to support setting `HostContext.TestMode = true` in standalone unit test environments without requiring a full running AppHost.
  - Added null guard in `GetDefaultNamespace()` when `ServiceStackHost.Instance == null` to prevent `AssertAppHost()` from throwing `ConfigurationErrorsException`.
  - In `Reset()`, reset static `testMode = null` and `defaultOperationNamespace = null` so unit test state does not leak across AppHost lifecycles.
  - Added null-conditional access to `VirtualFileSources` accessors (`FileSystemVirtualFiles`, `MemoryVirtualFiles`, `GistVirtualFiles`).
- **`ServiceStackHost.cs`**:
  - Wrapped individual callbacks in `OnDisposeCallbacks` in try-catch blocks to prevent a failing callback from aborting container disposal, static state cleanup, and event unsubscription.

---

## 3. Session & Service Resilience
- **`ServiceExtensions.cs`**:
  - Fixed inverted condition bug in `SessionAs<TUserSession>` and `SessionAsAsync<TUserSession>` where `if (!Equals(mockSession, default(TUserSession)))` mistakenly discarded resolved mock sessions. Aligned with correct pattern `if (Equals(mockSession, default(TUserSession)))`.
  - Guarded against null service/request in `GetSessionId` and null AppHost in cache accessors.
- **`Service.cs`**:
  - Added safe navigation to `ServiceStackHost.Instance?.Container` in `GetResolver()`.
  - Guarded `GetService<T>()`, `GetRequiredService<T>()`, and `GetServices<T>()` against uninitialized `Request`.
- **`SessionFeature.cs`**:
  - Safely checked `session is IAuthSession authSession` and `HostContext.AppHost != null` before invoking `OnSessionFilter` in `GetOrCreateSession` and `GetOrCreateSessionAsync`, avoiding `InvalidCastException` when custom non-`IAuthSession` types are stored.

---

## 4. Real-Time & Messaging Hardening
- **`ServerEventsFeature.cs`**:
  - Implemented `IAsyncDisposable` on `MemoryServerEvents` (`ValueTask IAsyncDisposable.DisposeAsync()`) for modern asynchronous disposal workflows.
- **`Messaging/BackgroundMqService.cs`**:
  - In `GetStats()`, guarded `lock (workers)` to ensure `workers != null` before locking, avoiding `NullReferenceException` on `lock(null)`.
  - In `BackgroundMqWorker.Stop()`, added `cts?.Cancel()` to prevent NRE if worker is stopped after disposal.
  - In `BackgroundMqWorker.RunAsync`, added null-check on `cts != null && !cts.IsCancellationRequested`.

---

## 5. Web, HTTP, and Proxy Hardening
- **`HttpResult.cs`**:
  - In `DeleteCookie`, guarded retrieval of cookies from `req?.Response as IHttpResponse` to prevent `InvalidCastException` or NRE when using mock or basic requests.
- **`Host/Cookies.cs`**:
  - In `UseSecureCookie`, replaced `HostContext.Config?.UseSecureCookies` with `HostContext.AppHost?.Config?.UseSecureCookies` to avoid asserting AppHost presence, and added null-safe check `httpRes.Request?.IsSecureConnection == true`.
- **`Testing/MockHttpResponse.cs`**:
  - In constructor, changed `HostContext.AssertAppHost().GetCookies(this)` to `HostContext.AppHost?.GetCookies(this) ?? new Cookies(this)` to allow creating mock responses in isolated tests without an active AppHost.
- **`HttpResponseExtensionsInternal.cs`**:
  - Set `Content-Length` header on `ReadOnlyMemory<byte>` responses matching `byte[]` behavior.
  - Added support for `Memory<byte>` payloads.
- **`ProxyFeature.cs`**:
  - Avoided disposing caller's `httpReq.InputStream` directly; only dispose transformed intermediate streams.
  - Handled `WebException` when `webEx.Response` is null (e.g., DNS error, timeout), writing `502 Bad Gateway` instead of silent return.
- **`ServiceRoutesExtensions.cs`**:
  - In `IsSubclassOfRawGeneric`, added `toCheck != null` in while loop so inspecting interface types (where `BaseType` becomes `null`) does not throw `NullReferenceException`.
  - In `PropertyName`, safely unwrapped lambda unary operands and member expressions.
- **`CommandsFeature.cs`**:
  - Added null guard to `Median` extension method (`if (nums == null) return 0;`).
- **`ServiceStackDiagnostics.cs`**:
  - Safe-checked `listener?.IsEnabled(name) == true` in `Supports`.

---

## 6. Multi-Targeting & Compilation
- **`ServiceStack.csproj`**:
  - Standardized target framework define constants (`NET6_0`, `NET8_0`, `NET10_0`).
