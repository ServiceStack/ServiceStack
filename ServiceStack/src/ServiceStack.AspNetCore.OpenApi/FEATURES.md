# ServiceStack.AspNetCore.OpenApi features

OpenAPI metadata integration for the older ASP.NET Core/Swashbuckle dependency line.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net8.0;net10.0`; see [ServiceStack.AspNetCore.OpenApi.csproj](ServiceStack.AspNetCore.OpenApi.csproj) for conditional compilation and dependencies.
- Pins Microsoft.OpenApi 1.x and Swashbuckle 8.x in this checkout. Do not mix with the newer sibling integration dependency line.
- Project dependencies: [ServiceStack.Client](../ServiceStack.Client/ServiceStack.Client.csproj); [ServiceStack.Common](../ServiceStack.Common/ServiceStack.Common.csproj); [ServiceStack.Extensions](../ServiceStack.Extensions/ServiceStack.Extensions.csproj); [ServiceStack.Interfaces](../ServiceStack.Interfaces/ServiceStack.Interfaces.csproj); [ServiceStack](../ServiceStack/ServiceStack.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| Host integration and discovery | Register the integration and include visible ServiceStack operations. | [ServiceStackOpenApiExtensions.cs](ServiceStackOpenApiExtensions.cs) |
| DTO schemas and operations | Convert DTO shapes, routes and response contracts into API documentation. | [ServiceStackDocumentFilter.cs](ServiceStackDocumentFilter.cs), [OpenApiMetadata.cs](OpenApiMetadata.cs) |
| Security and customization | Describe security requirements and customize schema/operation output. | [SwaggerUtils.cs](SwaggerUtils.cs) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [openapi.md](../../../../docs.servicestack.net/MyApp/_pages/openapi.md)

## Test evidence and review limits

Related test files located (not run in this documentation pass; not proof that every capability or database dialect is covered):

- [ServiceStack.Extensions.Tests/AspNetCoreOpenApiSecurityAndBugTests.cs](../../tests/ServiceStack.Extensions.Tests/AspNetCoreOpenApiSecurityAndBugTests.cs)

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
