# ServiceStack features

DTO-first services, hosting, authentication, metadata, generated UIs and cross-cutting plugins.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net472;net6.0;net8.0;net10.0`; see [ServiceStack.csproj](ServiceStack.csproj) for conditional compilation and dependencies.
- Features are opt-in where supplied as plugins. Hosting and serializer behavior varies by target framework. AutoQuery SQL and distributed Redis providers belong to ServiceStack.Server; SQLite jobs belong to ServiceStack.Jobs.
- Project dependencies: [ServiceStack.Text](../../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj); [ServiceStack.Interfaces](../ServiceStack.Interfaces/ServiceStack.Interfaces.csproj); [ServiceStack.Client](../ServiceStack.Client/ServiceStack.Client.csproj); [ServiceStack.Common](../ServiceStack.Common/ServiceStack.Common.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| Request pipeline and hosting | Run DTO services through shared routing, filters, DI and error handling. | [ServiceStackHost.cs](ServiceStackHost.cs), [AppHostBase.cs](AppHostBase.cs), [Host/ServiceRunner.cs](Host/ServiceRunner.cs) |
| Authentication and sessions | Authenticate with credentials, JWT, API keys and external providers; authorize services with roles and permissions. | [AuthFeature.cs](AuthFeature.cs), [SessionFeature.cs](SessionFeature.cs), [Auth/JwtAuthProvider.cs](Auth/JwtAuthProvider.cs), [Auth/ApiKeyAuthProvider.cs](Auth/ApiKeyAuthProvider.cs) |
| Validation | Apply validators and declarative rules before service execution. | [Validation/ValidationFeature.cs](Validation/ValidationFeature.cs) |
| Formats and HTTP caching | Negotiate response formats and cache HTTP responses or application values. | [HttpCacheFeature.cs](HttpCacheFeature.cs), [CacheResponseAttribute.cs](CacheResponseAttribute.cs), [Caching/MemoryCacheClient.cs](Caching/MemoryCacheClient.cs) |
| Metadata, schemas and DTO generation | Discover API contracts and generate typed DTOs for multiple languages. | [MetadataFeature.cs](MetadataFeature.cs), [MetadataSchema.cs](MetadataSchema.cs), [NativeTypesFeature.cs](NativeTypesFeature.cs), [NativeTypes/NativeTypesService.cs](NativeTypes/NativeTypesService.cs) |
| AutoQuery Data | Query in-memory and service-backed data using declarative DTOs. | [AutoQueryDataFeature.cs](AutoQueryDataFeature.cs), [AutoQueryDataServiceSource.cs](AutoQueryDataServiceSource.cs) |
| Server events and in-memory messaging | Push channel events over SSE and dispatch background DTO messages. | [ServerEventsFeature.cs](ServerEventsFeature.cs), [Messaging/BackgroundMqService.cs](Messaging/BackgroundMqService.cs) |
| Commands and retry policies | Execute typed commands with per-command retry attributes and diagnostics. | [CommandsFeature.cs](CommandsFeature.cs) |
| Operational tools and generated UIs | Explore APIs and administer applications with embedded UI modules and profiling. | [UiFeature.cs](UiFeature.cs), [ProfilingFeature.cs](ProfilingFeature.cs), [modules/admin-ui](modules/admin-ui), [modules/locode](modules/locode) |
| Uploads, scripts and development utilities | Configure upload locations, render #Script pages and reload development assets. | [FilesUploadFeature.cs](FilesUploadFeature.cs), [SharpPagesFeature.cs](SharpPagesFeature.cs), [HotReloadFeature.cs](HotReloadFeature.cs) |
| Shared job infrastructure | Share scheduling helpers, result delivery, shutdown and tracing between job providers. | [Jobs/JobUtils.cs](Jobs/JobUtils.cs), [Jobs/JobReplyTo.cs](Jobs/JobReplyTo.cs), [Jobs/JobsDiagnostics.cs](Jobs/JobsDiagnostics.cs), [Jobs/JobsHealthCheck.cs](Jobs/JobsHealthCheck.cs) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [architecture-overview.md](../../../../docs.servicestack.net/MyApp/_pages/architecture-overview.md)
- [routing.md](../../../../docs.servicestack.net/MyApp/_pages/routing.md)
- [endpoint-routing.md](../../../../docs.servicestack.net/MyApp/_pages/endpoint-routing.md)
- [request-and-response-filters.md](../../../../docs.servicestack.net/MyApp/_pages/request-and-response-filters.md)
- [auth/authentication-and-authorization.md](../../../../docs.servicestack.net/MyApp/_pages/auth/authentication-and-authorization.md)
- [auth/jwt-authprovider.md](../../../../docs.servicestack.net/MyApp/_pages/auth/jwt-authprovider.md)
- [auth/api-key-authprovider.md](../../../../docs.servicestack.net/MyApp/_pages/auth/api-key-authprovider.md)
- [validation.md](../../../../docs.servicestack.net/MyApp/_pages/validation.md)
- [declarative-validation.md](../../../../docs.servicestack.net/MyApp/_pages/declarative-validation.md)
- [formats.md](../../../../docs.servicestack.net/MyApp/_pages/formats.md)
- [http-caching.md](../../../../docs.servicestack.net/MyApp/_pages/http-caching.md)
- [caching.md](../../../../docs.servicestack.net/MyApp/_pages/caching.md)
- [metadata-page.md](../../../../docs.servicestack.net/MyApp/_pages/metadata-page.md)
- [api-schema.md](../../../../docs.servicestack.net/MyApp/_pages/api-schema.md)
- [dto-langs.md](../../../../docs.servicestack.net/MyApp/_pages/dto-langs.md)
- [autoquery/data.md](../../../../docs.servicestack.net/MyApp/_pages/autoquery/data.md)
- [server-events.md](../../../../docs.servicestack.net/MyApp/_pages/server-events.md)
- [background-mq.md](../../../../docs.servicestack.net/MyApp/_pages/background-mq.md)
- [commands.md](../../../../docs.servicestack.net/MyApp/_pages/commands.md)
- [admin-ui.md](../../../../docs.servicestack.net/MyApp/_pages/admin-ui.md)
- [built-in-profiling.md](../../../../docs.servicestack.net/MyApp/_pages/built-in-profiling.md)
- [virtual-file-system.md](../../../../docs.servicestack.net/MyApp/_pages/virtual-file-system.md)

## Test evidence and review limits

Related test files located (not run in this documentation pass; not proof that every capability or database dialect is covered):

- [ServiceStack.Extensions.Tests/BackgroundJobsTests.cs](../../tests/ServiceStack.Extensions.Tests/BackgroundJobsTests.cs)
- [ServiceStack.WebHost.Endpoints.Tests/AutoQueryDataTests.cs](../../tests/ServiceStack.WebHost.Endpoints.Tests/AutoQueryDataTests.cs)
- [ServiceStack.Extensions.Tests/ClientSecurityAndBugTests.cs](../../tests/ServiceStack.Extensions.Tests/ClientSecurityAndBugTests.cs)

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
