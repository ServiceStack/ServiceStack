# CONTEXT: ServiceStack.AspNetCore.OpenApi

## Purpose

`ServiceStack.AspNetCore.OpenApi` provides OpenAPI 3.x specification generation for ServiceStack services
hosted in ASP.NET Core applications. It integrates ServiceStack's service metadata and DTO type system
into [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)'s schema generation
pipeline, producing a standards-compliant OpenAPI document consumable by Swagger UI, code generators,
and API explorers.

This is an **optional add-on plugin**. It is not loaded unless the application explicitly registers it.
It bridges two independent metadata systems: ServiceStack's `ServiceMetadata` / `Operation` model, and
the `Microsoft.OpenApi.Models` object graph that Swashbuckle serializes.

**Target frameworks**: `net8.0`, `net10.0`

---

## Role in ServiceStack Ecosystem

### Dependencies (upstream)
| Project | Purpose |
|---|---|
| `ServiceStack` (core) | `ServiceMetadata`, `Operation`, `HostContext`, `IRestPath`, attribute types |
| `ServiceStack.Interfaces` | `IReturn<T>`, `IReturnVoid`, `Feature`, DTO attribute contracts |
| `ServiceStack.Common` | `TypeProperties`, serialization helpers, `JsConfig` |
| `ServiceStack.Extensions` | ASP.NET Core integration (`ServiceStackOptions`, endpoint routing) |
| `ServiceStack.Client` | Shared client-facing types, `MimeTypes` |
| `Microsoft.OpenApi` v1.x | `OpenApiDocument`, `OpenApiSchema`, `OpenApiOperation`, etc. |
| `Swashbuckle.AspNetCore` v8.x | `IDocumentFilter`, `SwaggerGenOptions`, Swagger middleware |
| `Microsoft.AspNetCore.OpenApi` v8.x | ASP.NET Core endpoint metadata integration |

### Consumers (downstream)
- Any ASP.NET Core application that hosts ServiceStack with endpoint routing enabled
  (`MapEndpointRouting = true`) and calls `AddSwagger` / `AddServiceStackSwagger`.
- Swagger UI at `/swagger/index.html`, registered automatically via `MetadataFeature`.

### Relationship to other ServiceStack OpenAPI packages
There is a sibling package `ServiceStack.OpenApi.Swashbuckle` that targets `Microsoft.OpenApi` v2.x and
`Swashbuckle.AspNetCore` v10.x for .NET 10+ environments. **This package pins to the v1.x/v8.x
generation of these dependencies.** See the version conflict warning in
[`ServiceStackOpenApiExtensions`](ServiceStackOpenApiExtensions.cs).

---

## Key Functionality

### `OpenApiMetadata` ([OpenApiMetadata.cs](OpenApiMetadata.cs))
The central configuration and conversion class. Holds all per-instance state (schemas cache, security
definitions, inline namespace rules, filters). Key responsibilities:

- **CLR to OpenAPI type mapping**: `ClrTypesToSwaggerScalarTypes` and `ClrTypesToSwaggerScalarFormats`
  dictionaries map .NET primitives to OpenAPI `type`/`format` strings.
- **`CreateSchema(Type, ...)`**: Converts a DTO type into an `OpenApiSchema`. Handles enums, collections,
  dictionaries, `KeyValuePair<,>`, and complex object types. Respects `[DataContract]` / `[DataMember]`
  ordering. Caches results in `Schemas` (`ConcurrentDictionary<string, OpenApiSchema>`).
- **`AddOperation(OpenApiOperation, Operation, verb, route)`**: Populates a Swashbuckle
  `OpenApiOperation` from a ServiceStack `Operation`. Handles:
  - Summary / description from `[Description]` / `[Notes]` attributes.
  - Request body vs. query/path parameter routing based on HTTP verb.
  - Path parameter extraction with wildcard (`{Id*}`) and constraint (`{Id:int}`) support.
  - Form and JSON content types for request bodies.
  - Security requirements from `RequiresAuthentication` / `RequiresApiKey`.
  - Tag overrides from `[Tag]` attributes.
- **`GetSchemaDefinitionRef(Type)`**: Produces the schema component key. All non-alphanumeric characters
  (e.g. `<`, `>`, `,`, space in generic names) are replaced with `_` via `schemaRefRegex`, producing
  RFC 6901-safe JSON Pointer references (e.g. `MyResult<String>` becomes `MyResult_String_`).
- **`GetMethodResponseCodes(...)`**: Builds response schemas from `IReturn<T>` / `IReturnVoid` and
  `[ApiResponse]` attributes. Emits `204 No Content` (no `Content` dictionary) when appropriate.
