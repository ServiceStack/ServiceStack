# CONTEXT: ServiceStack.Extensions

## Purpose

`ServiceStack.Extensions` is the ASP.NET Core–specific extension and integration layer for ServiceStack. It bridges ServiceStack's authentication model, service pipeline, and request handling with:

- **ASP.NET Core Identity** – maps `UserManager<T>` / `RoleManager<T>` to ServiceStack sessions and JWT tokens.
- **gRPC** – hosts ServiceStack services over the gRPC protocol via `Grpc.AspNetCore`.
- **Blazor WebAssembly** – provides `HttpClient` configuration helpers for Blazor WASM clients calling ServiceStack APIs.
- **Node.js dev proxy** – provides an HTTP reverse proxy for forwarding requests to a Vite/Node.js dev server during development.
- **Apple Sign-In** – OAuth provider for Sign in with Apple, including `.p8` private key loading for client secret generation.

This project does **not** replace `ServiceStack.Auth`; it extends it with ASP.NET Core–native integration points that require a reference to `Microsoft.AspNetCore.*` packages which cannot be carried by the core library.

---

## Role in ServiceStack Ecosystem

```
ServiceStack (core)
  └── ServiceStack.Auth (auth primitives)
        └── ServiceStack.Extensions   ← this project
              ├── depends on: Microsoft.AspNetCore.Identity
              ├── depends on: Grpc.AspNetCore
              └── consumed by: host ASP.NET Core applications
```

- **Depends on**: `ServiceStack` core, `ServiceStack.Auth`, `Microsoft.AspNetCore.Authentication`, `Microsoft.AspNetCore.Identity`, `Grpc.AspNetCore`, `Grpc.Core`.
- **Consumed by**: ASP.NET Core host projects that use `app.UseServiceStack()` alongside `services.AddIdentity<TUser, TRole>()` or gRPC.
- **Target frameworks**: `net6.0`, `net8.0`, `net10.0` — no .NET Framework or .NET Standard targets.

---

## Key Functionality

### ASP.NET Core Identity Integration

| Type | Description |
|------|-------------|
| `IdentityAuth` | Static factory / registration helpers. Configures ServiceStack to delegate authentication to ASP.NET Core Identity. |
| `IdentityAuthProvider<TUser, TRole>` | Core auth provider. Resolves `UserManager<TUser>` and `RoleManager<TRole>` from the DI container, validates credentials, and populates a ServiceStack `AuthUserSession`. |
| `IdentityJwtAuthProvider` | JWT bearer token provider built on top of Identity. Issues and validates JWT tokens using ASP.NET Core's data-protection / token infrastructure. |
| `IdentityConvertSessionToTokenService` | Named `IdentityConvertSessionToTokenService` (not `ConvertSessionToTokenService`) to avoid a CS0436 type-collision with the identically-named type in `ServiceStack.dll`. Converts an existing auth session to a JWT token. |
| `IdentityUtils` | Static helpers: `GetClaimsPrincipalRoles`, wrapping `ClaimsPrincipal` role claims; `IdentityException` (see Security section). |
| `BasicAuthenticationHandler` | ASP.NET Core `AuthenticationHandler<AuthenticationSchemeOptions>`. Decodes HTTP Basic auth credentials from the `Authorization` header and authenticates them through the ServiceStack pipeline. |

### Apple Sign-In

| Type | Description |
|------|-------------|
| `AppleAuthProvider` | OAuth2 provider for "Sign in with Apple". Loads an Apple-issued `.p8` private key file from disk and uses it to generate the short-lived client secrets required by Apple's OAuth flow. |

### gRPC Hosting

| Type | Description |
|------|-------------|
| `GrpcFeature` | ServiceStack plugin. Registers gRPC endpoints for all ServiceStack request types. Wires up reflection and route bindings. |
| `GrpcServiceBase` | Base class for generated gRPC service implementations. Adapts gRPC `ServerCallContext` to ServiceStack's `IRequest` interface and dispatches into the normal ServiceStack service pipeline. |
| `GrpcRequest` | Adapts a gRPC `ServerCallContext` to `IHttpRequest`. All 15 optional HTTP request properties (Path, QueryString, Headers, etc.) are properly nullable. |

