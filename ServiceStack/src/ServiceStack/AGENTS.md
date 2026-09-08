# CONTEXT: ServiceStack

## Purpose

`ServiceStack` is the **core monolithic framework library** of the ServiceStack suite. It provides the HTTP request pipeline, routing engine, `AppHost` lifecycle, dependency injection (Funq IoC container), authentication and authorization subsystem, session management, content negotiation and serializers, server events, background messaging, and metadata/auto-discovery services.

It integrates seamlessly with modern ASP.NET Core (`Microsoft.AspNetCore.App`) as middleware or endpoints, while maintaining backwards compatibility with classic .NET Framework (`System.Web` / `net472`).

**Target frameworks**: `net472;net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack.Interfaces  ServiceStack.Text  ServiceStack.Common  ServiceStack.Client
         │                     │                    │                    │
         └─────────────────────┴──────────┬─────────┴────────────────────┘
                                          ▼
                                     ServiceStack (Core)
                                          │
    ┌──────────────────────────┬──────────┴──────────┬────────────────────────┐
    ▼                          ▼                     ▼                        ▼
ServiceStack.Server     ServiceStack.AI       ServiceStack.Jobs       ServiceStack.Kestrel
(OrmLite Auth, Redis)   (LLMs, Agents, Tools) (Task Scheduling)       (Direct Kestrel Host)
```

- **Depends on**: `ServiceStack.Interfaces`, `ServiceStack.Text`, `ServiceStack.Common`, `ServiceStack.Client`.
- **Depended on by**: Almost all server-side plugins and high-level feature packages (`ServiceStack.Server`, `ServiceStack.AI`, `ServiceStack.Jobs`, `ServiceStack.Kestrel`, `ServiceStack.RabbitMq`, `ServiceStack.GoogleCloud`, etc.).
- **Role**: The central server orchestrator. Hosts, registers, filters, executes, and serializes all ServiceStack services.

---

## Key Functionality

### 1. AppHost Lifecycle & Request Execution
- **`ServiceStackHost` / `HostContext`**: Singleton host orchestrator configuring plugins, service runners, and container registrations.
- **`AppHostBase`**: Base class for configuring services, filters, routes, and plugins in `Configure()`.
- **`IRequest` / `IResponse` Pipeline**: Normalized HTTP abstractions unifying ASP.NET Core `HttpContext` and legacy `System.Web.HttpContextBase`.
- **`ServiceRunner<T>`**: Manages per-request lifecycle: filter execution (`PreRequestFilters`, `RequestFilters`, `ResponseFilters`), validation, service execution, and exception handling.

### 2. Authentication & Authorization (`ServiceStack/Auth`)
- **`AuthFeature`**: Pluggable authentication system supporting multiple concurrent auth providers.
- **Built-in Providers**: `CredentialsAuthProvider`, `JwtAuthProvider`, `ApiKeyAuthProvider`, `DigestAuthProvider`, `OAuth2Provider`, `BasicAuthProvider`.
- **Session State**: `SessionFeature`, `IAuthSession`, `UserAuth`, session caching, and role/permission verification (`[Authenticate]`, `[RequiredRole]`, `[RequiredPermission]`).

### 3. Funq IoC Container (`ServiceStack/Funq`)
- Lightweight, ultra-fast embedded dependency injection container.
- Supports singleton, transient, and request-scoped lifecycles.
- Bridges bidirectionally with `Microsoft.Extensions.DependencyInjection` (`IServiceCollection`).

### 4. Content Negotiation & Serialization (`Formats/`)
- Built-in formatters: JSON, XML (`XmlSerializerFormat`), CSV (`CsvFormat`), JSV, JSON Lines (`JsonlFormat`), HTML (`HtmlFormat`), SOAP 1.1/1.2.
- Extensible content-type registration via `IContentTypeWriter` / `IContentTypeReader`.

### 5. Caching Subsystem (`Caching/`)
- In-memory cache client (`MemoryCacheClient`) with regex/pattern invalidation and TTL.
- Wrappers: `CacheClientAsyncWrapper`, `CacheClientWithPrefix`, `MultiCacheClient`.
- HTTP response caching via `HttpCacheFeature` (`[CacheResponse]`).

### 6. Real-Time Server Events & Messaging
- **`ServerEventsFeature`**: Built-in Server-Sent Events (SSE) provider (`MemoryServerEvents`) supporting channels, pub/sub, heartbeat, and client queries.
- **`BackgroundMqService`**: In-memory background worker queue executing background service requests asynchronously.

