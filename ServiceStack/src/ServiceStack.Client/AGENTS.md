# CONTEXT: ServiceStack.Client

## Purpose

`ServiceStack.Client` is the **base client library** for all ServiceStack service client implementations. It provides the foundational infrastructure shared across every client variant (JSON, XML, gRPC, etc.), including:

- REST URL generation from request DTOs
- HTTP authentication header parsing and handling
- Stream compression/decompression utilities
- RSA key parsing for the Encrypted Messaging feature
- Browser/crawler User-Agent detection
- Shared exception types (`WebServiceException`)
- OpenTelemetry diagnostics integration

This library contains **no HTTP transport logic itself** — it only supplies the utilities, types, and algorithms that concrete client implementations build upon.

**Target frameworks:** `net472;net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack.Client  (this library)
    ↑
    ├── ServiceStack.HttpClient   (JsonHttpClient, JsonApiClient — HttpClient-based)
    ├── ServiceStack.GrpcClient   (GrpcServiceClient)
    └── ServiceStack (server-side gateway/proxy calls, shared DTOs)
```

- **Depends on:** `ServiceStack.Interfaces`, `ServiceStack.Text` (serialization), `ServiceStack.Common`
- **Consumed by:** Every ServiceStack client package; also used server-side when the server acts as a gateway calling other services.
- **Shared with server:** Types like `WebServiceException`, `ResponseStatus`, and `UrlExtensions` are used on both sides of the wire.

---

## Key Functionality

### URL Generation — `UrlExtensions`

The most critical subsystem. Converts request DTOs into REST URLs.

| Method | Description |
|---|---|
| `ToUrl(verb, format)` | Primary entry point — returns REST URL string for a DTO |
| `GetUrlVariables(route)` | Extracts `{variableName}` tokens from a route template |
| `ToGetUrl()` / `ToPostUrl()` | Convenience wrappers for common HTTP verbs |
| `AppendQueryString()` | Appends non-route DTO properties as query string parameters |

**`RestRoute`** — internal class that represents a single `[Route]` attribute candidate:
- `RestRoute.Apply(dto)` — substitutes `{variable}` placeholders with values from the DTO and builds the final URL path
- Route variable parsing must handle `:constraint`, `?` optional, and `*` wildcard suffixes (stripped before property lookup)

### RSA Key Parsing — `CryptUtils` / `PlatformRsaUtils`

Used exclusively by the **Encrypted Messaging** feature (`EncryptedServiceClient`).

- `PlatformRsaUtils.ExtractFromXml(xmlKey)` — parses RSA keys from .NET XML format (`<RSAKeyValue>…</RSAKeyValue>`)
- `CryptUtils` — higher-level helpers for encrypting/decrypting service request payloads

### HTTP Authentication — `WebRequestUtils` / `AuthenticationInfo`

Handles the client-side of HTTP challenge/response authentication:

- `AuthenticationInfo` — parses `WWW-Authenticate` response headers into a structured object
  - Supports `Basic`, `Digest`, and `Bearer` schemes
  - Parses comma-separated key=value pairs, handling quoted strings
- `WebRequestUtils.AddAuthInfo()` — attaches the correct `Authorization` header to a retry request based on the parsed challenge

### Stream Compression — `StreamCompressors`

Adapts `System.IO.Compression` primitives into a common interface used by service clients when negotiating compressed responses:

- Supports **GZip**, **Deflate**, and **Brotli**
- Used when clients send `Accept-Encoding` headers and need to decompress responses
- Compressors are registered by content-encoding name for lookup

### User-Agent Detection — `UserAgentHelper`

Classifies incoming `User-Agent` strings:

- `IsBrowserUserAgent(ua)` — returns `true` for web browsers
- `IsCrawlerUserAgent(ua)` — returns `true` for bots/crawlers

Used server-side to decide response formats or access restrictions.

### Exception Types — `WebServiceException`

The client-side exception thrown when a service call fails with a non-success HTTP status:

```csharp
public class WebServiceException : Exception
{
    public int StatusCode { get; }           // HTTP status code
    public string StatusDescription { get; } // HTTP reason phrase
    public ResponseStatus ResponseStatus { get; } // Deserialized error payload
    public object ResponseDto { get; }       // Typed response DTO if available
}
```

`WebServiceException.ToErrorResponse()` converts the exception back into a typed error DTO for uniform error handling.

### Response Formatting — `ResponseStatusUtils`

Formats `ResponseStatus` error objects for display:

- `ResponseStatusUtils.ToErrorSummary(status)` — produces a human-readable error string
- Iterates `status.Errors` (field-level validation errors); must handle null items defensively

### OpenTelemetry — `ClientDiagnosticUtils`

Thin integration layer for distributed tracing:

- Creates `Activity` spans for outbound service calls
- Propagates `traceparent`/`tracestate` headers
- Used by `JsonHttpClient` / `JsonApiClient` when tracing is enabled

---

## Architecture & Design Patterns

### Static Utility Classes
Most functionality is exposed as **static extension methods or static helper classes** rather than injected services. This keeps the client library dependency-free and usable in constrained environments (e.g., Blazor WASM, mobile).

### Route Selection Algorithm (`UrlExtensions`)
1. Collect all `[Route]` attributes on the request DTO type
2. For each candidate route, call `RestRoute.Apply(dto)` to attempt variable substitution
3. Score each successful match by how many DTO properties it consumed
4. Return the highest-scoring route's URL + append remaining properties as query string

