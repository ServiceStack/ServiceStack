# CONTEXT: ServiceStack.HttpClient

## Purpose

`ServiceStack.HttpClient` provides high-performance, strongly-typed HTTP service clients built directly on .NET's modern `System.Net.Http.HttpClient`. It is the recommended client library for calling ServiceStack services from .NET Core, .NET 6+, Blazor, and modern .NET Framework applications.

Key capabilities include:
- End-to-end typed REST operations (`Get`, `Post`, `Put`, `Delete`, `Patch`) using Request and Response DTOs.
- Multipart form uploads (`PostFileWithRequest`, `PostFilesWithRequestAsync`).
- Built-in authentication support (Bearer Tokens, API Keys, Cookies, Basic Auth).
- Client-side HTTP caching and ETag management (`CachedHttpClient`).
- Transparent decompression (Gzip, Deflate, Brotli) and OpenTelemetry activity tracing.

**Target frameworks**: `net472;netstandard2.0;net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack.Interfaces     ServiceStack.Text     ServiceStack.Client
         │                          │                     │
         └──────────────────────────┼─────────────────────┘
                                    ▼
                         ServiceStack.HttpClient
                                    │
           ┌────────────────────────┴────────────────────────┐
           ▼                                                 ▼
Client Apps (Blazor, MAUI, CLI)             Microservice Gateways & In-Process HTTP
```

- **Depends on**: `ServiceStack.Interfaces`, `ServiceStack.Text`, `ServiceStack.Client`.
- **Depended on by**: Client applications, Blazor frontends, and microservices communicating over HTTP.
- **Relation to `ServiceStack.Client`**: `ServiceStack.Client` provides shared utilities (`UrlExtensions`, `AuthenticationInfo`, `WebServiceException`). `ServiceStack.HttpClient` implements the concrete transport layer using `System.Net.Http.HttpClient`.

---

## Key Functionality

### Primary Clients
| Class | Description |
|---|---|
| `JsonHttpClient` | Full-featured typed client for ServiceStack JSON APIs using `System.Net.Http.HttpClient`. Implements `IServiceClient`, `IRestClient`, `IServiceGateway`, `IHasSessionId`, `IHasBearerToken`. |
| `JsonApiClient` | Lightweight client designed for modern .NET environments and JSON API communication. |
| `CachedHttpClient` | Caching decorator over `JsonHttpClient` that supports memory caching of GET responses, ETags, and offline fallback. |

### Key Methods
- `GetAsync<TResponse>(IReturn<TResponse> requestDto)` / `SendAsync<TResponse>`: Core typed async request dispatchers.
- `PostFilesWithRequestAsync`: Multipart/form-data upload supporting multiple files alongside serialized DTO properties.
- `BearerToken` / `RefreshToken`: Automatic JWT Bearer token attachment and transparent token refreshing.
- `OnExceptionFilter` / `ResponseFilter`: Extensibility hooks for inspecting or altering requests and responses.

---

## Architecture & Design Patterns

### HttpClient Lifetime Management
`JsonHttpClient` wraps an underlying `System.Net.Http.HttpClient` instance or uses `HttpMessageHandler`. It can be instantiated per request or registered as a singleton with `IHttpClientFactory` in dependency injection containers.

### DTO-Driven URL Generation
Delegates URL and query string construction to `UrlExtensions` (from `ServiceStack.Client`), matching route definitions declared via `[Route]` attributes on request DTOs.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Maintain these fixes across future modifications:

### 1. Multipart Form Field Name vs. File Name Disambiguation
- **Risk**: In `PostFilesWithRequestAsync`, passing the file name instead of the designated form field name causes servers expecting specific field names (e.g. `file` or upload keys) to fail model binding.
- **Rule**: Always register multipart form content using `content.Add(fileContent, fieldName, fileName)`, preserving `UploadFile.FieldName`.

### 2. Null Dereference on Contentless Responses (`CachedHttpClient`)
- **Risk**: Accessing `webRes.Content.Headers` or `webRes.RequestMessage` on contentless responses (e.g., `304 Not Modified`, `204 No Content`) or detached responses throws `NullReferenceException`.
- **Rule**: Use null-conditional operators: `webRes.RequestMessage?.Method == HttpMethod.Get` and `webRes.Content?.Headers...`.

### 3. URL Query Parameter Delimiter Corruption
- **Risk**: Unconditionally appending `?` to request URLs corrupts URLs that already contain query strings.
- **Rule**: Check existing query parameters before appending: `absoluteUrl += (absoluteUrl.IndexOf('?') >= 0 ? "&" : "?") + queryString;`.

### 4. Rich Exception Diagnostics
- **Risk**: Creating empty `WebServiceException` instances without capturing inner exceptions or HTTP status codes discards critical failure diagnostics.
- **Rule**: Always populate `WebServiceException` with the inner exception, status code, and status description.

---

## Common Modification Scenarios

1. **Adding Support for Custom HTTP Handlers or Proxies**
   - Configure `JsonHttpClient.GetHttpClientHandler()` or pass a custom `HttpMessageHandler` into the `JsonHttpClient` constructor.

2. **Customizing Request Serialization / Content Headers**
   - Use `RequestFilter` or override `CreateHttpRequestMessage` in `JsonHttpClient`.
   - Ensure header mutations handle cloned or retried requests cleanly.

3. **Handling File Uploads**
   - Use `PostFileWithRequest` or `PostFilesWithRequestAsync`.
   - Ensure both the form field name and file name are supplied in `UploadFile`.