---

## Architecture & Design Patterns

### Unifying Abstractions
ServiceStack abstracts the underlying web server through `IRequest` and `IHttpResponse`. Code inside services, filters, and plugins should rarely touch raw ASP.NET Core `HttpContext` or `System.Web` instances.

### Plugin Architecture (`IPlugin`)
All optional or modular features are packaged as plugins implementing `IPlugin` (or `IPluginInstall` / `IAsyncPlugin`). Examples: `AuthFeature`, `ServerEventsFeature`, `ValidationFeature`, `MetadataFeature`, `OpenApiFeature`.

### Dual Sync and Async Support
Where possible, pipeline interfaces and providers provide both sync and async execution paths (e.g., `Execute` / `ExecuteAsync`, `IUserAuthRepository` / `IUserAuthRepositoryAsync`). Avoid sync-over-async (`Task.Result`) in request pipelines.

### TestMode Isolation
`HostContext.TestMode` allows running unit tests and mocking requests/responses without spinning up a full ASP.NET Core web server.

---

## Security & Reliability Considerations

> Extracted from `SECURITY_CHANGES.md`. Always preserve these security mitigations:

### 1. Cryptography & Password Hashing
- **Timing Attacks**: Password hash and digest comparisons must use constant-time comparison (`CryptUtils.FixedTimeEquals`), never direct string `==`.
- **Hash State Concurrency**: `SaltedHash.ComputeHash` must lock `HashProvider` (`lock (HashProvider)`) to protect non-thread-safe hash algorithms.
- **Input Robustness**: Catch `FormatException` on base64 password decoding rather than letting malformed inputs throw unhandled 500 errors.

### 2. XSS & HTML Encoding
- `HtmlFormat.EncodeForJavaScriptString` must safely escape `<` (`\u003c`), `>` (`\u003e`), and `&` (`\u0026`) to avoid `</script>` tag breakout vulnerabilities when rendering JSON/strings in HTML templates.

### 3. ReDoS in Regex Caching
- `MemoryCacheClient.ConvertToRegex` must escape all regex metacharacters when converting wildcards (`*` -> `.*`, `?` -> `.+`).
- Regex matches in `RemoveByRegex` must specify an explicit timeout (e.g., 2 seconds) and catch `RegexMatchTimeoutException`.

### 4. Setting & Config File Overwriting
- In `AppSettingsUtils.SaveAppSetting`, avoid loose prefix matching (`line.StartsWith(name)`) which corrupts settings sharing common prefixes (e.g., `Host` vs `HostName`). Use explicit delimiter checks (`name + " "` or `name + "="`).

### 5. Proper Resource Disposal
- Always dispose hash algorithm instances (`MD5`, `SHA256`) via `using`.
- Ensure async wrappers (`UserAuthRepositoryAsyncWrapper`, `CacheClientAsyncWrapper`) forward `Dispose()` and `DisposeAsync()` to underlying implementations.
- AppHost `OnDisposeCallbacks` must catch exceptions per callback to prevent an error in one callback from aborting cleanup of others.

---

## Common Modification Scenarios

1. **Adding a New Built-in Auth Provider**
   - Inherit from `AuthProvider` or `OAuth2Provider`.
   - Implement `Authenticate`, `AuthenticateAsync`, and session mapping.
   - Register the provider in `AuthFeature.AuthProviders`.
   - Ensure timing attack protections (`FixedTimeEquals`) are used on secret verification.

2. **Adding a New Content-Type Serializer**
   - Implement `IContentTypeWriter` and/or `IContentTypeReader`.
   - Register in `IContentTypeFilter` via `AppHost.ContentTypes.Register(...)`.
   - Ensure the formatter handles null models, null streams, and empty inputs gracefully.

3. **Modifying Request Filters / Execution Pipeline**
   - Global filters are registered in `AppHost.GlobalRequestFilters` or `GlobalRequestFiltersAsync`.
   - Service-specific filters are registered via `[RequestFilter]` attributes.
   - Verify that filters do not close the response stream unless they intend to short-circuit the pipeline (`res.EndRequest()`).

4. **Extending Session or Request Context**
   - Access session via `req.GetSession()` or `req.GetSessionAsync()`.
   - Use `req.Items` for request-scoped storage.
   - Guard against null `req` or uninitialized `HostContext.AppHost` to maintain testability under `HostContext.TestMode`.
