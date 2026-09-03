# Security & Hardening Changes: ServiceStack.Extensions

## Summary
Audit and remediation of security, proxy protocol integrity, authentication handling, and compiler warnings across `net6.0`, `net8.0`, and `net10.0` in `ServiceStack.Extensions`.

---

## 1. Authentication & Input Robustness

### `BasicAuthenticationHandler.cs`
- **Vulnerability**: `HandleAuthenticateAsync` previously called `AuthenticationHeaderValue.Parse(auth!)` and `Convert.FromBase64String` unconditionally. Malformed headers or non-Base64 payloads triggered unhandled `FormatException` runtime exceptions, causing 500 Internal Server Errors instead of standard HTTP 401 / `AuthenticateResult.Fail`.
- **Remediation**:
  - Implemented safe `AuthenticationHeaderValue.TryParse` validation.
  - Verified authentication scheme is explicitly "Basic" before decoding credentials.
  - Guarded Base64 credential extraction against `FormatException`, returning a clean `AuthenticateResult.Fail("Invalid Base64-encoded credentials.")`.
  - Replaced primary constructors with explicit constructors to eliminate compiler warning `CS9107` (parameter capture in derived types).

### `Auth/IdentityUtils.cs`
- **Vulnerability**: `IdentityException(List<IdentityError> errors)` accessed `errors[0]` directly without verifying that the collection contained elements, risking `ArgumentOutOfRangeException`.
- **Remediation**: Guarded message and code initialization with `errors.FirstOrDefault()?.Description ?? "Identity operation failed"` and `errors.FirstOrDefault()?.Code`.
- Guarded `GetClaimsPrincipalRoles` against null `IOptions<IdentityOptions>` resolution, falling back to `ClaimTypes.Role`.

### `Auth/AppleAuthProvider.cs`
- Guarded private key file parsing: added explicit validation ensuring loaded `.p8` key files are not empty before converting.
- Corrected nullability on `KeyBytes`, `ClientSecretFactory`, `authInfo`, and `hashParams`.

---

## 2. HTTP Proxying & Protocol Integrity

### `NodeProxy.cs`
- **Bug**: In `HttpToNode`, request headers were iterated and copied to `forwardRequest.Headers`. If header addition failed (which occurs for all HTTP content headers such as `Content-Type`, `Content-Length`, and `Content-Encoding`), the code attempted `forwardRequest.Content?.Headers.TryAddWithoutValidation(...)`. However, `forwardRequest.Content` was instantiated *after* the header loop. As a result, `forwardRequest.Content` was null and all content headers on proxied POST/PUT/PATCH requests were silently dropped.
- **Remediation**: Instantiated `forwardRequest.Content = new StreamContent(request.Body)` prior to copying request headers, ensuring `Content-Type` and other content headers are preserved on proxied Node.js requests.
- **Null Safety**: Replaced unsafe `context.Request.Host.Value!.Contains("localhost")` with `context.Request.Host.Value?.Contains("localhost") == true`, preventing `NullReferenceException` when requests lack a Host header value.
- Removed unused exception variable `ex` in process polling loop (`CS0168`).

---

## 3. Dependency Injection & Service Configuration

### `BlazorExtensions.cs`
- **Bug & Deprecation**: `AddBlazorApiClient` used the obsolete `.ConfigureHttpMessageHandlerBuilder(...)` (`CS0618`). Within the configuration lambda, `HttpUtils.HttpClientHandlerFactory()` was created and configured, but was never actually assigned to `h.PrimaryHandler`.
- **Remediation**: Migrated to modern `.ConfigurePrimaryHttpMessageHandler(() => ...)` and returned the configured handler so that it is properly utilized by the `HttpClient`.

### `Auth/IdentityJwtAuthProvider.cs` & `Auth/IdentityAuth.cs`
- **Type Collision**: Resolved `CS0436` collision where `ConvertSessionToTokenService` existed with the identical namespace in both `ServiceStack.dll` and `ServiceStack.Extensions.dll`. Renamed the ASP.NET Core Identity implementation to `IdentityConvertSessionToTokenService` (matching the existing `GetAccessTokenIdentityService`).
- Resolved XML doc comment formatting (`&amp;`) in `IdentityAuth.cs` (`CS1570`).
- Cleaned up obsolete property access in `IdentityAuth.cs` by introducing internal property backing `HasCredentialsAuth`.

---

## 4. gRPC Protocol & Dynamic Method Safety

### `Grpc/GrpcRequest.cs`
- Corrected nullability on 15 optional HTTP request properties (`UserHostAddress`, `UserAgent`, `CompressionType`, `UrlReferrer`, `AcceptTypes`, `HttpResponse`, `HttpMethod`, `XForwardedFor`, `XForwardedProtocol`, `XRealIp`, `Accept`).
- Resolved nullability in `GetRawBodyAsync()` (`Task.FromResult((string?)null)`).
- Null-guarded `context.RequestHeaders` iteration.

### `GrpcFeature.cs` & `GrpcServiceBase.cs`
- Added null safety checks for dynamic reflection method bindings (`Execute`, `ExecuteDynamic`, `Stream`, `MapGrpcService`).
- Guarded `op.Actions` against null before enumerating service actions.
- Guarded `context.CallOptions.Headers` in `GrpcServiceBase.Execute` and `Stream`.
- Safely read file byte arrays in `StreamFileService` to prevent null dereference warnings.

---

## 5. Verification

- **Automated Tests**: Added `SecurityRemediationTests.cs` covering:
  - `BasicAuthenticationHandler` handling malformed Base64 credentials and non-Basic schemes without throwing exceptions.
  - `NodeProxy` preservation of `Content-Type` on proxied request bodies.
  - `NodeProxy` safe evaluation of null or localhost host strings.
  - `IdentityException` handling of empty error lists without index out-of-range exceptions.
  - `GetClaimsPrincipalRoles` fallback when `IdentityOptions` is null.
- **Suite Results**:
  - `src/ServiceStack.Extensions/ServiceStack.Extensions.csproj` builds across `net6.0`, `net8.0`, and `net10.0` with **0 warnings and 0 errors**.
  - All 539 tests in `tests/ServiceStack.Extensions.Tests/` pass with **0 failures**.
  - `src/ServiceStack.sln` compiles cleanly with **0 errors**.
