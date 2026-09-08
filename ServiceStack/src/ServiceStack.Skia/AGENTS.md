# CONTEXT: ServiceStack.Skia

## Purpose

`ServiceStack.Skia` provides ultra-fast, hardware-accelerated 2D graphics and image processing for ServiceStack applications powered by Google's **SkiaSharp** engine.

It implements ServiceStack's imaging provider contract (`IImageProvider`), delivering:
- High-performance image resizing, cropping, and thumbnail generation.
- Production-ready native support for Linux (via bundled `SkiaSharp.NativeAssets.Linux`), macOS, and Windows.
- Center-crop aspect-ratio calculations matching other ServiceStack image providers.
- Direct-to-managed memory stream output using pooled buffers.

**Target frameworks**: `net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack (Core)          SkiaSharp (Google Skia)
         │                              │
         └──────────────┬───────────────┘
                        ▼
                ServiceStack.Skia
                        │
                        ▼
High-Performance Image Resizing & Avatar Generation
 (Includes native Linux assets for Docker containers)
```

- **Depends on**: `ServiceStack`, `ServiceStack.Interfaces`, `ServiceStack.Common`, `ServiceStack.Client`, `ServiceStack.Text`, `SkiaSharp`, `SkiaSharp.NativeAssets.Linux`.
- **Depended on by**: ServiceStack applications needing maximum image processing throughput and vector/raster graphical transformations.
- **Alternative**: `ServiceStack.ImageSharp` (pure-managed, zero native library dependencies).

---

## Key Functionality

### Primary Types
| Class | Role |
|---|---|
| `SkiaImageProvider` | Implements `IImageProvider`. Performs decoding, resizing, aspect-ratio center cropping, and PNG encoding. |
| `SkiaImageExtensions` | Extension methods on `SKBitmap` for cropping and resizing (`ResizeToPng`, `Crop`). |

---

## Architecture & Design Patterns

### Native Memory Management
`SkiaSharp` allocates unmanaged C++ memory inside the underlying Skia library. Every `SKBitmap`, `SKImage`, and `SKData` must be deterministically disposed. Intermediate bitmaps created during scaling and cropping are tracked and released inside `finally` blocks.

### Zero-Copy Memory Stream Pooling
Encoded image bytes from Skia's unmanaged `SKData` buffer are copied into managed pooled streams via `MemoryStreamFactory.GetStream()`, immediately allowing unmanaged Skia resources to be freed.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Always enforce these rules:

### 1. Unmanaged Native Memory Leaks
- **Risk**: Failing to dispose intermediate `SKBitmap` instances from `img.Resize(...)` and `Crop(...)` leaks unmanaged C++ memory, eventually exhausting system RAM under sustained image processing load.
- **Rule**: All intermediate bitmaps must be tracked and disposed in `finally` blocks.

### 2. Caller Bitmap Lifecycle Preservation
- In `Crop(SKBitmap img, ...)`, never call `img.Dispose()` on the input parameter; the caller owns the lifetime of the input image.

### 3. Safe Stream Decoding & Rewinding
- Always check `stream != null`, rewind seekable streams (`stream.Position = 0`), and verify that `SKBitmap.Decode(stream)` returned a non-null instance before accessing properties.

### 4. Dimension Bounds & Division by Zero
- Validate `newWidth > 0` and `newHeight > 0` (`ArgumentOutOfRangeException`).
- Ensure `img.Width > 0` and `img.Height > 0` before calculating aspect ratios to prevent `NaN` or `Infinity` arithmetic failures.

---

## Common Modification Scenarios

1. **Registering the Skia Image Provider**
   ```csharp
   container.Register<IImageProvider>(c => new SkiaImageProvider());
   ```

2. **Extending Output Formats (JPEG, WebP)**
   - Utilize `SKEncodedImageFormat.Jpeg` or `SKEncodedImageFormat.Webp` in `SKBitmap.Encode`.
   - Ensure the output stream is seekable and positioned at `Position = 0`.
