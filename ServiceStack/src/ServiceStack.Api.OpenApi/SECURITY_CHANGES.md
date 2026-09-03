# Security Changes & Remediation Reference (`ServiceStack.Api.OpenApi`)

This document details security vulnerabilities identified and remediated across `ServiceStack.Api.OpenApi`.

---

## 1. Cross-Site Scripting (XSS) & Attribute Breakout in Swagger UI (`OpenApiFeature`)
- **Severity**: Medium
- **Description**:
  - In `OpenApiFeature`, `CustomResponseHandler` serves the embedded Swagger UI HTML template and dynamically replaces placeholder tokens.
  - `HostContext.ServiceName` was previously interpolated directly into HTML markup (`<span class="logo__title">{HostContext.ServiceName}</span>`) and replaced the `ApiDocs` title without HTML entity encoding. If `ServiceName` contained HTML or script characters (`<`, `>`, `"`), it enabled Cross-Site Scripting (XSS).
  - `LogoHref` and `LogoUrl` were directly substituted into `<a href="...">` and `<img src="...">` attributes without validation or encoding. A `javascript:` URL or an attribute breakout quote would execute arbitrary script.
  - In addition, template string mutation inside the request handler closure (`html = html.Replace(...)`) risked accumulator side effects across requests.
- **Change**:
  - Applied `HtmlEncode()` to `HostContext.ServiceName`.
  - Added URL scheme validation and HTML attribute encoding for `LogoHref` and `LogoUrl`, rejecting dangerous URI schemes (such as `javascript:`, `vbscript:`, and unsafe `data:` URLs).
  - Used an isolated local variable `pageHtml` per request to avoid mutating captured template state.

---

## 2. Regular Expression Denial of Service (ReDoS) in `ResourceFilterPattern` (`OpenApiFeature`)
- **Severity**: Low / Medium
- **Description**:
  - `OpenApiService.ResourceFilterRegex` was compiled from `ResourceFilterPattern` without specifying a `matchTimeout`.
  - If a user-configured regex pattern contained overlapping or nested quantifiers, matching against API request paths or operation names could trigger catastrophic backtracking, exhausting thread pool resources.
- **Change**:
  - Added an explicit regex timeout of 1 second (`TimeSpan.FromSeconds(1)`) to `new Regex(ResourceFilterPattern, RegexOptions.Compiled, TimeSpan.FromSeconds(1))`.
  - Exposed `ResourceFilterRegex` as an instance property on `OpenApiFeature` and connected the regex filter to path filtering in `OpenApiService.Get`.

---

## 3. `NullReferenceException` in `OpenApiService.Get` when `SchemaFilter` is Configured (`OpenApiService`)
- **Severity**: Medium (Reliability / Availability)
- **Description**:
  - In `OpenApiService.Get()`, when `SchemaFilter != null`, `result.Responses.Each(...)` was invoked on `OpenApiDeclaration`.
  - Because `OpenApiDeclaration.Responses` was not initialized by default (`null`), registering any `SchemaFilter` caused `/openapi` to crash with an unhandled `NullReferenceException`.
- **Change**:
  - Changed invocation to null-conditional navigation: `result.Responses?.Each(...)`.

---

## 4. `KeyNotFoundException` and Broken References for Generic Types (`OpenApiService`)
- **Severity**: Medium (Reliability)
- **Description**:
  - OpenAPI definitions are registered in `OpenApiService` under sanitized identifiers via `GetSchemaDefinitionRef` (which replaces `< > ,` with `_`, e.g. `MyGeneric_string_`).
  - Several lookup and reference call sites (`InlineSchema`, `GetSchemaForResponseType`, and `GetParameter`) incorrectly called `GetSchemaTypeName` (e.g. `MyGeneric<string>`), causing `KeyNotFoundException` during schema generation and emitting illegal JSON Pointer characters in `$ref` values.
- **Change**:
  - Standardized schema dictionary lookups and definition `$ref` pointers to consistently use `GetSchemaDefinitionRef`.

---

## 5. Route Path Parameter Matching for Wildcards & Constraints (`OpenApiService`)
- **Severity**: Low / Robustness
- **Description**:
  - In `OpenApiService.ParseParameters`, route parameter detection evaluated `route.Contains("{" + propertyName + "}")`.
  - Routes specifying wildcards (e.g. `{Path*}`) or route constraints (e.g. `{Id:int}`) were not recognized as path parameters and were erroneously classified as query or form parameters.
- **Change**:
  - Enhanced path parameter matching to check for `{name}`, `{name*`, and `{name:`.

---

## 6. Multi-Host Isolation via Active Plugin Resolution (`OpenApiService`)
- **Severity**: Low / Robustness
- **Description**:
  - `OpenApiService` previously stored configuration solely in `static` properties clobbered during `OpenApiFeature.Register`. When multiple AppHosts run in the same process (e.g. integration testing or multi-tenant hosting), configurations interfered with each other.
- **Change**:
  - Updated `OpenApiService` methods to resolve options from `HostContext.GetPlugin<OpenApiFeature>()` with fallback to static properties.