### Multi-targeting
The library targets `net472`, `net6.0`, `net8.0`, and `net10.0`. Platform-specific code (e.g., Brotli compression, RSA APIs) is conditionally compiled with `#if` directives or routed through `PlatformRsaUtils` which has per-TFM implementations.

---

## Security & Reliability Considerations

> These represent known vulnerability classes that have been fixed. Any modification to the affected methods must preserve these mitigations.

### XXE Injection & Infinite Loop in XML Key Parsing (`PlatformRsaUtils`)
- **Risk:** `XmlReader` with default settings is vulnerable to XXE (XML External Entity) attacks when parsing RSA key XML.
- **Mitigation:** `ExtractFromXml` **must** create the `XmlReader` with `DtdProcessing = DtdProcessing.Prohibit`.
- **Infinite loop risk:** If the XML is truncated, `reader.Read()` may return `false` immediately. Always check the return value; do not loop unconditionally.

### Digest Header Parsing Crash (`AuthenticationInfo`)
- **Risk:** Malformed `WWW-Authenticate: Digest` headers with unclosed quotes or fewer-than-expected tokens cause `IndexOutOfRangeException` when accessing `pars[i+1]`.
- **Mitigation:** The parser must:
  1. Track open/close quote state when splitting on commas
  2. Validate `i + 1 < pars.Length` before accessing the next element
  3. Skip or abort gracefully on malformed input rather than throwing

### ReDoS in User-Agent Detection (`UserAgentHelper`)
- **Risk:** Complex backtracking regex patterns applied to attacker-controlled `User-Agent` strings can cause catastrophic backtracking (Denial of Service).
- **Mitigation:** All User-Agent regexes **must** be:
  - Precompiled (`RegexOptions.Compiled`) — initialized once at static field level
  - Constructed with an explicit **1-second timeout** (`TimeSpan.FromSeconds(1)`)
  - Wrapped to catch `RegexMatchTimeoutException` and return a safe default

### Route Variable Substring Crash (`GetUrlVariables`)
- **Risk:** Route templates with malformed variable tokens (e.g., `{}` — empty braces, length < 2) cause `ArgumentOutOfRangeException` in `Substring(1, component.Length - 2)`.
- **Mitigation:** Before calling `Substring`, validate `component.Length >= 2`.

### Route Constraint / Optional / Wildcard Suffixes (`GetUrlVariables` / `RestRoute.Apply`)
- **Risk:** Variable names like `{id:int}`, `{name?}`, or `{*path}` would fail property lookups if the suffix characters are not stripped.
- **Mitigation:** After extracting a variable name from braces, normalize it:
  ```csharp
  varName = varName.LeftPart(':').TrimEnd('?').Trim('*');
  ```

### Null Wildcard Replacement (`RestRoute.Apply`)
- **Risk:** If a wildcard route variable resolves to `null`, calling `uri.Replace("{varName}", null)` throws `ArgumentNullException` in some .NET versions.
- **Mitigation:** Use `variableValue ?? string.Empty` as the replacement value.

### Null Error Items in `ResponseStatusUtils`
- **Risk:** `ResponseStatus.Errors` may contain null entries (e.g., from partial deserialization). Iterating and accessing properties on null items causes `NullReferenceException`.
- **Mitigation:** Filter null items before formatting: `status.Errors?.Where(e => e != null)`.

---

## Common Modification Scenarios

### Adding a new HTTP authentication scheme
- Modify `AuthenticationInfo` to recognize the new scheme keyword
- Add parsing logic for the scheme's header parameters
- Update `WebRequestUtils.AddAuthInfo()` to construct the appropriate `Authorization` header
- **Preserve** the bounds-checking guard on `pars[i+1]` access

### Adding a new compression algorithm
- Implement a new compressor class satisfying the `IStreamCompressor` interface (or equivalent)
- Register it in `StreamCompressors` keyed by its content-encoding name (e.g., `"zstd"`)
- Add conditional compilation if the underlying API is not available on all target frameworks

### Updating route variable parsing
- Changes go in `UrlExtensions.GetUrlVariables()` and `RestRoute.Apply()`
- **Always** re-validate the `component.Length >= 2` guard after any changes to the extraction loop
- **Always** preserve constraint/optional/wildcard suffix stripping
- Run against route templates with edge cases: empty braces `{}`, constraint `{id:int}`, optional `{slug?}`, wildcard `{*remainder}`

### Adding OpenTelemetry attributes
- Modify `ClientDiagnosticUtils` — add `activity.SetTag(...)` calls
- Follow OpenTelemetry semantic conventions for HTTP client spans (`http.request.method`, `server.address`, etc.)

### Modifying RSA key parsing
- Changes go in `PlatformRsaUtils.ExtractFromXml()`
- **Must not** remove `DtdProcessing.Prohibit` — doing so reintroduces the XXE vulnerability
- **Must** retain the `reader.Read()` return-value check to avoid the infinite loop on truncated input

### Extending `WebServiceException`
- The type is part of the public API surface used by all consumers — avoid removing or renaming existing properties
- When adding new properties sourced from response headers or body, ensure they degrade gracefully when the server doesn't send them (null-safe)
