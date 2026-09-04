# Security & Hardening Changes: ServiceStack.ImageSharp

## Overview
This document summarizes the stream lifecycle bug fixes, crash prevention, input validation hardening, and performance improvements implemented in `ServiceStack.ImageSharp`.

---

### 1. Fix Unintended Caller Stream Disposal
- **Severity**: High / Resource Management Bug
- **Issue**:
  - `ImageSharpImageProvider.Resize(Stream stream, ...)` contained `using var inputStream = stream;`.
  - When the method exited, it called `.Dispose()` on the caller's input stream. This destroyed the caller's stream (such as uploaded file streams or reusable memory streams), preventing subsequent reads or inspections.
- **Remediation**:
  - Removed `using var inputStream = stream;`. The provider reads from `stream` without hijacking or disposing caller-owned streams.

---

### 2. Stream Rewind & Pre-Validation
- **Severity**: Medium / Crash Resilience
- **Issue**:
  - If a stream was passed with `Position > 0` (e.g. from previous reads or writes), ImageSharp failed to decode the image header from the stream offset, throwing image format exceptions.
  - Passing `stream == null` caused an unhandled `NullReferenceException` inside ImageSharp.
- **Remediation**:
  - Added early null guard: `if (stream == null) throw new ArgumentNullException(nameof(stream));`.
  - Automatically rewinds seekable streams: `if (stream.CanSeek && stream.Position != 0) stream.Position = 0;`.

---

### 3. Dimension Validation & Explicit Resizing Mode
- **Severity**: Medium / Robustness & API Consistency
- **Issue**:
  - Passing non-positive dimensions (`newWidth <= 0` or `newHeight <= 0`) threw internal library exceptions with non-standard parameter names.
  - Calling `image.Mutate(i => i.Resize(newWidth, newHeight))` without explicit `ResizeOptions` risked ambiguous resize modes across library updates.
- **Remediation**:
  - Added explicit dimension validation throwing `ArgumentOutOfRangeException` for `newWidth <= 0` and `newHeight <= 0`.
  - Configured explicit `ResizeOptions` with `Mode = ResizeMode.Crop` and `Position = AnchorPositionMode.Center`, ensuring exact aspect-ratio-preserving center crops consistent with `ServiceStack.Desktop` and `ServiceStack.Skia`.

---

### 4. Memory Stream Pooling
- **Severity**: Low / Performance & Allocation Reduction
- **Issue**:
  - Every resize operation allocated an unpooled `new MemoryStream()`.
- **Remediation**:
  - Switched to ServiceStack's pooled `MemoryStreamFactory.GetStream()` to reduce garbage collection pressure under high-throughput image processing.

---

### 5. Multi-Targeting & Modernization
- **Severity**: Quality / Modern .NET Compatibility
- **Issue**:
  - `ServiceStack.ImageSharp.csproj` targeted only `net6.0;net8.0`, missing modern `net10.0`.
  - Contained a typo in `<PackageDescription>`: `"Imagigng"`.
- **Remediation**:
  - Added `net10.0` target framework to `ServiceStack.ImageSharp.csproj` with appropriate conditional compilation constants (`NET10_0_OR_GREATER`).
  - Corrected spelling to `"Imaging"`.
  - Added `ImageSharpExtensions` extension method (`image.ResizeToPng(newWidth, newHeight)`) for enhanced developer ergonomics.