### Node.js Development Proxy

| Type | Description |
|------|-------------|
| `NodeProxy` | A lightweight ASP.NET Core middleware that acts as an HTTP reverse proxy to a locally running Node.js/Vite dev server. Preserves request body and response headers. Used for development hot-reload workflows where the front-end is served from a separate process. |

### Blazor WebAssembly Helpers

| Type | Description |
|------|-------------|
| `BlazorExtensions` | Extension methods on `IServiceCollection`. `AddBlazorApiClient(baseUrl)` registers a typed `HttpClient` configured to call a ServiceStack JSON API, with CORS credentials. |

---

## Architecture & Design Patterns

### Plugin Registration Pattern

`GrpcFeature` and `IdentityAuth` follow the standard ServiceStack `IPlugin` / `IConfigureAppHost` pattern and are registered via `appHost.Plugins.Add(...)` or the equivalent `services.AddServiceStack(...)` fluent API.

### ASP.NET Core AuthenticationHandler Integration

`BasicAuthenticationHandler` implements the ASP.NET Core authentication middleware contract (`AuthenticateAsync`, `ChallengeAsync`). It sits in the ASP.NET Core middleware pipeline before ServiceStack and lets ASP.NET Core route authenticated identities into ServiceStack without requiring ServiceStack's own auth endpoint.

### Identity → ServiceStack Session Mapping

`IdentityAuthProvider` resolves the `UserManager<TUser>` from the request's DI scope (not the root container) because `UserManager` is typically `Scoped`. It maps `IdentityUser` properties and `ClaimsPrincipal` roles to ServiceStack's `IAuthSession` properties on every authenticated request.

### gRPC Adaptation Layer

`GrpcRequest` wraps `ServerCallContext` and implements `IHttpRequest` so ServiceStack services remain protocol-agnostic. The gRPC feature dynamically generates method bindings (via reflection) for every registered ServiceStack request DTO at startup; these bindings include null-safety guards to handle missing metadata.

### Proxy Pattern (NodeProxy)

`NodeProxy` creates a new `HttpRequestMessage` for each incoming request, copies safe headers (excluding hop-by-hop headers), and streams the response body back. **Content is instantiated before headers are copied** (see Security section) to avoid silently dropping `Content-Type` and `Content-Encoding`.

---

## Security & Reliability Considerations

> These reflect real bugs fixed in `SECURITY_CHANGES.md`. They must be preserved or re-applied whenever this code is modified.

### BasicAuthenticationHandler — Malformed Header Hardening

```csharp
// CORRECT: parse defensively
if (!AuthenticationHeaderValue.TryParse(request.Headers["Authorization"], out var header))
    return AuthenticateResult.Fail("Invalid Authorization header");

string decoded;
try { decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter ?? "")); }
catch { return AuthenticateResult.Fail("Invalid Base64 in Authorization header"); }
```

- **Never** call `Convert.FromBase64String` without a `try-catch`; a malformed `Authorization` header must return `AuthenticateResult.Fail(...)`, not propagate a `FormatException` as HTTP 500.
- **Always** use `AuthenticationHeaderValue.TryParse`, not direct string splitting.

### IdentityException — Safe Error Indexing

```csharp
// CORRECT
var first = errors.FirstOrDefault();
// WRONG — throws IndexOutOfRangeException when errors is empty
var first = errors[0];
```

`IdentityException` wraps an `IEnumerable<IdentityError>`. Always use `FirstOrDefault()` (or enumerate safely); never index with `[0]`.

### AppleAuthProvider — Key File Validation

After loading the `.p8` key file, validate the content is non-empty before passing it to the cryptographic key constructor. An empty file produces a misleading `CryptographicException` rather than an actionable configuration error.

```csharp
var keyContent = File.ReadAllText(p8KeyPath);
if (string.IsNullOrWhiteSpace(keyContent))
    throw new Exception($"Apple .p8 key file is empty: {p8KeyPath}");
```

