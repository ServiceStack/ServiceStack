# CONTEXT: ServiceStack.Api.OpenApi

## Purpose

`ServiceStack.Api.OpenApi` provides **Swagger 2.0 / OpenAPI 2.x** specification generation and the bundled **Swagger UI** for ServiceStack services. It is the classic, legacy OpenAPI integration — implementing the [OpenAPI Specification v2.0](https://swagger.io/specification/v2/) (historically called "Swagger 2.0").

At runtime it:
1. Generates a machine-readable JSON document at `GET /openapi` that describes every visible ServiceStack service (paths, operations, request/response schemas, security schemes).
2. Serves an embedded, patched Swagger UI at `/swagger-ui/` that consumes the `/openapi` endpoint, giving developers an interactive browser-based UI for exploring and invoking APIs.

> **This is the Swagger 2.0 implementation.** For OpenAPI 3.x, see `ServiceStack.AspNetCore.OpenApi`, `ServiceStack.OpenApi.Microsoft`, or `ServiceStack.OpenApi.Swashbuckle`.

---

## Role in ServiceStack Ecosystem

### Position
Optional plugin — not loaded by default. Consumers call `Plugins.Add(new OpenApiFeature())` in their `AppHost.Configure`.

### Dependencies (project references)
| Project | Role |
|---|---|
| `ServiceStack` | Core host — `IPlugin`, `IAppHost`, `ServiceMetadata`, `RestPath`, `HostContext`, `AuthFeature` |
| `ServiceStack.Common` | `HtmlEncode`, extension helpers |
| `ServiceStack.Client` | Client-side type utilities |
| `ServiceStack.Interfaces` | `IReturn<T>`, `IReturnVoid`, `IPlugin`, DTO attributes |
| `ServiceStack.Text` | `JsConfig`, JSON serialization scoping |

### Consumers / Dependents
Nothing in the main ServiceStack repository depends on this assembly. It is a leaf plugin package consumed directly by end-user applications.

### Relationship to Other OpenAPI Packages
| Package | Spec version | Notes |
|---|---|---|
| `ServiceStack.Api.OpenApi` (**this**) | Swagger 2.0 | Classic/legacy; bundled Swagger UI |
| `ServiceStack.AspNetCore.OpenApi` | OpenAPI 3.x | ASP.NET Core integration |
| `ServiceStack.OpenApi.Microsoft` | OpenAPI 3.x | Uses Microsoft.OpenApi |
| `ServiceStack.OpenApi.Swashbuckle` | OpenAPI 3.x | Uses Swashbuckle |

### Target Frameworks
`net472;net6.0;net8.0;net10.0`

On `net472`, uses the custom `OrderedDictionary<TKey,TValue>` from `Support/`. On `net10.0+`, the BCL's `OrderedDictionary<TKey,TValue>` is available and the `Support/` shim is not compiled in (`#if !NET10_0_OR_GREATER`).

---

## Key Functionality

### `OpenApiFeature` — the `IPlugin`
File: `OpenApiFeature.cs`

The entry point. Registered via `Plugins.Add(new OpenApiFeature())`.

**Key configuration properties:**
| Property | Purpose |
|---|---|
| `ResourceFilterPattern` | Regex string to include only matching operations (by type name or route path) |
| `ResourceFilterRegex` | Compiled `Regex` (1-second timeout) derived from `ResourceFilterPattern` |
| `UseCamelCaseSchemaPropertyNames` | Emit camelCase property names in schema definitions |
| `UseLowercaseUnderscoreSchemaPropertyNames` | Emit `lowercase_underscore` names in schema definitions |
| `DisableAutoDtoInBodyParam` | Suppress automatic body parameter generation for POST/PUT |
| `LogoHref` | Custom URL for the Swagger UI logo link (`<a href>`) |
| `LogoUrl` | Custom URL for the Swagger UI logo image (`<img src>`) |
| `Tags` | Additional `OpenApiTag` objects to include in the declaration |
| `AnyRouteVerbs` | Verbs to use when a route allows `Any` (default: GET/POST/PUT/DELETE) |
| `InlineSchemaTypesInNamespaces` | Namespaces whose types are inlined (not `$ref`-d) in the schema |
| `DisableSwaggerUI` | Suppress the Swagger UI; only serve the `/openapi` JSON endpoint |
| `SecurityDefinitions` | Global Swagger security scheme map |
| `OperationSecurity` | Per-operation security requirement map |
| `UseBearerSecurity` / `UseBasicSecurity` | Convenience setters to configure common schemes |
| `IgnoreRequest` | Predicate to exclude specific request types entirely |
| `ApiDeclarationFilter` | Post-process the entire `OpenApiDeclaration` |
| `OperationFilter` | Post-process each `OpenApiOperation` (verb + operation) |
| `SchemaFilter` | Post-process each `OpenApiSchema` |
| `SchemaPropertyFilter` | Post-process each `OpenApiProperty` |

**Lifecycle hooks:**
- `BeforePluginsLoaded`: Registers the assembly as an embedded resource source and adds "Swagger UI" to the metadata plugin links.
- `Register`: Compiles `ResourceFilterRegex`, auto-detects Bearer/Basic auth from `AuthFeature`, copies all configuration to `OpenApiService` static fields, registers `OpenApiService` at `/openapi`, and installs the CatchAll handler that serves Swagger UI HTML.

**Swagger UI serving (`CustomResponseHandler` inline lambda):**
- Reads `swagger-ui/index.html` from embedded resources.
- Replaces template tokens: the default Petstore URL to `~/openapi`, `ApiDocs` to HTML-encoded `ServiceName`, the logo link and image.
- Optionally injects `patch-preload.js` (before `window.swaggerUi.load()`) and `patch.js` (before `</body>`).
- Uses a **local `pageHtml` variable per request** (not the captured `html` template) to avoid cross-request mutation.

**URL sanitisation helpers (private static):**
- `SanitizeUrl(url, defaultUrl)` — rejects `javascript:`, `data:`, `vbscript:` schemes; HTML-encodes safe values.
- `SanitizeImageUrl(url)` — same but allows `data:` (e.g. inline base64 images), rejects `javascript:` and `vbscript:`.

---

### `OpenApiService` — the schema-generation service
File: `OpenApiService.cs`

A standard ServiceStack `Service` decorated with `[ExcludeMetadata]` and `[Restrict(VisibilityTo = RequestAttributes.None)]` (hidden from metadata, accessible via direct route).

**Request DTO:** `OpenApiSpecification` (maps to `GET /openapi`; accepts optional `apiKey` query param).

**Main method: `Get(OpenApiSpecification)`**
1. Resolves `ResourceFilterRegex` from `HostContext.GetPlugin<OpenApiFeature>()` with fallback to static field.
2. Iterates `HostContext.ServiceController.RestPathMap`, filtering by visibility and the resource filter regex.
3. Calls `ParseDefinitions` for every (path, verb) to recursively build the `definitions` dictionary.
4. Calls `ParseOperations` to build `OrderedDictionary<string, OpenApiPath>`.
5. Assembles and returns `OpenApiDeclaration` wrapped in `HttpResult` with a scoped `JsConfig` that suppresses null values and type info.
6. Applies `SchemaFilter`, `OperationFilter`, and `ApiDeclarationFilter` callbacks.

**Schema generation pipeline:**
| Method | Role |
|---|---|
| `ParseDefinitions` | Recursively registers CLR types into the `schemas` dict using `GetSchemaDefinitionRef` as key |
| `GetSchemaDefinitionRef(type)` | Sanitized schema key — replaces `[^A-Za-z0-9.\-_]` with `_` (e.g. `MyGeneric<string>` becomes `MyGeneric_string_`) |
| `GetSchemaTypeName(type)` | Human-readable name, may contain `<>` — **only for display/title**, never for `$ref` or dict key |
| `GetOpenApiProperty` | Maps a CLR `PropertyInfo` or `Type` to `OpenApiProperty` (scalars, lists, dicts, enums, refs, inline schemas) |
| `GetParameter` | Maps a CLR type to `OpenApiParameter` for a specific `paramIn` location |
| `ParseParameters` | Iterates request DTO properties, classifies each as `path`/`query`/`formData`/`body` |
| `ParseOperations` | Builds `OpenApiPath` entries with per-verb `OpenApiOperation` objects |
| `GetResponseSchema` / `GetSchemaForResponseType` | Extracts `IReturn<T>` / `IReturnVoid` response type and builds schema reference |
| `GetMethodResponseCodes` | Produces `OrderedDictionary<string, OpenApiResponse>` including `[ApiResponse]` attrs |
| `IsInlineSchema(type)` | Returns `true` if type's namespace is in `InlineSchemaTypesInNamespaces` |
| `GetOperationName` | Generates unique `operationId` (verb postfix + path postfix + numeric dedup) |

**CLR to Swagger type mapping:**
- Scalars: `bool`, `int`, `long`, `float`, `double`, `decimal`, `string`, `DateTime`, `DateTimeOffset`, `byte[]`, `sbyte[]`, nullable versions.
- Enums: string enum (names) or numeric enum (value + name).
- Lists/arrays → `type: array` with `items`.
- Dictionaries → `type: object` with `additionalProperties`.
- `KeyValuePair<,>` → object with `Key`/`Value` properties.
- All other types → `$ref: #/definitions/<SchemaDefinitionRef>`.

**Configuration resolution pattern (multi-host safe):**
All configuration reads first try `HostContext.GetPlugin<OpenApiFeature>()?.Property` then fall back to the static field. Example:
```csharp
var feature = HostContext.GetPlugin<OpenApiFeature>();
var resourceFilter = feature?.ResourceFilterRegex ?? ResourceFilterRegex;
```

---

### Specification Models (`Specification/`)

Pure data-transfer objects representing Swagger 2.0 document structure. All use `[DataContract]`/`[DataMember]` for serialization control.

| Class | Swagger 2.0 Object |
|---|---|
| `OpenApiDeclaration` | Root document (`swagger: "2.0"`) |
| `OpenApiInfo` | `info` object (title, version, description, terms, contact, license) |
| `OpenApiPath` | Path item (holds per-verb `OpenApiOperation` refs) |
| `OpenApiOperation` | Operation object (operationId, parameters, responses, tags, security) |
| `OpenApiParameter` | Parameter object (in, name, type, schema, required) |
| `OpenApiSchema` | Schema object (type, properties, allOf, required, enum, etc.) |
| `OpenApiProperty` | Property within a schema |
| `OpenApiResponse` | Response object (description, schema) |
| `OpenApiSecuritySchema` | Security definition (type, name, in, flow, etc.) |
| `OpenApiSecurity` | Security requirement |
| `OpenApiTag` | Tag object (name, description, externalDocs) |
| `OpenApiContact` | Contact info |
| `OpenApiLicense` | License info |
| `OpenApiType` | String constants: `"string"`, `"integer"`, `"number"`, `"boolean"`, `"array"`, `"object"` |
| `OpenApiDataTypeSchema` | Data type format constants |
| `OpenApiXmlObject` | XML object metadata |
| `OpenApiExternalDocumentation` | External docs object |

`OpenApiDeclaration.Swagger` is a computed property that always returns `"2.0"`.

`OpenApiDeclaration.Responses` is **not initialised by default** (`null`). Always use `result.Responses?.Each(...)` — never `result.Responses.Each(...)`.

---

### Support Utilities (`Support/`)

- `OrderedDictionary<TKey, TValue>` + `IOrderedDictionary<TKey, TValue>` — insertion-ordered generic dictionary used everywhere schemas and paths are collected, ensuring deterministic JSON output. Compiled only for targets below `net10.0`; `net10.0+` uses the BCL version via `#if !NET10_0_OR_GREATER`.

---

### Swagger UI Assets (`swagger-ui/`)

Bundled static files (JS, CSS, fonts, images, HTML) for the classic Swagger UI 2.x.

- `index.html` — main page; template tokens replaced at request time.
- `swagger-ui.js` / `swagger-ui.min.js` — Swagger UI client-side JavaScript.
- `patch-preload.js` — injected before `window.swaggerUi.load()` for customization.
- `patch.js` — injected before `</body>` for customization.
- `o2c.html` — OAuth2 redirect callback page.

All files are embedded as resources via `<EmbeddedResource Include="swagger-ui\**\*.*" />`.

---

## Architecture & Design Patterns

### Plugin Registration
`OpenApiFeature` implements both `IPlugin` and `IPreInitPlugin`. `BeforePluginsLoaded` runs before other plugins to register the embedded resource assembly. `Register` wires everything together.

### Static Field Propagation (legacy pattern)
`OpenApiFeature.Register` copies configuration into `internal static` fields on `OpenApiService`. This is a legacy pattern predating multi-host support. Current code always reads from `HostContext.GetPlugin<OpenApiFeature>()` first, falling back to the static fields. **Do not remove the static-field fallback** — it retains backward compatibility.

### Schema Dictionary Keying
The `definitions` dictionary is keyed by `GetSchemaDefinitionRef(type)` — the sanitized, `$ref`-safe identifier. Display names (`GetSchemaTypeName`) contain generic syntax (`<>`) and must **never** be used as dictionary keys or `$ref` values.

### Ordered Output
`OrderedDictionary<string, OpenApiPath>` and `OrderedDictionary<string, OpenApiResponse>` preserve the registration order of routes and responses, producing stable, deterministic JSON for diffing and client generation.

### Inline vs. Referenced Schemas
Types whose namespace is listed in `InlineSchemaTypesInNamespaces` are inlined directly into the property rather than referenced via `$ref`. Their schemas are still registered in `definitions` but excluded from the final `OpenApiDeclaration.Definitions` output.

### JSON Serialization Scope
The `Get` method returns an `HttpResult` with a `ResultScope` that activates a `JsConfig` suppressing null values, null dictionary values, and type info — essential for producing clean, spec-compliant Swagger JSON.

### `DataContract` / `DataMember` Ordering
When a DTO uses `[DataContract]` and has `[DataMember]` attributes with explicit `Order`, `ParseDefinitions` sorts properties accordingly (base types last, then by `Order`, then by name) to match WCF-compatible ordering rules.

---

## Security & Reliability Considerations

See `SECURITY_CHANGES.md` for the full remediation log. Key points:

### 1. XSS in Swagger UI HTML Template
**Risk:** `HostContext.ServiceName`, `LogoHref`, `LogoUrl` are injected into HTML.

**Mitigations applied:**
- `ServiceName` is HTML-encoded via `HtmlEncode()` before substitution.
- `LogoHref` is passed through `SanitizeUrl()` — rejects `javascript:`, `data:`, `vbscript:` schemes and HTML-encodes the result.
- `LogoUrl` is passed through `SanitizeImageUrl()` — rejects `javascript:` and `vbscript:` (allows `data:` for base64 images) and HTML-encodes the result.
- Template replacement uses a **local `pageHtml` variable** per request, not the captured template string, preventing state leakage across requests.

**When modifying** the Swagger UI serving code: any new value interpolated into HTML must be HTML-encoded. Any new URL inserted into an `href` or `src` attribute must be scheme-validated with `SanitizeUrl`/`SanitizeImageUrl` before encoding.

### 2. ReDoS via `ResourceFilterPattern`
**Risk:** A pathological user-supplied regex can cause catastrophic backtracking.

**Mitigation applied:** `ResourceFilterRegex` is compiled with `TimeSpan.FromSeconds(1)` timeout. Matches that exceed 1 second throw `RegexMatchTimeoutException` rather than hanging indefinitely.

**When modifying:** Never compile `ResourceFilterRegex` without the timeout argument.

### 3. `NullReferenceException` when `SchemaFilter` is Set
**Risk:** `OpenApiDeclaration.Responses` is `null` by default; iterating it without null-check crashes when `SchemaFilter != null`.

**Mitigation applied:** `result.Responses?.Each(...)` — null-conditional operator guards the iteration.

**When modifying:** Any code that accesses `result.Responses` or `OpenApiDeclaration.Responses` must use `?.` or an explicit null-check.

### 4. `KeyNotFoundException` / Broken `$ref` for Generic Types
**Risk:** Using `GetSchemaTypeName` (returns `MyGeneric<string>`) as a schema dictionary key or `$ref` value causes `KeyNotFoundException` and emits invalid JSON Pointer characters.

**Mitigation applied:** All schema dictionary lookups and `$ref` construction use `GetSchemaDefinitionRef` (sanitized form, e.g. `MyGeneric_string_`).

**Rule:** `GetSchemaDefinitionRef` for dictionary keys and `$ref` strings. `GetSchemaTypeName` for human-readable `title`/`description` only.

### 5. Route Parameter Detection for Wildcards and Constraints
**Risk:** Routes like `{Path*}` (wildcard) or `{Id:int}` (constraint) were not recognised as path parameters.

**Mitigation applied:** `ParseParameters` checks three patterns:
```csharp
routeLower.Contains("{" + propNameLower + "}")   // exact match
routeLower.Contains("{" + propNameLower + "*")    // wildcard suffix
routeLower.Contains("{" + propNameLower + ":")    // constraint separator
```

**When modifying:** If ServiceStack adds new route parameter syntax, update all three checks in `ParseParameters`.

### 6. Multi-Host Isolation
**Risk:** Static fields on `OpenApiService` are overwritten by the last registered host in multi-host scenarios (integration tests, multi-tenant setups).

**Mitigation applied:** All configuration reads in `OpenApiService` use `HostContext.GetPlugin<OpenApiFeature>()?.Property ?? StaticField` so the active host's plugin configuration takes precedence.

**When adding new configuration:** Always follow the same resolution pattern — check plugin instance first, fall back to static field.

---

## Common Modification Scenarios

### Adding a new `OpenApiFeature` configuration option
1. Add a property to `OpenApiFeature`.
2. Add a corresponding `internal static` field on `OpenApiService`.
3. In `OpenApiFeature.Register`, copy the value: `OpenApiService.NewField = NewProperty;`.
4. In `OpenApiService` where the value is consumed, resolve via:
   ```csharp
   var value = HostContext.GetPlugin<OpenApiFeature>()?.NewProperty ?? NewField;
   ```

### Adding support for a new CLR scalar type / mapping
- Add to `ClrTypesToSwaggerScalarTypes` and `ClrTypesToSwaggerScalarFormats` in `OpenApiService`.
- Update `IsSwaggerScalarType` if the type is a value type not covered by existing logic.

### Fixing schema generation for a new DTO pattern
- `ParseDefinitions` is the entry point for DTO-to-schema conversion. Trace through `GetOpenApiProperty` → `GetParameter` → `GetSchemaForResponseType` depending on whether the issue is in schema definitions, operation parameters, or response schemas.
- Always use `GetSchemaDefinitionRef` for any `$ref` or dictionary key; use `GetSchemaTypeName` only for `title`/`description`.

### Modifying the Swagger UI
- Edit `swagger-ui/index.html` or the patch scripts (`patch.js`, `patch-preload.js`).
- Template tokens replaced at request time: `http://petstore.swagger.io/v2/swagger.json`, `ApiDocs`, `<span class="logo__title">swagger</span>`, `http://swagger.io`, `images/logo_small.png`.
- Any new dynamic value inserted into HTML must be HTML-encoded. Any value inserted into a URL attribute must be sanitized via `SanitizeUrl` / `SanitizeImageUrl`.

### Adding a new filter/callback hook
1. Add `Action<...>` property to `OpenApiFeature`.
2. Add matching `internal static Action<...>` to `OpenApiService`.
3. Copy in `Register`.
4. Apply in `OpenApiService` via `HostContext.GetPlugin<OpenApiFeature>()?.NewFilter ?? NewFilter`.
5. Guard null before invocation.

### Adjusting JSON serialization of the generated document
- Serialization is controlled by the `JsConfig` scope in `OpenApiService.Get`. Modify the `Config` object passed to `JsConfig.With(...)` to change output format.
- Note: `[SystemJson(UseSystemJson.Never)]` on `OpenApiSpecification` prevents System.Text.Json from handling the response on .NET 8+; ServiceStack.Text is always used.

### Supporting a new HTTP verb
- Add to `AnyRouteVerbs` default list in `OpenApiFeature` constructor.
- Add a case to the `switch (verb)` block in `ParseOperations` and to `GetOperations` in `OpenApiService`.
- Add a postfix entry to the `postfixes` dictionary in `OpenApiService`.
