# CONTEXT: ServiceStack.GrpcClient

## Purpose

`ServiceStack.GrpcClient` provides a high-performance, strongly-typed .NET client (`GrpcServiceClient`) that communicates with ServiceStack gRPC services using HTTP/2 and Protocol Buffers via `protobuf-net.Grpc` and `Grpc.Net.Client`.

It enables ServiceStack services to be invoked as gRPC endpoints transparently without requiring `.proto` code generation. The same Request and Response DTOs used for REST and JSON APIs are reused seamlessly over gRPC.

Key features:
- End-to-end typed unary and server-streaming service calls over HTTP/2.
- Integrated authentication handling (Bearer Tokens, Refresh Tokens, API Keys).
- TLS and custom PEM client/server certificate configuration.
- Native cancellation token support and channel lifetime management.

**Target frameworks**: `net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack.Interfaces     ServiceStack.Text     ServiceStack.Client
         │                          │                     │
         └──────────────────────────┼─────────────────────┘
                                    ▼
                         ServiceStack.GrpcClient
                                    │
                                    ▼
       High-performance gRPC Microservices & Client Applications
```

- **Depends on**: `ServiceStack.Interfaces`, `ServiceStack.Text`, `ServiceStack.Client`, `Grpc.Net.Client`, `protobuf-net.Grpc`, `protobuf-net`.
- **Depended on by**: .NET applications and microservices requiring low-latency binary serialization and HTTP/2 multiplexing.
- **Server counterpart**: `ServiceStack.Grpc` (server-side gRPC endpoint host).

---

## Key Functionality

### Primary Types
| Class | Role |
|---|---|
| `GrpcServiceClient` | Core client implementation. Implements `IServiceClient`, `IServiceGateway`, and provides unary (`Send`, `SendAsync`), streaming (`Stream`, `StreamAsync`), and batching (`SendAll`) methods. |
| `GrpcClientConfig` | Configuration settings: base URI, channel options, credentials, token refresh URIs, and timeout policies. |
| `GrpcUtils` | Helper methods for initializing `CallOptions`, creating channels, attaching headers/auth tokens, and configuring TLS/PEM certificates. |

### Streaming & Unary Invocations
- `Execute<TResponse>` / `ExecuteAsync<TResponse>`: Dispatches unary gRPC service requests.
- `Stream<TResponse>`: Consumes server-side streaming responses as `IAsyncEnumerable<TResponse>` or `IEnumerable<TResponse>`.
- Token Refreshing: Catches unauthenticated responses and automatically executes token refresh workflows before retrying requests.

---

## Architecture & Design Patterns

### Code-First gRPC via protobuf-net
Unlike conventional gRPC requiring compiled `.proto` definitions, ServiceStack uses `protobuf-net.Grpc` code-first contracts. Request DTOs carrying `IReturn<T>` map directly to gRPC service methods.

### Pure Managed Stack
Relies exclusively on managed .NET networking via `Grpc.Net.Client` and `HttpClientHandler`, with no unmanaged dependencies on deprecated `Grpc.Core` C-binaries.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Follow these guidelines when making modifications:

### 1. Channel Disposal on Retry
- **Risk**: In `GrpcServiceClient.RetryRequest`, instantiating temporary `GrpcChannel` instances to refresh tokens can leak HTTP/2 sockets and connection pools if exceptions occur before or during token retrieval.
- **Rule**: Always wrap newly created channel instances in `try { ... } finally { newChannel?.Dispose(); }`.

### 2. CancellationToken Propagation
- **Risk**: Unpropagated cancellation tokens allow background gRPC calls to continue running after client cancellation or web request termination.
- **Rule**: `GrpcUtils.Init` must attach the cancellation token to `CallOptions` (`options.WithCancellationToken(token)`), and all call paths (`Execute`, `ExecuteAll`, `Stream`) must forward tokens.

### 3. Streaming Fall-Through Termination
- **Risk**: In `GrpcServiceClient.Stream`, retrying an unauthenticated stream must terminate with `yield break;` immediately after enumerating the retried stream. Falling through causes secondary exceptions when reading the original failed stream.

### 4. Modern Certificate Loading
- **Risk**: Calling `new X509Certificate2(fileName)` generates `SYSLIB0057` warnings on modern .NET and has platform-inconsistent certificate parsing.
- **Rule**: Use `#if NET9_0_OR_GREATER` targeting `X509CertificateLoader.LoadCertificateFromFile(fileName)`.

---

## Common Modification Scenarios

1. **Configuring Custom Channel Options or SSL / TLS**
   - Configure `GrpcClientConfig.ChannelOptions` or pass custom `GrpcChannelOptions` into the constructor.
   - Use `GrpcUtils.AddPemCertificateFromFile` for PEM certificate authentication.

2. **Extending Header or Metadata Propagation**
   - Modify `GrpcUtils.Init` to append custom gRPC `Metadata` entries to outbound calls.
   - Ensure header keys conform to gRPC metadata naming rules (lowercase ASCII, no spaces).

3. **Adding Streaming Endpoints**
   - Extend `GrpcServiceClient` streaming overloads.
   - Always ensure `IAsyncEnumerable` iteration paths support `CancellationToken` passing to `WithCancellation`.
