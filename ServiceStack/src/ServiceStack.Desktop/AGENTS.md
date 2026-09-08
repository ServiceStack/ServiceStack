# CONTEXT: ServiceStack.Desktop

## Purpose

`ServiceStack.Desktop` enables building native desktop applications using web technologies (HTML/CSS/JS) hosted inside native WebView windows (e.g., CefSharp / Chromium Embedded Framework on Windows). It acts as the bridge between a web-based front-end and native OS capabilities — providing file system access, clipboard integration, shell operations, image processing, and async JavaScript evaluation from the native host.

It is **not** a general-purpose library; it is a focused add-on for the specific pattern of shipping a ServiceStack-backed app as a local desktop executable where the UI is served from an embedded HTTP server and rendered in a native browser control.

---

## Role in ServiceStack Ecosystem

| Relationship | Detail |
|---|---|
| **Depends on** | `ServiceStack` core (IPlugin, IRequest, HttpResult, ScriptMethods, etc.) |
| **Consumed by** | Desktop app hosts (typically a WinForms/WPF host embedding CefSharp or a similar WebView) |
| **Does NOT depend on** | ServiceStack.OrmLite, ServiceStack.Redis, ServiceStack.Auth, or other service libraries |

The library registers itself as a ServiceStack plugin (`IPlugin`). Once added via `AppHost.Plugins.Add(new DesktopFeature())`, it wires up HTTP endpoints and script methods accessible from within the hosted web UI.

---

## Key Functionality

### `DesktopFeature` (IPlugin)
- Registers desktop-specific HTTP service routes (`/desktop/...`).
- Adds `DesktopScriptMethods` to the script context, making native operations callable from `#Script` templates.
- Supports async script evaluation: the web UI can invoke a script expression and receive a serialized result back over HTTP.
- Result communication uses a shared `MemoryStream`; **must reset `ms.Position = 0`** before copying to the response stream, otherwise the response body is empty (see Security section).

### `DesktopFileService`
Handles HTTP requests from the web UI to read and write files on the local file system.

- **Read**: Returns file bytes/text for an allowed path.
- **Write**: Persists content to disk using **`FileMode.Create`** (not `FileMode.OpenOrCreate`) to guarantee file truncation when overwriting with shorter content.
- **Path validation**: Delegates to `AssertFile`, which must reject any path containing traversal characters (`/`, `\\`, `:`, `\0`) in addition to `Path.GetInvalidFileNameChars()`.

### `DesktopDownloadUrlService`
Downloads a remote URL and saves it to a local file or returns the content, initiated from the web UI.

### `NativeWin` (Windows-only)
P/Invoke wrappers for Windows-specific native APIs:

| Member | Purpose |
|---|---|
| `SetStringInClipboard(string)` | Write text to the Windows clipboard via `OpenClipboard`/`SetClipboardData` |
| `GetStringFromClipboard()` | Read text from the Windows clipboard |
| `SHGetPathFromIDListLongPath` | Resolve a shell PIDL to a long file system path |
| `Start(url)` | Launch a URL or process using `ProcessStartInfo { UseShellExecute = true }` |

> **Critical**: `Start(url)` must use `UseShellExecute = true`. The old `cmd.exe /c start {url}` pattern allows command injection via `&` in URLs.

### `KnownFolders`
Bridges the Windows `SHGetKnownFolderPath` API (known folder GUIDs like `Downloads`, `Pictures`, `Desktop`) with a cross-platform fallback using `Environment.GetFolderPath`.

- Folder name lookups must use **`StringComparer.OrdinalIgnoreCase`** for reliable matching.
- On non-Windows targets, falls back gracefully rather than throwing a `PlatformNotSupportedException`.

### `ImageProvider`
Image resize and crop operations using `System.Drawing` (GDI+):

| Method | Behaviour |
|---|---|
| `ResizeToPng(img, w, h)` | Resize image to fit within `w x h`, return PNG bytes |
| `CropToPng(img, x, y, w, h)` | Crop a region of an image, return PNG bytes |

- **`Graphics` objects** from `Graphics.FromImage(...)` must be wrapped in `using` to avoid GDI+ handle leaks.
- **`ResizeToPng`** must apply the crop to `newImage` (the resized copy), not to the original `img`. Cropping the unscaled original is a latent bug.

---

## Architecture & Design Patterns

### Plugin Registration
`DesktopFeature` implements `IPlugin.Register(IAppHost host)`. All routes and script methods are registered during `AppHost.Init()`. No special startup order is required beyond adding the plugin before `Init()` is called.

### HTTP Services (Request/Response DTOs)
Desktop operations are exposed as ordinary ServiceStack services:
```
POST /desktop/file          => DesktopFileService  (read/write local files)
POST /desktop/download-url  => DesktopDownloadUrlService
```
These services are only meaningful when the host is `localhost`; they should not be exposed over a network.

### Script Methods (`DesktopScriptMethods`)
Extends `ScriptMethods` so that `#Script` pages and templates rendered in the desktop UI can call native operations (clipboard read/write, folder resolution, image ops) directly from script expressions.

