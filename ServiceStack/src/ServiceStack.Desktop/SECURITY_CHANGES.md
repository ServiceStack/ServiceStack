# Security & Hardening Changes: ServiceStack.Desktop

## Overview
This document summarizes the security hardening, resource lifecycle management, unmanaged handle cleanup, clipboard crash resilience, and cross-platform compatibility improvements implemented in `ServiceStack.Desktop`.

---

### 1. Fix Empty HTTP Response Bug on Async Script Execution
- **Severity**: High / Functional Defect
- **Issue**:
  - In `DesktopFeature.cs`, `setResultAsync` serialized the evaluation result into a memory stream (`JsonSerializer.SerializeToStream(value, ms)`) but failed to reset its position (`ms.Position = 0;`) before copying to `base.Response.OutputStream`.
  - As a result, 0 bytes were copied and all asynchronous script evaluations returned empty HTTP response bodies to callers.
- **Remediation**:
  - Added `ms.Position = 0;` prior to `await ms.CopyToAsync(...)`, matching `SetResult` and `SetOutput`.

---

### 2. File Truncation & Overwrite Defect in Desktop File Services
- **Severity**: Medium / Data Integrity Bug
- **Issue**:
  - In `DesktopFileService.Put` and `DesktopDownloadUrlService.Any`, files were written using `new FileStream(..., FileMode.OpenOrCreate)`.
  - If a file already existed and was overwritten with smaller content, trailing remnant bytes from the previous file content remained intact at the end of the file.
- **Remediation**:
  - Changed `FileMode.OpenOrCreate` to `FileMode.Create`, ensuring previous file contents are properly truncated and cleanly overwritten.

---

### 3. Path Traversal & File Name Injection Hardening
- **Severity**: High / Path Traversal (CWE-22)
- **Issue**:
  - `DesktopFileService.AssertFile` checked only for `".."` and `file.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0`.
  - On Unix/macOS, Windows-style path separators (`'\\'`) and colons (`':'`) are not included in `Path.GetInvalidFileNameChars()`, allowing malicious paths containing alternate directory delimiters.
- **Remediation**:
  - Hardened `AssertFile` to explicitly validate against `'/'`, `'\\'`, `':'`, and null characters on all platforms in addition to `Path.GetInvalidFileNameChars()` and `file.IndexOf("..", StringComparison.Ordinal) >= 0`.

---

### 4. Process Injection & Shell Argument Escaping in NativeWin.Start
- **Severity**: High / Command Injection (CWE-78)
- **Issue**:
  - On Windows, `NativeWin.Start(string url)` executed `Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"))`.
  - URLs containing query string parameters (such as `https://example.com/login?param=1&token=abc`) were parsed by `cmd.exe`, which treats `&` as a shell command separator, attempting to execute the trailing parameters as commands.
- **Remediation**:
  - Changed Windows invocation to `Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })`, avoiding `cmd.exe` shell parsing entirely and delegating safely to Windows ShellExecute.

---

### 5. GDI+ Unmanaged Handle Leak & Image Crop Defect in ImageProvider
- **Severity**: Medium / Resource Leak & Functional Defect
- **Issue**:
  - In `ImageProvider.Resize`, `Graphics.FromImage(newImage)` allocated an unmanaged GDI+ graphics context without disposing it, leaking native GDI handles on Windows.
  - In `ResizeToPng(this Image img, int newWidth, int newHeight)`, the code calculated scaled dimensions and created `newImage`, but then returned `CropToPng(img, ...)` on the unscaled original image rather than `newImage`. This caused the corner of the original image to be cropped instead of the resized image.
- **Remediation**:
  - Wrapped `Graphics.FromImage(newImage)` in `using (var g = ...)` with high-quality rendering hints.
  - Corrected `ResizeToPng` to return `CropToPng(newImage, ...)` and safely dispose `newImage`.
  - Added input stream validation, non-positive dimension guards, and stream rewinding (`stream.Position = 0`).

---

### 6. Clipboard Permanent Lock Prevention & Null Safety
- **Severity**: Medium / Denial of Service & Crash Resilience
- **Issue**:
  - In `NativeWin.SetStringInClipboard(string text)`, if `text` was `null`, dereferencing `text.Length` threw `NullReferenceException` after `TryOpenClipboard()` had opened the clipboard, leaving the operating system clipboard locked indefinitely for all processes.
- **Remediation**:
  - Added `if (text == null) text = string.Empty;` before opening the clipboard.
  - Ensured null terminator (`\0`) is explicitly written to the allocated global memory.
  - Added null check on `Marshal.PtrToStringAuto` in `SHGetPathFromIDListLongPath`.

---

### 7. KnownFolders Case-Insensitive Lookup & Non-Windows Fallback
- **Severity**: Low / Reliability & Cross-Platform Resilience
- **Issue**:
  - `KnownFolders.Map` used case-sensitive dictionary lookups, failing on lowercase or mixed-case folder requests.
  - On non-Windows platforms (macOS/Linux), calling `SHGetKnownFolderPath` threw `DllNotFoundException` attempting to load `Shell32.dll`.
- **Remediation**:
  - Configured `KnownFolders.Map` with `StringComparer.OrdinalIgnoreCase`.
  - Added cross-platform fallback resolving known folders (Desktop, Documents, Music, Pictures, Videos, Downloads) using `Environment.GetFolderPath` on non-Windows platforms.

---

### 8. Multi-Targeting Modernization & CA1416 Suppression
- **Severity**: Quality / Modern .NET Compatibility
- **Issue**:
  - Project lacked conditional compilation constants for `NET6_0_OR_GREATER`, `NET8_0_OR_GREATER`, and `NET10_0_OR_GREATER`.
  - Windows-specific P/Invoke calls and `System.Drawing` APIs raised platform compatibility warnings (CA1416) on non-Windows target platforms.
- **Remediation**:
  - Added target framework constants and enabled nullable reference types.
  - Suppressed CA1416 warnings cleanly in `.csproj` with `<NoWarn>$(NoWarn);CA1416</NoWarn>`.
  - Fixed typos in `<PackageTags>` metadata.
