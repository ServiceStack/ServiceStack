# CONTEXT: ServiceStack.ImageSharp

## Purpose

`ServiceStack.ImageSharp` provides cross-platform 2D image resizing, transformation, and thumbnail generation for ServiceStack services using the pure-managed **SixLabors.ImageSharp** library.

It fulfills ServiceStack's imaging provider contract (`IImageProvider`), enabling automated image cropping, thumbnail generation for uploaded avatars/media, and on-the-fly image manipulation without native C++ dependencies or GDI+ (`System.Drawing`).

**Target frameworks**: `net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack (Core)       SixLabors.ImageSharp
         │                         │
         └────────────┬────────────┘
                      ▼
          ServiceStack.ImageSharp
                      │
                      ▼
Dynamic Image Resizing, Thumbnails & Upload Processing
```

- **Depends on**: `ServiceStack`, `ServiceStack.Interfaces`, `ServiceStack.Common`, `ServiceStack.Client`, `ServiceStack.Text`, `SixLabors.ImageSharp.Web`.
- **Depended on by**: ServiceStack applications handling media uploads, user profile avatars, and dynamic image delivery.
- **Sibling Imaging Providers**: `ServiceStack.Skia` (uses Google's SkiaSharp vector/raster library).

---

## Key Functionality

### Primary Types
| Class | Role |
|---|---|
| `ImageSharpImageProvider` | Core imaging provider implementing `IImageProvider`. Performs aspect-ratio-preserving center-crop image resizing into pooled memory streams. |
| `ImageSharpExtensions` | Convenience extension methods (such as `image.ResizeToPng(...)`) for mutating and encoding images directly. |

---

## Architecture & Design Patterns

### Pure Managed Image Processing
Avoids any unmanaged or OS-specific dependencies (`libgdiplus`), making it ideal for Linux containers (Docker), Alpine, macOS, and Windows.

### MemoryStream Pooling
Utilizes `MemoryStreamFactory.GetStream()` to rent buffers from ServiceStack's shared pool during encoding, preventing memory fragmentation under high-frequency image transformations.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Adhere to these principles:

### 1. Caller Stream Preservation
- **Risk**: Wrapping incoming streams in `using var inputStream = stream;` causes `.Dispose()` on the caller's input stream when the method exits, destroying uploaded file streams or request streams.
- **Rule**: Never dispose caller-owned input streams. Only read from them.

### 2. Stream Position Rewinding
- Always check and rewind seekable streams before image decoding (`if (stream.CanSeek && stream.Position != 0) stream.Position = 0;`), preventing format decoding crashes when streams are read multiple times.

### 3. Dimension Validation & Explicit Resizing
- Validate `newWidth > 0` and `newHeight > 0` (`ArgumentOutOfRangeException`).
- Use explicit `ResizeOptions` with `Mode = ResizeMode.Crop` and `Position = AnchorPositionMode.Center` to guarantee deterministic cropping across ImageSharp releases.

---

## Common Modification Scenarios

1. **Registering the Image Provider**
   ```csharp
   container.Register<IImageProvider>(c => new ImageSharpImageProvider());
   ```

2. **Customizing Output Formats / Compression**
   - Override or extend `ImageSharpImageProvider.Resize` to support JPEG, WebP, or PNG encoding options.