### Async Result Channel
The desktop host (native side) can evaluate a script expression asynchronously and poll or await the result via a shared `MemoryStream`/`TaskCompletionSource` pair managed inside `DesktopFeature`. The result is serialized (typically JSON) into the stream and flushed to the HTTP response.

### Multi-Target Build
```
<TargetFrameworks>net472;net6.0;net8.0;net10.0</TargetFrameworks>
```
- Windows-only code (P/Invoke, GDI+, clipboard) is guarded by `#if WINDOWS` preprocessor symbols and/or `[SupportedOSPlatform("windows")]` attributes.
- `net472` targets use `#if NET472` guards where API differences exist.
- Cross-platform paths must compile and run cleanly on Linux/macOS (used in dev/test scenarios even if the final app ships only on Windows).

---

## Security & Reliability Considerations

These are the highest-priority areas to audit whenever modifying this project:

### Path Traversal (HIGH)
**`AssertFile` / file service path validation**

`Path.GetInvalidFileNameChars()` on Windows **does not include `/`** and does not include `\` on some runtimes. An attacker-controlled path like `../../etc/passwd` or `..\Windows\System32\config\SAM` passes naive validation.

**Required checks** — reject any path that contains:
- `/` (Unix separator, valid in Windows filenames by `GetInvalidFileNameChars`)
- `\\` (Windows separator)
- `:` (drive letter separator / alternate data streams)
- `\0` (null byte — used to truncate paths in some native APIs)

### Command Injection (HIGH)
**`NativeWin.Start(url)`**

Launching URLs via `cmd.exe /c start {url}` interprets `&` as a shell command separator. A URL containing `& del /f /q C:\important` will execute the second command.

**Fix**: Always use `new ProcessStartInfo(url) { UseShellExecute = true }`. This passes the URL directly to the OS shell handler without cmd.exe parsing.

### Empty HTTP Response (HIGH)
**`setResultAsync` / async result stream**

After writing the result into a `MemoryStream`, the stream position is at the end. Copying it to the response without resetting produces a zero-byte response body.

**Fix**: Always call `ms.Position = 0` (or `ms.Seek(0, SeekOrigin.Begin)`) before `ms.CopyTo(responseStream)`.

### File Truncation Bug
**`DesktopFileService` write path**

`FileMode.OpenOrCreate` leaves trailing bytes from the previous content when the new content is shorter. A 100-byte file overwritten with 50 bytes results in a 100-byte file with corrupt trailing data.

**Fix**: Use `FileMode.Create`, which always truncates to zero before writing.

### GDI+ Handle Leak
**`ImageProvider` — `Graphics.FromImage`**

`Graphics` implements `IDisposable`. Failing to dispose it leaks a GDI+ handle per call. Under load (e.g., batch image processing) this exhausts the GDI handle pool and causes `OutOfMemoryException` from GDI+.

**Fix**: Always wrap in `using (var g = Graphics.FromImage(newImage)) { ... }`.

### Image Crop Applied to Wrong Image
**`ResizeToPng`**

If `ResizeToPng` internally calls `CropToPng(img, ...)` using the original `img` rather than the resized `newImage`, the crop operates on the full-resolution source. This returns incorrectly sized output and wastes memory holding the full original in scope.

**Fix**: Pass `newImage` (the scaled copy) to the crop call.

### Clipboard OS Lock
**`NativeWin.GetStringFromClipboard`**

Calling `OpenClipboard()` when the intended text value is `null` (e.g., no text on clipboard) and then failing to call `CloseClipboard()` in all error paths can permanently lock the Windows clipboard for all applications until reboot.

**Fix**: Check `text == null` **before** calling `OpenClipboard()`. Ensure `CloseClipboard()` is always called in a `finally` block if the clipboard was successfully opened.

### KnownFolders Case Sensitivity
**`KnownFolders` name lookup**

Folder name lookups keyed on strings (e.g., `"downloads"`, `"Downloads"`, `"DOWNLOADS"`) must use `StringComparer.OrdinalIgnoreCase`. A case-sensitive dictionary silently returns `null` for mismatched casing, causing confusing null-reference failures downstream.

---

## Common Modification Scenarios

| Scenario | Files / Areas to Touch |
|---|---|
| Add a new native OS operation | `NativeWin.cs` (P/Invoke), `DesktopScriptMethods.cs` (script method), route registration in `DesktopFeature` |
| Expose a new HTTP endpoint to the web UI | Add Request/Response DTOs + `IService` implementation; register route in `DesktopFeature.Register` |
| Add a new Known Folder mapping | `KnownFolders.cs` — add GUID constant + dictionary entry with `OrdinalIgnoreCase` comparer |
| Extend image operations | `ImageProvider.cs` — ensure all `Graphics`/`Bitmap` objects are disposed; apply ops to the correct (possibly resized) image |
| Fix cross-platform build issues | Check `#if WINDOWS` / `[SupportedOSPlatform("windows")]` guards; provide `else` branch for non-Windows |
| Tighten file access security | `AssertFile` in `DesktopFileService.cs` — extend the deny-list of traversal characters |
| Change async result serialization | `DesktopFeature.cs` around the `MemoryStream` result channel — always reset position before read |