- **`InlineSchemaTypesInNamespaces`**: Instance list of namespaces whose types should be inlined
  (embedded) rather than referenced via `$ref`. **Instance property, not static** — avoids cross-host
  contamination in multi-tenant or test scenarios.

#### Security preset helpers
`OpenApiSecurity` (static class) provides ready-made `OpenApiSecurityScheme` /
`OpenApiSecurityRequirement` presets for:
- HTTP Basic auth (`BasicAuthScheme` / `BasicAuth`)
- JWT Bearer (`JwtBearerScheme` / `JwtBearer`)
- API Key (`ApiKeyScheme` / `ApiKey`)

Called via `metadata.AddBasicAuth()`, `AddJwtBearer()`, `AddApiKeys()`.

---

### `ServiceStackDocumentFilter` ([ServiceStackDocumentFilter.cs](ServiceStackDocumentFilter.cs))
Implements Swashbuckle's `IDocumentFilter`. Runs **last** in the filter pipeline (registered by
`ConfigureServiceStackSwagger`). Responsibilities:

1. Registers security scheme definitions into `swaggerDoc.Components.SecuritySchemes`.
2. Traverses `HostContext.Metadata.OperationsMap` to discover all DTO types reachable from registered
   operations via `AddReferencedTypes` (a recursive walk of properties and generic arguments).
3. Calls `metadata.CreateSchema(type)` for each discovered type and registers the result under
   `swaggerDoc.Components.Schemas[GetSchemaDefinitionRef(type)]`.

**`IsDtoTypeOrEnum(Type)`**: Gate predicate that accepts only ServiceStack DTO types or enums, excluding
open generic type definitions and types that opt out via `[ExcludeFeature(Feature.Metadata)]` or
`[ExcludeFeature(Feature.ApiExplorer)]`.

**`AddReferencedTypes`**: Recursive type graph walker. Skips any `PropertyInfo` for which
`SwaggerUtils.IgnoreProperty` returns `true` — this is the critical guard against leaking internal or
sensitive property types into the schema registry.

---

### `SwaggerUtils` ([SwaggerUtils.cs](SwaggerUtils.cs))
Single utility class with one replaceable predicate:

```csharp
public static Func<PropertyInfo, bool> IgnoreProperty { get; set; } = DefaultIgnoreProperty;
```

**`DefaultIgnoreProperty(PropertyInfo pi)`** returns `true` (exclude from schema) if the property has
any of:
- `[Obsolete]` — deprecated properties
- `[JsonIgnore]` — System.Text.Json exclusion
- `[IgnoreDataMember]` — ServiceStack's primary attribute for internal/sensitive fields
- `[SwaggerIgnore]` — explicit Swashbuckle exclusion

The predicate is **replaceable** — applications can substitute their own logic. However, the built-in
defaults must cover all standard ServiceStack exclusion patterns; removing any of them risks information
disclosure.

---

### `ServiceStackOpenApiExtensions` ([ServiceStackOpenApiExtensions.cs](ServiceStackOpenApiExtensions.cs))
Extension methods in the `ServiceStack` namespace (not the `ServiceStack.AspNetCore.OpenApi` namespace):

| Method | Target | Description |
|---|---|---|
| `WithOpenApi(ServiceStackOptions)` | `ServiceStackOptions` | Hooks `OpenApiMetadata.Instance.AddOperation` into every endpoint's route builder. Requires `MapEndpointRouting = true`. |
| `AddSwagger(ServiceStackServicesOptions, ...)` | `ServiceStackServicesOptions` | Registers `OpenApiMetadata`, `ConfigureServiceStackSwagger`, and the Swagger UI link in `MetadataFeature`. |
| `AddServiceStackSwagger(IServiceCollection, ...)` | `IServiceCollection` | Same as above but accepts a raw `IServiceCollection` for use outside the `AddServiceStack` callback. |
| `AddBasicAuth<TUser>(IServiceCollection)` | `IServiceCollection` | Combines ASP.NET Core Identity basic auth handler with `OpenApiMetadata.AddBasicAuth()`. |
| `AddJwtAuth(IServiceCollection)` | `IServiceCollection` | Calls `OpenApiMetadata.AddJwtBearer()`. |

Both `AddSwagger` and `AddServiceStackSwagger` have **two overloads**: one uses `OpenApiMetadata.Instance`
(singleton default), the other accepts an explicit `OpenApiMetadata` instance for isolated DI containers.

---

### `ConfigureServiceStackSwagger` ([ConfigureServiceStackSwagger.cs](ConfigureServiceStackSwagger.cs))
Implements both `IConfigureOptions<SwaggerGenOptions>` and `IConfigureOptions<ServiceStackOptions>`.
Wires up:
- All `DocumentFilterTypes` from the `OpenApiMetadata` instance into `SwaggerGenOptions.DocumentFilterDescriptors`
  (with the metadata instance passed as a constructor argument, enabling DI-free filter instantiation).
