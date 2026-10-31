# ServiceStack.Client features

Typed .NET clients, client DTOs, URL construction, caching, server events and diagnostics.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net472;netstandard2.0;net6.0;net8.0;net10.0`; see [ServiceStack.Client.csproj](ServiceStack.Client.csproj) for conditional compilation and dependencies.
- JsonApiClient is implemented here, despite its name often being associated with the HttpClient package. Protocol availability differs across conditional target-framework paths.
- Project dependencies: [ServiceStack.Text](../../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj); [ServiceStack.Interfaces](../ServiceStack.Interfaces/ServiceStack.Interfaces.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| Typed service clients | Call DTO services using JSON, XML, JSV, CSV and SOAP client implementations. | [ServiceClientBase.cs](ServiceClientBase.cs), [JsonServiceClient.cs](JsonServiceClient.cs), [XmlServiceClient.cs](XmlServiceClient.cs), [JsvServiceClient.cs](JsvServiceClient.cs), [CsvServiceClient.cs](CsvServiceClient.cs), [Soap11ServiceClient.cs](Soap11ServiceClient.cs) |
| Modern JSON API client | Call APIs through JsonApiClient and use its cache decorator. | [JsonApiClient.cs](JsonApiClient.cs), [JsonApiClientUtils.cs](JsonApiClientUtils.cs), [CachedApiClient.cs](CachedApiClient.cs) |
| Routing, authentication and errors | Construct DTO routes and consistently handle auth headers and structured service failures. | [UrlExtensions.cs](UrlExtensions.cs), [WebRequestUtils.cs](WebRequestUtils.cs), [WebServiceException.cs](WebServiceException.cs), [ResponseStatusUtils.cs](ResponseStatusUtils.cs) |
| Client caching | Reuse cache-aware responses and maintain HTTP cache entries. | [CachedServiceClient.cs](CachedServiceClient.cs), [HttpCacheEntry.cs](HttpCacheEntry.cs) |
| SSE client | Subscribe to server events and dispatch notifications to typed receivers. | [ServerEventsClient.cs](ServerEventsClient.cs), [ServerEventsClient.Receiver.cs](ServerEventsClient.Receiver.cs) |
| Shared metadata and operational DTOs | Consume generated UI metadata, auth APIs and jobs APIs from clients. | [MetadataTypes.cs](MetadataTypes.cs), [AuthDtos.cs](AuthDtos.cs), [JobDtos.cs](JobDtos.cs) |
| Client diagnostics and compression | Instrument outbound calls and use supported compression helpers. | [ClientDiagnosticUtils.cs](ClientDiagnosticUtils.cs), [StreamCompressors.cs](StreamCompressors.cs) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [csharp-client.md](../../../../docs.servicestack.net/MyApp/_pages/csharp-client.md)
- [clients-overview.md](../../../../docs.servicestack.net/MyApp/_pages/clients-overview.md)
- [cache-aware-clients.md](../../../../docs.servicestack.net/MyApp/_pages/cache-aware-clients.md)
- [csharp-server-events-client.md](../../../../docs.servicestack.net/MyApp/_pages/csharp-server-events-client.md)
- [auth/client-auth.md](../../../../docs.servicestack.net/MyApp/_pages/auth/client-auth.md)
- [compression.md](../../../../docs.servicestack.net/MyApp/_pages/compression.md)

## Test evidence and review limits

Related test files located (not run in this documentation pass; not proof that every capability or database dialect is covered):

- [ServiceStack.Extensions.Tests/ClientSecurityAndBugTests.cs](../../tests/ServiceStack.Extensions.Tests/ClientSecurityAndBugTests.cs)

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
