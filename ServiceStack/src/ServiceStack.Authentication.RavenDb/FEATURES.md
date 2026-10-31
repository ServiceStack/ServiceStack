# ServiceStack.Authentication.RavenDb features

RavenDB-backed ServiceStack authentication repositories and API keys.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net472;net6.0;net8.0;net10.0`; see [ServiceStack.Authentication.RavenDb.csproj](ServiceStack.Authentication.RavenDb.csproj) for conditional compilation and dependencies.
- Raven document IDs require translation at the ServiceStack boundary. Generic user models and sync/async sessions are provider-specific; live RavenDB validation is outside this pass.
- Project dependencies: [ServiceStack.Text](../../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj); [ServiceStack.Interfaces](../ServiceStack.Interfaces/ServiceStack.Interfaces.csproj); [ServiceStack.Common](../ServiceStack.Common/ServiceStack.Common.csproj); [ServiceStack.Client](../ServiceStack.Client/ServiceStack.Client.csproj); [ServiceStack](../ServiceStack/ServiceStack.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| Document-backed authentication | Persist custom user models, external identities and API keys in RavenDB. | [RavenDbUserAuthRepository.cs](RavenDbUserAuthRepository.cs), [RavenDbUserAuthRepositorySync.cs](RavenDbUserAuthRepositorySync.cs), [RavenDbUserAuthRepositoryAsync.cs](RavenDbUserAuthRepositoryAsync.cs) |
| ID and model integration | Map Raven user models and translate document IDs safely. | [RavenIdConverter.cs](RavenIdConverter.cs), [RavenUserAuth.cs](RavenUserAuth.cs), [RavenUserAuthDetails.cs](RavenUserAuthDetails.cs) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [auth/auth-repository.md](../../../../docs.servicestack.net/MyApp/_pages/auth/auth-repository.md)
- [auth/authentication-and-authorization.md](../../../../docs.servicestack.net/MyApp/_pages/auth/authentication-and-authorization.md)

## Test evidence and review limits

Related test files located (not run in this documentation pass; not proof that every capability or database dialect is covered):

- [ServiceStack.WebHost.Endpoints.Tests/RavenDbAuthRepositoryUnitTests.cs](../../tests/ServiceStack.WebHost.Endpoints.Tests/RavenDbAuthRepositoryUnitTests.cs)

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