- All `SchemaFilterTypes` into `SwaggerGenOptions.SchemaFilterDescriptors`.
- Calls `options.WithOpenApi()` to activate the endpoint-level operation enrichment.

---

### Type/Format Constants
- **`OpenApiType`** ([OpenApiType.cs](OpenApiType.cs)): String constants for OpenAPI primitive type
  names (`array`, `boolean`, `integer`, `number`, `object`, `string`).
- **`OpenApiTypeFormat`** ([OpenApiTypeFormat.cs](OpenApiTypeFormat.cs)): String constants for OpenAPI
  format strings (`byte`, `binary`, `date`, `date-time`, `double`, `float`, `int32`, `int64`,
  `password`).

### `OrderedDictionary<TKey, TValue>` ([OrderedDictionary.cs](OrderedDictionary.cs))
A custom insertion-ordered dictionary used for `schema.Properties` to preserve declaration order in
generated JSON. This is important for Swagger UI readability and to honour `[DataMember(Order=N)]`.

---

## Architecture & Design Patterns

### Integration Point: Swashbuckle Document Filter
ServiceStack does not use Swashbuckle's reflection-based schema generator for its own types. Instead,
`ServiceStackDocumentFilter` runs after Swashbuckle's built-in filters and **directly mutates**
`swaggerDoc.Components.Schemas` and `swaggerDoc.Components.SecuritySchemes`. This avoids conflicts with
Swashbuckle's own type scanning and gives ServiceStack full control over its schema shape.

### Integration Point: ASP.NET Core Endpoint Metadata
For operations (path+verb pairs), `ServiceStackOpenApiExtensions.WithOpenApi` registers a
`RouteHandlerBuilder` callback that calls `builder.WithOpenApi(op => ...)` for each ServiceStack
endpoint. This hook is invoked by ASP.NET Core's endpoint metadata pipeline and allows enriching the
`OpenApiOperation` produced by `Microsoft.AspNetCore.OpenApi` before Swashbuckle serializes it.

### Schema Caching
`OpenApiMetadata.Schemas` is a `ConcurrentDictionary<string, OpenApiSchema>`. `CreateSchema` checks
this cache first and returns immediately on hit, preventing redundant schema construction and infinite
recursion for self-referential types.

### Property Exclusion Pipeline
Properties flow through two separate gates:
1. **`SwaggerUtils.IgnoreProperty`** — gates `AddReferencedTypes` (type graph traversal) and
   `CreateSchema` property enumeration (`GetProperties().Where(!IgnoreProperty)`).
2. **`[IgnoreDataMember]` check** inside `CreateSchema`'s `parseProperties` loop — a secondary guard
   ensuring that even if a property passes gate 1, its schema entry is still omitted.

### Schema Key Sanitization
`GetSchemaDefinitionRef` normalises any type name to match `^[a-zA-Z0-9.\-_]+$` using:

```csharp
private static readonly Regex schemaRefRegex =
    new("[^A-Za-z0-9\\.\\-_]", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
```

The 1-second timeout prevents ReDoS on pathological type names.

### Parameter Location Resolution
For **GET/DELETE/HEAD** (no request body) — `CreateParameters` resolves each property against the route
template to set `ParameterLocation.Path` vs. `ParameterLocation.Query`:

```csharp
var inPath = route.Contains("{" + propertyName + "}")
          || route.Contains("{" + propertyName + "*")   // wildcard catch-all
          || route.Contains("{" + propertyName + ":");  // route constraint
```

The same triple-check is applied in `AddOperation` when stripping path-bound properties from the request
body schema.

### Text Case Awareness
`GetSchemaPropertyName` honours `JsConfig.TextCase` (`CamelCase`, `SnakeCase`, or default PascalCase),
so the generated schema property names match the actual serialisation format of the service.

---

## Security & Reliability Considerations

### 1. Sensitive Property Information Disclosure
**`[IgnoreDataMember]` is ServiceStack's primary exclusion attribute.** Any property decorated with it
(passwords, tokens, internal state) must be excluded from schema generation and the type graph walk.
`SwaggerUtils.DefaultIgnoreProperty` must include `IgnoreDataMemberAttribute`. Removing or bypassing
this check will cause sensitive types to appear in `components/schemas`.

If replacing `SwaggerUtils.IgnoreProperty` with a custom predicate, ensure the replacement also handles
`IgnoreDataMemberAttribute`, `ObsoleteAttribute`, `JsonIgnoreAttribute`, and
`SwaggerIgnoreAttribute`.

