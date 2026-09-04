# Security and Spec Compliance Remediation: `ServiceStack.OpenApi.Swashbuckle`

## Summary
Audit and hardening of `ServiceStack.OpenApi.Swashbuckle` (.NET 10 with `Swashbuckle.AspNetCore 10.2.3` and `Microsoft.OpenApi 2.11.0`), addressing sensitive DTO property information disclosure, OpenAPI 3.0 / RFC 6901 schema reference sanitization for generic types, route parameter recognition for wildcards and constraints, RFC 9110 HTTP 204 spec compliance, instance isolation for schemas and DI, elimination of all compiler warnings, and security guards on SwaggerGen extensions.

---

## Remediations

### 1. Information Disclosure / Sensitive Property Exclusion (`SwaggerUtils`)
- **Issue**: `SwaggerUtils.DefaultIgnoreProperty` inspected attributes to exclude properties from schema generation and recursive type exploration, but only checked `ObsoleteAttribute`, `JsonIgnoreAttribute`, and `SwaggerIgnoreAttribute`. In ServiceStack, `[IgnoreDataMember]` is the standard attribute used to mark sensitive or internal properties (passwords, tokens, internal identifiers) to prevent serialization.
- **Fix**: Added `IgnoreDataMemberAttribute` to `SwaggerUtils.DefaultIgnoreProperty`.

### 2. Generic Type Schema Reference Compliance (OpenAPI 3.0 / RFC 6901) (`OpenApiMetadata`, `ServiceStackDocumentFilter`)
- **Issue**: `GetSchemaDefinitionRef` directly used `GetSchemaTypeName(type)`, which for generic types returned pretty names containing `<`, `>`, `,`, and spaces (e.g., `QueryResponse<ItemDto>`). OpenAPI 3.0 and JSON Pointer specification (RFC 6901) require component schema keys to match `^[a-zA-Z0-9\.\-_]+$`. Generics containing angle brackets and commas broke schema resolution in Swagger UI and downstream code generators.
- **Fix**: Implemented compiled timeout-safe regex `schemaRefRegex` (`[^A-Za-z0-9\.\-_]`) in `GetSchemaDefinitionRef` to sanitize generic names to valid token keys (e.g. `QueryResponse_ItemDto_`). Synchronized `swaggerDoc.Components.Schemas[...]` keying in `ServiceStackDocumentFilter` to use `GetSchemaDefinitionRef(type)`.

### 3. Route Parameter Wildcard and Constraint Support (`OpenApiMetadata`)
- **Issue**: Route parameter detection in `CreateParameters` and `AddOperation` evaluated route path variables using exact string matching: `route.Contains("{" + propertyName + "}")`. Wildcard route tokens (`{Path*}`) or constrained parameters (`{Id:int}`) were not recognized as path variables, causing them to be incorrectly placed in query parameters or left inside request body form schemas. In addition, missing route properties threw `ArgumentException`.
- **Fix**: Updated route parameter matching to evaluate `{propertyName}`, `{propertyName*`, and `{propertyName:`. Replaced throwing property lookup with safe resolution fallback (`TypeProperties.Get(...).GetPublicProperty(...)?.Name ?? entry.Key`).

### 4. HTTP 204 No Content Spec Compliance (`OpenApiMetadata`)
- **Issue**: For operations returning empty responses with HTTP 204 No Content, `GetMethodResponseCodes` added an OpenAPI `Content` dictionary specifying `application/json` with a null schema. RFC 9110 and OpenAPI 3.0 dictate that HTTP 204 responses must not specify a response body or `content` map.
- **Fix**: Omitted the `Content` dictionary when the response status code is `204`.

### 5. Multi-Tenant and DI Instance Isolation (`OpenApiMetadata`, `ServiceStackOpenApiExtensions`)
- **Issue**: `InlineSchemaTypesInNamespaces` was declared as a global static list, causing configuration cross-contamination across concurrent test hosts or multi-tenant applications.
- **Fix**: Converted `InlineSchemaTypesInNamespaces` to an instance property on `OpenApiMetadata`. Added an overload for `AddServiceStackSwagger` accepting an explicit `OpenApiMetadata` instance for isolated DI container setups.

### 6. SwaggerGen Extension Security Scheme Guards (`ServiceStackOpenApiExtensions`)
- **Issue**: `SwaggerGenOptions` extension methods (`AddBasicAuth`, `AddJwtAuth`, `AddApiKeys`) passed scheme strings directly to `AddSecurityDefinition` without null checks on `Scheme`.
- **Fix**: Added guards ensuring scheme strings are non-null before registering security definitions.

### 7. Null Safety and Compiler Warning Resolution
- **`OpenApiMetadata.cs`**:
  - Guarded `openApiType.Properties != null` and `formSchema.Properties != null` before manipulating body parameters.
  - Guarded `SecurityDefinition?.Scheme != null` and `ApiKeySecurityDefinition?.Scheme != null`.
  - Guarded `tag != null` before adding `OpenApiTagReference`.
  - Coalesced `CreateSchema(propType) ?? new OpenApiSchema()` and `CreateDictionarySchema(propertyType) ?? new OpenApiSchema()`.
  - Guarded `prop.DeclaringType != null ? typeOrder.IndexOf(prop.DeclaringType) : -1` and `propDataMemberAttrs[prop]?.Order`.
  - Initialized `schema.Properties ??= new OrderedDictionary<string, IOpenApiSchema>();`.
- **`ServiceStackDocumentFilter.cs`**:
  - Guarded `metadata.SecurityDefinition?.Scheme != null` and `metadata.ApiKeySecurityDefinition?.Scheme != null`.
  - Initialized `swaggerDoc.Components ??= new OpenApiComponents();` and `swaggerDoc.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();`.
  - Initialized `openApiOp.Responses ??= new OpenApiResponses();`.
  - Guarded `tag != null` before adding `OpenApiTagReference`.
  - Removed commented-out debug log line `//Console.WriteLine(...)`.
