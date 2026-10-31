# ServiceStack.HttpClient features

HttpClient-based typed transport and a cache decorator for ServiceStack JSON services.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net472;netstandard2.0;net6.0;net8.0;net10.0`; see [ServiceStack.HttpClient.csproj](ServiceStack.HttpClient.csproj) for conditional compilation and dependencies.
- This package owns JsonHttpClient, not JsonApiClient (which lives in ServiceStack.Client). Handler, cookie, token and disposal configuration must be considered when sharing client instances.
- Project dependencies: [ServiceStack.Text](../../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj); [ServiceStack.Interfaces](../ServiceStack.Interfaces/ServiceStack.Interfaces.csproj); [ServiceStack.Client](../ServiceStack.Client/ServiceStack.Client.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| Typed HttpClient transport | Send DTO requests using System.Net.Http with custom clients or handlers. | [JsonHttpClient.cs](JsonHttpClient.cs) |
| Authentication and uploads | Use bearer/refresh tokens, cookies and multipart DTO/file requests. | [JsonHttpClient.cs](JsonHttpClient.cs), [JsonHttpClientUtils.cs](JsonHttpClientUtils.cs) |
| HTTP cache integration | Cache responses, handle validators and explicitly evict cached entries. | [CachedHttpClient.cs](CachedHttpClient.cs) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [csharp-client.md](../../../../docs.servicestack.net/MyApp/_pages/csharp-client.md)
- [cache-aware-clients.md](../../../../docs.servicestack.net/MyApp/_pages/cache-aware-clients.md)
- [auth/client-auth.md](../../../../docs.servicestack.net/MyApp/_pages/auth/client-auth.md)

## Test evidence and review limits

Related test files located (not run in this documentation pass; not proof that every capability or database dialect is covered):

- [ServiceStack.Extensions.Tests/ClientSecurityAndBugTests.cs](../../tests/ServiceStack.Extensions.Tests/ClientSecurityAndBugTests.cs)

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