### 2. Generic Type Schema Key Validity (RFC 6901)
Generic type names from `Type.ToPrettyName()` contain `<`, `>`, `,`, and spaces, which are **illegal
in JSON Pointer** segments and break OpenAPI document validation. Always use `GetSchemaDefinitionRef`
(not `GetSchemaTypeName`) when indexing `components/schemas` or generating `$ref` values. Both
`ToOpenApiReference` and `ServiceStackDocumentFilter` are aligned to use `GetSchemaDefinitionRef`.
Do not bypass this sanitization.

### 3. HTTP 204 No Content Compliance (RFC 9110)
When `HostContext.Config.Return204NoContentForEmptyResponse == true` and a service returns
`IReturnVoid`, the response status code becomes `"204"`. In this case the `OpenApiResponse.Content`
dictionary **must be omitted** (not set to an empty schema). The check in `GetMethodResponseCodes` is:

```csharp
if (responseSchema != null && statusCode != "204")
{
    okResponse.Content[MimeTypes.Json] = ...;
}
```

Do not add `Content` to 204 responses — validators and strict clients will reject the document.

### 4. Instance Isolation (`InlineSchemaTypesInNamespaces`)
`InlineSchemaTypesInNamespaces` is an **instance property** on `OpenApiMetadata`. Never promote it to a
static field or property. Multiple AppHosts sharing the same process (integration tests, multi-tenant
scenarios) each need independent configuration. The `AddSwagger`/`AddServiceStackSwagger` overloads
that accept an explicit `OpenApiMetadata` instance exist precisely to support this.

### 5. `MapEndpointRouting` Requirement
`WithOpenApi()` throws `NotSupportedException` if `MapEndpointRouting` is not enabled. Check this
guard when adding new code paths that conditionally call `WithOpenApi`.

### 6. Package Version Conflicts
This package pins to `Microsoft.OpenApi` v1.x, `Swashbuckle.AspNetCore` v8.x, and
`Microsoft.AspNetCore.OpenApi` v8.x. Mixing v2.x/v10.x of these packages causes
`TypeInitializationException` at startup. The `AddServiceStackSwagger` extension catches this and
prints a diagnostic message pointing to the `ServiceStack.OpenApi.Swashbuckle` alternative.

---

## Common Modification Scenarios

### Adding a new schema type mapping
Add entries to `ClrTypesToSwaggerScalarTypes` and `ClrTypesToSwaggerScalarFormats` in `OpenApiMetadata`.
Verify that `IsSwaggerScalarType` picks them up correctly (it uses `ContainsKey` on these dictionaries).

### Supporting a new attribute that should suppress a property from schemas
Add the attribute type to `SwaggerUtils.DefaultIgnoreProperty`'s predicate. Ensure the change is
applied in both the type-graph walker (`AddReferencedTypes`) and the schema property loop
(`CreateSchema`'s `parseProperties` block).

### Adding a custom document filter
Add the filter type to `metadata.DocumentFilterTypes`. `ConfigureServiceStackSwagger.Configure` will
register it with Swashbuckle, passing the `OpenApiMetadata` instance as a constructor argument if the
filter accepts it.

### Adding new security scheme support
Add `OpenApiSecurityScheme` / `OpenApiSecurityRequirement` static presets to `OpenApiSecurity`.
Add a corresponding `Add*()` convenience method on `OpenApiMetadata` that sets
`SecurityDefinition`/`SecurityRequirement` (or the `ApiKey*` variants for non-primary schemes).
Wire up an extension method on `IServiceCollection` in `ServiceStackOpenApiExtensions`.

### Adding a new operation-level attribute
In `OpenApiMetadata.AddOperation`, read the attribute from `operation.RequestType` and mutate the
`OpenApiOperation` (`op`) accordingly. Call your logic before `OperationFilter?.Invoke(...)` so that
user-supplied filters can override it.

### Changing how response codes are generated
Modify `GetMethodResponseCodes`. The method resolves the return type from `IReturn<T>` / `IReturnVoid`
interfaces and supplements with `[ApiResponse]` attributes. Always preserve the 204 No Content guard.

### Changing property name serialisation
`GetSchemaPropertyName` drives property names in schema output. It is already `JsConfig.TextCase`-aware.
If new serialisation modes are added to ServiceStack, update this method to match.

### Updating Swashbuckle / Microsoft.OpenApi package versions
Consult the package version conflict notes above. A move from `Microsoft.OpenApi` v1 to v2 requires
switching to the `ServiceStack.OpenApi.Swashbuckle` package. Within the v1.x line, update the
`ServiceStack.AspNetCore.OpenApi.csproj` PackageReference versions and run the full test suite
against a live Swagger UI to verify schema output.
