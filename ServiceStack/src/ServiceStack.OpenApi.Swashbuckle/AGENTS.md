# CONTEXT: ServiceStack.OpenApi.Swashbuckle

## Purpose

`ServiceStack.OpenApi.Swashbuckle` integrates ServiceStack's service metadata with **Swashbuckle.AspNetCore** (`SwaggerGen`). It allows ASP.NET Core applications using Swashbuckle to automatically discover, document, and expose all ServiceStack services alongside any standard ASP.NET Core controllers in Swagger UI and `/swagger/v1/swagger.json`.

Key capabilities:
- **`ServiceStackDocumentFilter`**: An `IDocumentFilter` that injects ServiceStack REST paths, operations, parameters, and DTO schemas into Swashbuckle's `OpenApiDocument`.
- **OpenAPI v3.0 & RFC 6901 Compliant**: Sanitizes generic type names (`QueryResponse_ItemDto_`) to comply with JSON Pointer specifications.
- **ServiceStack Metadata Inspection**: Translates `[Route]`, `[Api]`, `[ApiMember]`, `[Notes]`, `[Tag]`, and validation attributes into Swagger parameter types, descriptions, and schemas.
- **Security Scheme Integration**: Built-in helper extensions for configuring JWT Bearer, API Key, and Basic Auth definitions in SwaggerGen.

**Target frameworks**: `net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack (Core)       Swashbuckle.AspNetCore (10.x)
         │                               │
         └───────────────┬───────────────┘
                         ▼
           ServiceStack.OpenApi.Swashbuckle
                         │
                         ▼
Swagger UI & Swagger JSON Endpoints (/swagger/v1/swagger.json)
```

- **Depends on**: `ServiceStack`, `ServiceStack.Interfaces`, `ServiceStack.Common`, `ServiceStack.Client`, `ServiceStack.Extensions`, `Swashbuckle.AspNetCore`, `Microsoft.OpenApi`.
- **Depended on by**: Applications standardizing on Swashbuckle for API documentation across ASP.NET Core and ServiceStack services.
- **Alternative**: `ServiceStack.OpenApi.Microsoft` (uses Microsoft's native `Microsoft.AspNetCore.OpenApi`).

---

## Key Functionality

### Primary Types
| Class | Role |
|---|---|
| `ServiceStackDocumentFilter` | Implements Swashbuckle's `IDocumentFilter`, populating paths, operations, tags, and component schemas from ServiceStack metadata. |
| `OpenApiMetadata` | Core engine extracting OpenAPI schemas, parameters, and responses from ServiceStack DTO reflection. |
| `ServiceStackOpenApiExtensions` | DI registration extensions: `builder.Services.AddServiceStackSwagger()`. |
| `SwaggerUtils` | Utility methods for property filtering and schema naming. |

---

## Architecture & Design Patterns

### Document Filter Pattern
Instead of running a standalone HTTP handler, Swashbuckle calls `ServiceStackDocumentFilter.Apply(swaggerDoc, context)` during document generation, merging ServiceStack's API definitions into Swashbuckle's object model.

### RFC 6901 Generic Token Sanitization
Generic type names with angle brackets (e.g. `QueryResponse<T>`) are sanitized via regex into safe schema keys (`QueryResponse_T_`), ensuring Swagger UI and code generators do not fail on invalid JSON Pointer tokens.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Always observe these constraints:

### 1. Information Disclosure Prevention (`[IgnoreDataMember]`)
- `SwaggerUtils.DefaultIgnoreProperty` must exclude properties decorated with `[IgnoreDataMember]` to prevent sensitive internal properties (passwords, tokens) from being published in the Swagger schema.

### 2. Generic Type Reference Compliance
- Always use `GetSchemaDefinitionRef(type)` when setting schema references in `swaggerDoc.Components.Schemas` to avoid schema resolution failures in Swagger UI.

### 3. Wildcard and Constrained Route Matching
- Route path parsing must recognize `{param}`, `{param*`, and `{param:` to prevent route variables from erroneously appearing as query parameters or in body schemas.

### 4. HTTP 204 No Content Compliance
- Omit the `content` map from HTTP 204 responses to strictly adhere to OpenAPI 3.0 and RFC 9110 specifications.

### 5. Multi-Tenant Instance Isolation
- `InlineSchemaTypesInNamespaces` must be an instance property on `OpenApiMetadata` rather than a static list, preventing configuration bleed between concurrent tests or multi-tenant setups.

---

## Common Modification Scenarios

1. **Enabling Swashbuckle Integration**
   ```csharp
   builder.Services.AddEndpointsApiExplorer();
   builder.Services.AddSwaggerGen(c => {
       c.AddServiceStackSwagger();
   });
   ```

2. **Registering Security Schemes in SwaggerGen**
   ```csharp
   builder.Services.AddSwaggerGen(c => {
       c.AddServiceStackSwagger();
       c.AddJwtAuth();
       c.AddApiKeys();
   });
   ```
