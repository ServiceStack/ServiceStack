# ServiceStack.Authentication.MongoDb features

MongoDB-backed ServiceStack user, OAuth-link and API-key repositories.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net472;net6.0;net8.0;net10.0`; see [ServiceStack.Authentication.MongoDb.csproj](ServiceStack.Authentication.MongoDb.csproj) for conditional compilation and dependencies.
- Repository support does not imply ASP.NET Core Identity integration. Sync and async implementations must maintain equivalent contracts; live MongoDB validation is outside this pass.
- Project dependencies: [ServiceStack.Text](../../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj); [ServiceStack.Interfaces](../ServiceStack.Interfaces/ServiceStack.Interfaces.csproj); [ServiceStack.Common](../ServiceStack.Common/ServiceStack.Common.csproj); [ServiceStack](../ServiceStack/ServiceStack.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| User and provider persistence | Persist users, credential changes and linked external identities. | [MongoDbAuthRepository.cs](MongoDbAuthRepository.cs), [MongoDbAuthRepositoryAsync.cs](MongoDbAuthRepositoryAsync.cs) |
| API keys and async repository operations | Manage auth API keys and use asynchronous MongoDB operations. | [MongoDbAuthRepositoryAsync.cs](MongoDbAuthRepositoryAsync.cs) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [auth/auth-repository.md](../../../../docs.servicestack.net/MyApp/_pages/auth/auth-repository.md)
- [auth/authentication-and-authorization.md](../../../../docs.servicestack.net/MyApp/_pages/auth/authentication-and-authorization.md)
- [auth/api-key-authprovider.md](../../../../docs.servicestack.net/MyApp/_pages/auth/api-key-authprovider.md)

## Test evidence and review limits

Related test files located (not run in this documentation pass; not proof that every capability or database dialect is covered):

- [ServiceStack.WebHost.Endpoints.Tests/MongoDbAuthRepositoryUnitTests.cs](../../tests/ServiceStack.WebHost.Endpoints.Tests/MongoDbAuthRepositoryUnitTests.cs)

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
