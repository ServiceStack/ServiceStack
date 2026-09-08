# CONTEXT: ServiceStack.MsgPack

## Purpose

`ServiceStack.MsgPack` integrates the MessagePack binary serialization format into ServiceStack via `MsgPack.Cli`.

It delivers:
- **`application/x-msgpack` Format**: Transparent serialization and deserialization of request/response DTOs using MessagePack's efficient binary representation.
- **Typed Binary Client (`MsgPackServiceClient`)**: Fast REST client communicating with ServiceStack endpoints using MessagePack payloads.
- **In-Memory Helpers (`MsgPackExtensions`)**: Extension methods for converting objects to and from MessagePack byte arrays.

**Target frameworks**: `net472;net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack.Interfaces     ServiceStack.Client     MsgPack.Cli
         │                          │                     │
         └──────────────────────────┼─────────────────────┘
                                    ▼
                          ServiceStack.MsgPack
                                    │
           ┌────────────────────────┴────────────────────────┐
           ▼                                                 ▼
Server Plugin (MsgPackFormat)                 Typed Client (MsgPackServiceClient)
(application/x-msgpack Content-Type)          (Binary REST client for MsgPack)
```

- **Depends on**: `ServiceStack`, `ServiceStack.Interfaces`, `ServiceStack.Client`, `ServiceStack.Common`, `ServiceStack.Text`, `MsgPack.Cli` (v1.0.1).
- **Depended on by**: ServiceStack applications and clients requiring compact binary serialization with JSON-like dynamic object schemas.
- **Alternative Binary Formats**: `ServiceStack.ProtoBuf` (Protocol Buffers).

---

## Key Functionality

### Primary Types
| Class | Role |
|---|---|
| `MsgPackFormat` | Core plugin registering `application/x-msgpack` with ServiceStack's `IContentTypeFilter`. Manages type serialization adapters (`IMsgPackType`). |
| `MsgPackServiceClient` | Service client sending and receiving MessagePack payloads over HTTP. |
| `MsgPackExtensions` | Helper extensions: `obj.ToMsgPack()` and `bytes.FromMsgPack<T>()`. |

---

## Architecture & Design Patterns

### Lock-Free Copy-On-Write Type Cache
`MsgPackFormat.GetMsgPackType` caches compiled type serializers (`IMsgPackType`) using a thread-safe copy-on-write snapshot strategy (`Interlocked.CompareExchange`).

### Non-Owning Stream Wrappers
When wrapping streams with `Packer` or `Unpacker`, `ownsStream: false` is used so serializer disposal flushes internal buffers without closing the underlying network or response stream.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Adhere to these principles:

### 1. Stack Trace Preservation on Exceptions
- In `MsgPackFormat.HandleException`, never use `throw ex;` (which resets the call stack). Use `ExceptionDispatchInfo.Capture(ex).Throw();` to preserve full debugging context.

### 2. Stream Ownership and Deterministic Disposal
- Writers (`Packer`) and readers (`Unpacker`) must be disposed deterministically in `using var` blocks with `ownsStream: false`.

### 3. Copy-on-Write Snapshot Atomicity
- In `GetMsgPackType`, clone the local `snapshot` variable rather than reading the volatile static field `msgPackTypeCache` directly during dictionary cloning.

### 4. Null Safety in Serializers & Extensions
- `MsgPackFormat.Serialize` must return early if `dto == null || outputStream == null`.
- `MsgPackExtensions.ToMsgPack` returns `TypeConstants.EmptyByteArray` directly for null inputs.
- `MsgPackExtensions.FromMsgPack<T>` returns `default(T)` for null or empty byte arrays.

### 5. Unified Client Deserialization
- `MsgPackServiceClient.DeserializeFromStream<T>` must delegate to `MsgPackFormat.Deserialize<T>(stream)` so that custom collection conversions and empty DTO normalization behave identically across client and server.

---

## Common Modification Scenarios

1. **Registering the MsgPack Plugin on the Server**
   ```csharp
   Plugins.Add(new MsgPackFormat());
   ```

2. **Consuming MsgPack Services from .NET**
   ```csharp
   var client = new MsgPackServiceClient(baseUrl);
   var response = await client.GetAsync(new MyRequest());
   ```
