# Security and Spec Compliance Remediation: `ServiceStack.OpenApi.Microsoft`

## Summary
Audit and hardening of `ServiceStack.OpenApi.Microsoft` (.NET 10 with `Microsoft.AspNetCore.OpenApi` and `Microsoft.OpenApi` 2.x), addressing sensitive DTO property information disclosure, OpenAPI 3.0 / RFC 6901 schema reference sanitization for generic types, route parameter recognition for wildcards and constraints, RFC 9110 HTTP 204 spec compliance, instance isolation for schemas, eliminating all compiler warnings, and removing leftover debug console logging.

---

## Remediations

### 1. Information Disclosure / Sensitive Property Exclusion (`OpenApiUtils`)
- **Issue**: `OpenApiUtils.DefaultIgnoreProperty` inspected attributes to exclude properties from schema generation and recursive type exploration, but only checked `ObsoleteAttribute` and `JsonIgnoreAttribute`. In ServiceStack, `[IgnoreDataMember]` is the standard attribute used to mark sensitive or internal properties (passwords, internal IDs, secret tokens) to prevent serialization.
- **Fix**: Added `IgnoreDataMemberAttribute` to `OpenApiUtils.DefaultIgnoreProperty`.

### 2. Generic Type Schema Reference Compliance (OpenAPI 3.0 / RFC 6901) (`OpenApiMetadata`, `ServiceStackDocumentTransformer`)
- **Issue**: `GetSchemaDefinitionRef` directly used `GetSchemaTypeName(type)`, which for generic types returned pretty names containing `<`, `>`, `,`, and spaces (e.g., `QueryResponse<ItemDto>`). OpenAPI 3.0 and JSON Pointer specification (RFC 6901) require component schema keys to match `^[a-zA-Z0-9\.\-_]+$`. Generics containing angle brackets and commas broke schema resolution in modern OpenAPI tooling and clients (Scalar, Swagger UI, code generators).
- **Fix**: Implemented compiled timeout-safe regex `schemaRefRegex` (`[^A-Za-z0-9\.\-_]`) in `GetSchemaDefinitionRef` to sanitize generic names to valid token keys (e.g. `QueryResponse_ItemDto_`). Synchronized `document.Components.Schemas[...]` keying in `ServiceStackDocumentTransformer` to use `GetSchemaDefinitionRef(type)`.

### 3. Route Parameter Wildcard and Constraint Support (`OpenApiMetadata`)
- **Issue**: Route parameter detection in `CreateParameters` and `AddOperation` evaluated route path variables using exact string matching: `route.Contains("{" + propertyName + "}")`. Wildcard route tokens (`{Path*}`) or constrained parameters (`{Id:int}`) were not recognized as path variables, causing them to be incorrectly placed in query parameters or left inside request body form schemas. In addition, missing route properties threw `ArgumentException`.
- **Fix**: Updated route parameter matching to evaluate `{propertyName}`, `{propertyName*`, and `{propertyName:`. Replaced throwing property lookup with safe resolution fallback (`TypeProperties.Get(...).GetPublicProperty(...)?.Name ?? entry.Key`).

### 4. HTTP 204 No Content Spec Compliance (`OpenApiMetadata`)
- **Issue**: For operations returning empty responses with HTTP 204 No Content, `GetMethodResponseCodes` added an OpenAPI `Content` dictionary specifying `application/json` with a null schema. RFC 9110 and OpenAPI 3.0 dictate that HTTP 204 responses must not specify a response body or `content` map.
- **Fix**: Omitted the `Content` dictionary when the response status code is `204`.

### 5. Multi-Tenant and DI Instance Isolation (`OpenApiMetadata`, `ServiceStackOpenApiExtensions`)
- **Issue**: `InlineSchemaTypesInNamespaces` was declared as a global static list, causing configuration cross-contamination across concurrent test hosts or multi-tenant applications.
- **Fix**: Converted `InlineSchemaTypesInNamespaces` to an instance property on `OpenApiMetadata`. Added overloads to `AddOpenApi` and `AddServiceStackOpenApi` accepting an explicit `OpenApiMetadata` instance for isolated DI container setups.

### 6. Null Safety and Compiler Warning Resolution
- **`OpenApiMetadata.cs`**:
  - Guarded `openApiType.Properties != null` and `formSchema.Properties != null` before manipulating body parameters.
  - Guarded `SecurityDefinition?.Scheme != null` and `ApiKeySecurityDefinition?.Scheme != null`.
  - Guarded `tag != null` before adding `OpenApiTagReference`.
  - Coalesced `CreateSchema(propType) ?? new OpenApiSchema()` and `CreateDictionarySchema(propertyType) ?? new OpenApiSchema()`.
  - Guarded `prop.DeclaringType != null ? typeOrder.IndexOf(prop.DeclaringType) : -1` and `propDataMemberAttrs[prop]?.Order`.
  - Initialized `schema.Properties ??= new OrderedDictionary<string, IOpenApiSchema>();`.
- **`ServiceStackDocumentTransformer.cs`**:
  - Guarded `metadata.SecurityDefinition?.Scheme != null` and `metadata.ApiKeySecurityDefinition?.Scheme != null`.
  - Initialized `openApiOp.Responses ??= new OpenApiResponses();`.
  - Guarded `tag != null` before adding `OpenApiTagReference`.
- **`ConfigureServiceStackOpenApi.cs`**:
  - Removed leftover debug `Console.WriteLine`.
- **`ServiceStackOpenApiExtensions.cs`**:
  - Guarded reflection method lookup `if (method != null)` before invoking `MakeGenericMethod`.
