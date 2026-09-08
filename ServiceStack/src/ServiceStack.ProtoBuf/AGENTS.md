# CONTEXT: ServiceStack.ProtoBuf

## Purpose

`ServiceStack.ProtoBuf` adds native Google Protocol Buffers binary serialization support to ServiceStack. It provides both a server-side content-type plugin (`ProtoBufFormat`) and a strongly-typed HTTP service client (`ProtoBufServiceClient`) utilizing Marc Gravell's **`protobuf-net`**.

Key capabilities:
- **`application/x-protobuf` Format**: Automatically serialize and deserialize request and response DTOs using compact Protocol Buffers binary format.
- **Code-First Serialization**: Leverages existing DTO classes with `[DataContract]` / `[DataMember]` attributes without needing `.proto` files.
- **Typed Binary Client**: `ProtoBufServiceClient` for client-server communication with minimal payload size and low serialization overhead.

**Target frameworks**: `net472;net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack.Interfaces     ServiceStack.Client     protobuf-net
         │                          │                     │
         └──────────────────────────┼─────────────────────┘
                                    ▼
                         ServiceStack.ProtoBuf
                                    │
           ┌────────────────────────┴────────────────────────┐
           ▼                                                 ▼
Server Plugin (ProtoBufFormat)                Typed Client (ProtoBufServiceClient)
(application/x-protobuf Content-Type)         (Low-latency binary REST client)
```

- **Depends on**: `ServiceStack`, `ServiceStack.Interfaces`, `ServiceStack.Client`, `ServiceStack.Common`, `ServiceStack.Text`, `protobuf-net` (v3.2.56).
- **Depended on by**: Bandwidth-constrained or high-throughput distributed systems seeking binary efficiency over JSON.
- **Alternative Binary Formats**: `ServiceStack.MsgPack` (MessagePack format).

---

## Key Functionality

### Primary Types
| Class | Role |
|---|---|
| `ProtoBufFormat` | ServiceStack plugin registering `application/x-protobuf` with `IContentTypeFilter`. Exposes the underlying `RuntimeTypeModel`. |
| `ProtoBufServiceClient` | Concrete client inheriting `ServiceClientBase` communicating over Protocol Buffers. |
| `ProtoBufExtensions` | In-memory serialization extensions: `obj.ToProtoBuf()` and `bytes.FromProtoBuf<T>()`. |

---

## Architecture & Design Patterns

### Code-First Binary Schema
`protobuf-net` uses reflection and data contract attributes (`[DataContract]`, `[DataMember(Order = N)]`) to generate binary schemas on the fly.

### Thread-Safe Model Initialization
The shared `RuntimeTypeModel` is initialized via double-checked locking, preventing race conditions or loss of custom type registrations during concurrent startup.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Follow these precautions:

### 1. Thread-Safe `RuntimeTypeModel` Singleton
- Initializing `ProtoBufFormat.Model` must use synchronization to prevent concurrent requests from creating duplicate models and overwriting type definitions.

### 2. Null Payload & Empty Stream Defenses
- `ProtoBufFormat.Serialize` must return early if `dto == null` or `outputStream == null` to prevent protobuf-net `ArgumentNullException`.
- `ProtoBufExtensions.ToProtoBuf` returns `TypeConstants.EmptyByteArray` directly for null objects.
- `ProtoBufExtensions.FromProtoBuf<T>` returns `default(T)` directly if `bytes == null || bytes.Length == 0`.

### 3. Stream Validation
- `ProtoBufServiceClient` must validate stream and request parameters, wrapping deserialization failures in `SerializationException` for clean client error propagation.

---

## Common Modification Scenarios

1. **Enabling ProtoBuf on the Server**
   ```csharp
   Plugins.Add(new ProtoBufFormat());
   ```

2. **Calling ProtoBuf Services from a .NET Client**
   ```csharp
   var client = new ProtoBufServiceClient(baseUrl);
   var response = await client.GetAsync(new MyRequest());
   ```
