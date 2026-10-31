# ServiceStack.Api.OpenApi features

Classic Swagger/OpenAPI 2 document generation and embedded Swagger UI.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net472;net6.0;net8.0;net10.0`; see [ServiceStack.Api.OpenApi.csproj](ServiceStack.Api.OpenApi.csproj) for conditional compilation and dependencies.
- Uses ServiceStack-owned Swagger 2 models and a bundled UI; not the OpenAPI 3 integration.
- Project dependencies: [ServiceStack.Text](../../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj); [ServiceStack.Interfaces](../ServiceStack.Interfaces/ServiceStack.Interfaces.csproj); [ServiceStack.Client](../ServiceStack.Client/ServiceStack.Client.csproj); [ServiceStack.Common](../ServiceStack.Common/ServiceStack.Common.csproj); [ServiceStack](../ServiceStack/ServiceStack.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| Host integration and discovery | Register the integration and include visible ServiceStack operations. | [OpenApiFeature.cs](OpenApiFeature.cs) |
| DTO schemas and operations | Convert DTO shapes, routes and response contracts into API documentation. | [OpenApiService.cs](OpenApiService.cs), [Specification](Specification) |
| Security and customization | Describe security requirements and customize schema/operation output. | [OpenApiFeature.cs](OpenApiFeature.cs) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [openapi-v2.md](../../../../docs.servicestack.net/MyApp/_pages/openapi-v2.md)
- [swagger-api.md](../../../../docs.servicestack.net/MyApp/_pages/swagger-api.md)

## Test evidence and review limits

Related test files located (not run in this documentation pass; not proof that every capability or database dialect is covered):

- [ServiceStack.Extensions.Tests/OpenApiSecurityAndBugTests.cs](../../tests/ServiceStack.Extensions.Tests/OpenApiSecurityAndBugTests.cs)

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
