# Security Changes & Remediation Reference (`ServiceStack.GrpcClient`)

This document details security, robustness, and stability fixes across `ServiceStack.GrpcClient`.

---

## 1. Deprecated Native Library Removal (`Grpc.Core` v2.46.6)
- **Severity**: Medium (Unmaintained Dependency / Deprecated Native Binaries)
- **Description**:
  - `ServiceStack.GrpcClient` declared an explicit package dependency on `Grpc.Core` 2.46.6. `Grpc.Core` is deprecated by the gRPC project, no longer maintained, and contains native C-core binaries that pose security, maintenance, and architecture compatibility issues.
- **Remediation**:
  - Removed `Grpc.Core` package reference.
  - Refactored `GrpcUtils.Execute` from `Grpc.Core.Channel` to `Grpc.Core.ChannelBase` utilizing `channel.CreateCallInvoker()`, relying solely on modern managed `Grpc.Net.Client` and `Grpc.Core.Api`.

---

## 2. Channel Resource Leak Remediation (`GrpcServiceClient.RetryRequest`)
- **Severity**: Medium (Resource Exhaustion / Connection & Socket Leaks)
- **Description**:
  - In `GrpcServiceClient.RetryRequest`, when refreshing an expired access token against a custom `RefreshTokenUri`, a new `GrpcChannel` was created (`GrpcChannel.ForAddress(config.RefreshTokenUri)`). The channel disposal was placed in `using (newChannel){}` *after* `await GetResponseAsync(...)`. If an exception occurred during token retrieval or deserialization, the channel and its underlying HTTP/2 connections and sockets were never disposed.
- **Remediation**:
  - Wrapped channel usage in a `try { ... } finally { newChannel?.Dispose(); }` block ensuring guaranteed disposal on success, network failure, or cancellation.

---

## 3. CancellationToken Propagation to gRPC CallOptions (`GrpcServiceClient`, `GrpcUtils`)
- **Severity**: Low (Hanging Requests / Unresponsive Cancellation)
- **Description**:
  - Public API methods (`Execute`, `ExecuteAll`, `Stream`, `PublishAsync`) accepted a `CancellationToken token = default`, but the token was never attached to `CallOptions`. Under request cancellation or server timeouts, gRPC transport calls continued running in the background, consuming network bandwidth and client/server resources.
- **Remediation**:
  - Updated `GrpcUtils.Init(this CallOptions options, GrpcClientConfig config, bool noAuth, CancellationToken token = default)` to attach tokens via `options.WithCancellationToken(token)`.
  - Forwarded `token` through all client call paths (`Execute`, `ExecuteAll`, `Stream`).

---

## 4. Streaming Retry Fall-Through Fix (`GrpcServiceClient.Stream`)
- **Severity**: Low (Logic Bug / Secondary Exception)
- **Description**:
  - In `GrpcServiceClient.Stream`, when an unauthenticated stream was successfully retried after token refresh, the retried stream was enumerated with `yield return item`. However, execution did not terminate afterwards, falling through to the subsequent code block which attempted to read the original failed `response` stream.
- **Remediation**:
  - Added `yield break;` immediately after the retry stream iteration.

---

## 5. Modern X509 Certificate Loading (`GrpcUtils.AddPemCertificateFromFile`)
- **Severity**: Low (Deprecation / Insecure Constructor Warning SYSLIB0057)
- **Description**:
  - `new X509Certificate2(fileName)` is obsolete in .NET 9+ due to platform-inconsistent certificate parsing and potential security pitfalls.
- **Remediation**:
  - Added `#if NET9_0_OR_GREATER` conditional compilation targeting `X509CertificateLoader.LoadCertificateFromFile(fileName)`.
