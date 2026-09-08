# CONTEXT: ServiceStack.OpenApi.Microsoft

## Purpose

`ServiceStack.OpenApi.Microsoft` integrates ServiceStack with modern **ASP.NET Core OpenAPI** (`Microsoft.AspNetCore.OpenApi` / `Microsoft.OpenApi` 2.x). It generates OpenAPI v3.0 specifications dynamically from ServiceStack's service models and metadata for consumption by Swagger UI, Scalar, OpenAPI generators, and API gateways.

Key capabilities:
- **Native Document Transformer**: Employs `ServiceStackDocumentTransformer` (`IOpenApiDocumentTransformer`) to inject ServiceStack endpoints, operations, and DTO schemas into ASP.NET Core's OpenAPI document pipeline (`MapOpenApi`).
- **OpenAPI v3.0 / RFC 6901 Compliant**: Generates compliant schema identifiers for generic types (`QueryResponse<T>` -> `QueryResponse_T_`) and JSON Pointers.
- **DTO Metadata Reflection**: Translates ServiceStack attributes (`[Route]`, `[Api]`, `[ApiMember]`, `[Validate]`, `[Tag]`, `[Notes]`) into OpenAPI parameters, summaries, descriptions, and validation schemas.
- **Route Constraint & Wildcard Support**: Maps route parameters (`{id:int}`, `{path*}`) correctly to path parameters.

**Target frameworks**: `net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack (Core)       Microsoft.AspNetCore.OpenApi (10.x)
         │                               │
         └───────────────┬───────────────┘
                         ▼
           ServiceStack.OpenApi.Microsoft
                         │
                         ▼
OpenAPI v3.0 Endpoints & Interactive Docs (Scalar, Swagger UI, ReDoc)
```

- **Depends on**: `ServiceStack`, `ServiceStack.Interfaces`, `ServiceStack.Common`, `ServiceStack.Client`, `ServiceStack.Extensions`, `Microsoft.OpenApi`, `Microsoft.AspNetCore.OpenApi`.
- **Depended on by**: Modern .NET 10+ web APIs using ASP.NET Core's native OpenAPI endpoints (`builder.Services.AddOpenApi()`).
- **Sibling Implementations**:
  - `ServiceStack.OpenApi.Swashbuckle` (integrates via Swashbuckle).
  - `ServiceStack.Api.OpenApi` (legacy self-contained Swagger 2.0 / OpenAPI 2 plugin).

---

## Key Functionality

### Primary Types
| Class | Role |
|---|---|
| `ServiceStackDocumentTransformer` | Implements ASP.NET Core `IOpenApiDocumentTransformer`, converting ServiceStack metadata into `OpenApiDocument` schemas and paths. |
| `OpenApiMetadata` | Metadata inspector translating DTO types, properties, routes, and data annotations into OpenAPI schemas. |
| `ServiceStackOpenApiExtensions` | Dependency injection extensions (`services.AddServiceStackOpenApi()`). |
| `OpenApiUtils` | Schema utility methods and property exclusion rules. |

---

## Architecture & Design Patterns

### ASP.NET Core Document Transformer Pattern
Rather than hosting a standalone JSON route handler, this package hooks into ASP.NET Core's native OpenAPI builder via `services.AddOpenApi(options => options.AddDocumentTransformer<ServiceStackDocumentTransformer>())`.

### Generic Schema Normalization
OpenAPI 3 and RFC 6901 JSON Pointers prohibit angle brackets and commas in schema references. Generic types like `QueryResponse<ItemDto>` are normalized via safe regex into valid tokens (`QueryResponse_ItemDto_`).

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Maintain these remediations:

### 1. Sensitive Property Exclusion (`[IgnoreDataMember]`)
- `OpenApiUtils.DefaultIgnoreProperty` must inspect and exclude properties marked with `[IgnoreDataMember]` in addition to `[JsonIgnore]` to prevent sensitive fields (passwords, secrets) from being exposed in public OpenAPI definitions.

### 2. Generic Schema Reference Sanitization
- In `OpenApiMetadata.GetSchemaDefinitionRef`, generic type names must be sanitized via regex (`[^A-Za-z0-9\.\-_]`) to prevent breaking OpenAPI tooling and schema validators.

### 3. Route Parameter Wildcards and Constraints
- Route path matching must recognize `{name}`, `{name*`, and `{name:`, preventing wildcard or constrained route parameters from leaking into query parameters.

### 4. RFC 9110 HTTP 204 No Content Compliance
- For operations returning HTTP 204 No Content, omit the `content` map entirely to adhere strictly to OpenAPI and HTTP specifications.

### 5. Multi-Tenant Instance Isolation
- `InlineSchemaTypesInNamespaces` and metadata options must be instance-level (not static) to prevent configuration cross-contamination across multi-tenant or concurrent test hosts.

---

## Common Modification Scenarios

1. **Configuring ServiceStack OpenAPI in ASP.NET Core**
   ```csharp
   builder.Services.AddOpenApi();
   builder.Services.AddServiceStackOpenApi();
   ```

2. **Customizing Security Definitions**
   - Configure JWT Bearer or API Key schemes on `OpenApiMetadata.SecurityDefinition`.
