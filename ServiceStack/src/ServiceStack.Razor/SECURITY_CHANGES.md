# Security & Hardening Changes: ServiceStack.Razor

## 1. XSS Vulnerability Fix in `ViewPageBase.GetErrorHtml`
- **Location**: `src/ServiceStack.Razor/ViewPageBase.cs`
- **Vulnerability**: Error details from `ResponseStatus` (`ErrorCode`, `Message`, and `StackTrace`) were concatenated directly into raw HTML strings returned by `GetErrorHtml(ResponseStatus responseStatus)`. If error responses reflected unvalidated user inputs (such as bad query parameters, invalid model fields, or custom exceptions), this exposed applications using Razor error rendering to Reflected and Stored Cross-Site Scripting (XSS).
- **Remediation**:
  - Applied `HttpUtility.HtmlEncode()` to `responseStatus.ErrorCode`, `responseStatus.Message`, and `responseStatus.StackTrace`.
  - Escaped error content ensures safe rendering inside HTML `<h4>` and `<pre>` elements.

## 2. Timing Attack Defense & Modern PRNG in Anti-XSRF Protection
- **Location**: `src/ServiceStack.Razor/Html/AntiXsrf/BinaryBlob.cs`
- **Issue**:
  - `CryptoUtil.AreByteArraysEqual` used a standard boolean bitwise AND (`areEqual &= (a[i] == b[i])`), which produces branching bytecode vulnerable to timing variations and JIT compiler optimizations.
  - Legacy `RNGCryptoServiceProvider` was utilized for random byte generation.
  - SHA256 initialization attempted CNG on platforms where CNG is unavailable or threw `PlatformNotSupportedException`.
- **Remediation**:
  - Replaced the boolean loop in `AreByteArraysEqual` with a constant-time XOR accumulator:
    ```csharp
    int diff = 0;
    for (int i = 0; i < a.Length; i++)
    {
        diff |= a[i] ^ b[i];
    }
    return diff == 0;
    ```
  - Replaced `RNGCryptoServiceProvider` with thread-safe, modern `RandomNumberGenerator.Create()`.
  - Replaced platform-conditional SHA256 instantiation with robust `SHA256.Create()`.

## 3. Concurrency Safety in View and Page Caches
- **Location**: `src/ServiceStack.Razor/Managers/RazorViewManager.cs`
- **Issue**:
  - `RazorViewManager.Pages` and `ViewNamesMap` were non-thread-safe `Dictionary<string, ...>`.
  - Background `FileSystemWatcher` threads in `FileSystemWatcherLiveReload` modified `Pages` concurrently with incoming HTTP requests resolving views (`GetPage`, `GetViewPage`, `GetContentPage`), risking corrupted dictionary state and `InvalidOperationException` or infinite loops during dictionary rehash.
- **Remediation**:
  - Converted `Pages` to `ConcurrentDictionary<string, RazorPage>` and `ViewNamesMap` to `ConcurrentDictionary<string, string>`.
  - Updated `FileSystemWatcherLiveReload` to safely use `TryRemove(path, out _)`.

## 4. Unhandled ReflectionTypeLoadException in Assembly Scanning
- **Location**: `src/ServiceStack.Razor/Managers/RazorViewManager.cs`
- **Issue**: Calling `assembly.GetTypes()` during startup precompilation threw unhandled `ReflectionTypeLoadException` whenever an assembly referenced missing optional dependencies, aborting application initialization.
- **Remediation**: Added a `try/catch (ReflectionTypeLoadException ex)` block to recover loaded types (`ex.Types.Where(t => t != null).ToArray()`) and added a null check for `Config.LoadFromAssemblies`.

## 5. Resource Leak Cleanup in `FileSystemWatcherLiveReload` and `RazorFormat`
- **Location**: `src/ServiceStack.Razor/Managers/FileSystemWatcherLiveReload.cs`, `src/ServiceStack.Razor/RazorFormat.cs`
- **Issue**: `FileSystemWatcherLiveReload` held active `FileSystemWatcher` instances and event subscriptions without implementing `IDisposable`, leaking unmanaged directory handles and event handler references.
- **Remediation**:
  - Implemented `IDisposable` on `FileSystemWatcherLiveReload` to unsubscribe events, disable notifications (`EnableRaisingEvents = false`), and dispose the underlying watcher.
  - Implemented `IDisposable` on `RazorFormat` to cascade disposal to `LiveReload`.

## 6. Stream and StreamWriter Resource Management
- **Location**: `src/ServiceStack.Razor/Managers/RazorPageResolver.cs`, `src/ServiceStack.Razor/RazorPageExtensions.cs`
- **Issue**:
  - `StreamWriter` allocations in `ExecuteRazorPage` and `ExecuteRazorPageWithLayout` were not wrapped in `using` statements, and buffered writers were not guaranteed to flush to the underlying streams.
  - In `ExecuteRazorPageWithLayout`, `childWriter` was read via `ms.ReadToEnd()` without explicit flushing.
- **Remediation**:
  - Scoped all `StreamWriter` instances in `using` blocks with `leaveOpen: true` (or explicit flushing) using consistent BOM-less UTF-8 encoding.
  - Added explicit `childWriter.Flush()` before `ms.ReadToEnd()` in layout execution.

## 7. Thread Safety in `CompilerServices.IncludeAssemblies`
- **Location**: `src/ServiceStack.Razor/Compilation/CompilerServices.cs`, `src/ServiceStack.Razor/RazorFormat.cs`
- **Issue**: `IncludeAssemblies` static list was concurrently enumerated in `GetLoadedAssemblies()` and modified by `RazorFormat` instances without synchronization.
- **Remediation**: Synchronized `GetLoadedAssemblies()` snapshot under `lock (IncludeAssemblies)` and deduplicated assembly registration in `RazorFormat`.

## 8. Null Safety Hardening
- **Location**: `src/ServiceStack.Razor/DynamicRequestObject.cs`, `src/ServiceStack.Razor/Html/AntiXsrf/AntiForgeryTokenSerializer.cs`, `src/ServiceStack.Razor/Compilation/RazorPageHost.cs`
- **Remediation**:
  - Added null checks in `DynamicRequestObject.TryGetMember` (`httpReq?.GetParam`) and `DynamicDictionary.TryGetItem` (`page?.ChildPage`).
  - Added argument validation in `AntiForgeryTokenSerializer.Serialize` verifying non-null `token` and `token.SecurityToken`.
  - Hardened compiled type discovery in `RazorPageHost` to prefer `IRazorView` types over compiler-generated helper types.
