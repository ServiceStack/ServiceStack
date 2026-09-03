# Security Changes & Remediation Reference (`ServiceStack.AspNetCore.OpenApi`)

This document details security, robustness, and OpenAPI specification compliance fixes across `ServiceStack.AspNetCore.OpenApi`.

---

## 1. Information Disclosure / Sensitive Property Schema Generation (`SwaggerUtils`, `ServiceStackDocumentFilter`)
- **Severity**: Medium
- **Description**:
  - `SwaggerUtils.DefaultIgnoreProperty` checks whether a property should be excluded from Swagger schema generation and referenced type inspection.
  - Previously, `DefaultIgnoreProperty` only checked `ObsoleteAttribute`, `JsonIgnoreAttribute`, and `SwaggerIgnoreAttribute`.
  - In ServiceStack, `[IgnoreDataMember]` is the primary attribute used to decorate internal or sensitive DTO properties to exclude them from serialization. Because `DefaultIgnoreProperty` did not check `IgnoreDataMemberAttribute`, `ServiceStackDocumentFilter.AddReferencedTypes` recursively inspected these properties and registered their types in `Components.Schemas`.
- **Change**:
  - Added `IgnoreDataMemberAttribute` to `SwaggerUtils.DefaultIgnoreProperty`.

---

## 2. Invalid Schema Identifiers and References for Generic Types (`OpenApiMetadata`, `ServiceStackDocumentFilter`)
- **Severity**: Low / Spec Compliance
- **Description**:
  - `GetSchemaDefinitionRef` previously directly returned `GetSchemaTypeName(type)`, which for generic types returned pretty names containing `<`, `>`, `,`, and spaces (e.g. `MyResult<String>`).
  - OpenAPI 3.0 and JSON Pointer (RFC 6901) require schema component keys in `components.schemas` to match `^[a-zA-Z0-9\.\-_]+$`. Generics containing angle brackets and commas broke JSON Pointer references (`#/components/schemas/MyResult<String>`) and caused OpenAPI parser and validator failures.
- **Change**:
  - Sanitized generic schema keys using a compiled timeout-safe regex (`schemaRefRegex`) that replaces non-token characters with underscores (e.g. `MyResult_String_`).
  - Synchronized `ToOpenApiReference` and `swaggerDoc.Components.Schemas` to use `GetSchemaDefinitionRef`.

---

## 3. Path Parameter Recognition for Route Wildcards and Constraints (`OpenApiMetadata`)
- **Severity**: Low / Robustness
- **Description**:
  - `CreateParameters` and `AddOperation` evaluated route path variables using exact string containment `route.Contains("{" + propertyName + "}")`.
  - Routes using wildcard variables (e.g. `{Path*}`) or route constraints (e.g. `{Id:int}`) were not recognized as path parameters. In `CreateParameters`, they were erroneously marked as `ParameterLocation.Query`. In `AddOperation` (for requests with request bodies), they failed to be stripped from the body schema.
- **Change**:
  - Updated path parameter detection in both `CreateParameters` and `AddOperation` to recognize wildcards (`{*`) and constraints (`{:`).

---

## 4. Multi-Tenant Isolation & Instance Support (`OpenApiMetadata`, `ServiceStackOpenApiExtensions`)
- **Severity**: Low / Robustness
- **Description**:
  - `InlineSchemaTypesInNamespaces` was previously declared as a global static list on `OpenApiMetadata`, causing cross-contamination across different AppHosts or test runners.
  - `AddSwagger` and `AddServiceStackSwagger` only registered the global singleton `OpenApiMetadata.Instance`.
- **Change**:
  - Converted `InlineSchemaTypesInNamespaces` to an instance property on `OpenApiMetadata`.
  - Added overloads to `AddSwagger` and `AddServiceStackSwagger` accepting an `OpenApiMetadata` instance, facilitating isolated configuration per DI container.

---

## 5. HTTP Specification Compliance for 204 No Content Responses (`OpenApiMetadata`)
- **Severity**: Low / Spec Compliance
- **Description**:
  - When generating response codes for endpoints returning empty content with 204, `responses.Add("204", ...)` added an OpenAPI `Content` dictionary with an empty schema. Under RFC 9110 and OpenAPI 3.0, 204 No Content responses must not contain a message body.
- **Change**:
  - Omitted the `Content` dictionary when status code is 204.