### NodeProxy — Content Headers Must Be Set After Body

```csharp
// CORRECT: assign content first, then copy content headers
forwardRequest.Content = new StreamContent(context.Request.Body);
foreach (var header in context.Request.Headers)
    forwardRequest.Content.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);

// WRONG: assigning content AFTER copying headers silently discards Content-Type etc.
// (HttpContent.Headers is only accessible on an HttpContent instance)
```

If `forwardRequest.Content` is `null` when you attempt to set `Content-Type`, the header is silently lost, causing proxied `POST`/`PUT`/`PATCH` requests to arrive at the Node server without a content type.

### BlazorExtensions — Non-Obsolete HttpClient Configuration

```csharp
// CORRECT (net6+)
builder.Services.AddHttpClient("api", c => c.BaseAddress = new Uri(baseUrl))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { ... });

// WRONG — obsolete API removed in .NET 6+
.ConfigureHttpMessageHandlerBuilder(...)
```

Do not use `.ConfigureHttpMessageHandlerBuilder`; it was marked obsolete in .NET 5 and removed in .NET 6.

### Type Name Collision — IdentityConvertSessionToTokenService

The service that converts sessions to JWT tokens **must** be named `IdentityConvertSessionToTokenService`. The name `ConvertSessionToTokenService` is already defined in `ServiceStack.dll`. Using the same name in this assembly causes CS0436 (type conflict across assemblies) and can lead to runtime `TypeLoadException` depending on resolution order.

### GrpcRequest Nullability

All 15 optional HTTP-level properties on `GrpcRequest` (`UrlReferrer`, `UserAgent`, `XForwardedFor`, `XRealIp`, `Accept`, `AcceptLanguage`, `AcceptEncoding`, `ContentType`, `ContentLength`, `RemoteIp`, `PathInfo`, `AbsoluteUri`, `RawUrl`, `QueryString`, `IsSecureConnection`) must be declared nullable (`string?` / `long?` / `bool?`). Callers must null-check before use.

### gRPC Reflection — Null Safety

Dynamic method binding code that uses reflection to enumerate gRPC service methods must guard against `null` `MethodInfo` or missing `ServiceDescriptor` attributes. A missing binding at startup should log a warning rather than throw, to avoid crashing the host when a DTO is registered that gRPC cannot handle.

---

## Common Modification Scenarios

1. **Adding a new Identity user property to the ServiceStack session** — modify `IdentityAuthProvider<TUser,TRole>.PopulateSession()` to map the new `TUser` property to the corresponding `IAuthSession` field.

2. **Supporting a new ASP.NET Core Identity version** — update `IdentityUtils` and `IdentityAuthProvider` if `UserManager` / `RoleManager` APIs change; check for new `IdentityOptions` that should be forwarded.

3. **Adding a new gRPC-exposed DTO** — ensure the DTO implements `IReturn<TResponse>` and is registered with `GrpcFeature`. If the DTO has nullable reference properties, verify `GrpcRequest` correctly maps them as nullable.

4. **Extending the Node.js proxy** — when adding header forwarding or WebSocket upgrade support, be careful to maintain the content-before-headers ordering (see Security section) and to exclude hop-by-hop headers (`Connection`, `Transfer-Encoding`, `Keep-Alive`, `Upgrade`).

5. **Updating Blazor HttpClient configuration** — use only non-obsolete APIs (`ConfigurePrimaryHttpMessageHandler`). Test against all three TFMs (`net6.0`, `net8.0`, `net10.0`).

6. **Apple auth key rotation** — the `.p8` file path is configured via `AppleAuthProvider.KeyId` / `P8KeyFilePath`. Any change to key loading must include the non-empty validation guard.

7. **Adding a new auth claim mapping** — update `IdentityUtils.GetClaimsPrincipalRoles` if the claim type used for roles changes, and ensure `BasicAuthenticationHandler` forwards the right `ClaimsPrincipal` into the ServiceStack request context.
