# ServiceStack.Extensions features

ASP.NET Core Identity, gRPC hosting and application integration helpers.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net6.0;net8.0;net10.0`; see [ServiceStack.Extensions.csproj](ServiceStack.Extensions.csproj) for conditional compilation and dependencies.
- Requires ASP.NET Core. gRPC transport and ASP.NET Identity integration are distinct from the older ServiceStack Auth repository providers. Shared contracts must preserve protocol-specific error and cancellation behavior.
- Project dependencies: [ServiceStack.Text](../../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj); [ServiceStack.Interfaces](../ServiceStack.Interfaces/ServiceStack.Interfaces.csproj); [ServiceStack.Client](../ServiceStack.Client/ServiceStack.Client.csproj); [ServiceStack.Common](../ServiceStack.Common/ServiceStack.Common.csproj); [ServiceStack](../ServiceStack/ServiceStack.csproj); [ServiceStack.GrpcClient](../ServiceStack.GrpcClient/ServiceStack.GrpcClient.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| Identity authentication | Bridge host Identity users, credentials, roles, sessions and JWT into ServiceStack. | [Auth/IdentityAuth.cs](Auth/IdentityAuth.cs), [Auth/IdentityAuthProvider.cs](Auth/IdentityAuthProvider.cs), [Auth/IdentityJwtAuthProvider.cs](Auth/IdentityJwtAuthProvider.cs), [Auth/IdentityCredentialsAuthProvider.cs](Auth/IdentityCredentialsAuthProvider.cs) |
| Identity administration | Manage Identity users and register accounts through integrated service APIs. | [Auth/AdminIdentityUsersFeature.cs](Auth/AdminIdentityUsersFeature.cs), [Auth/IdentityRegisterService.cs](Auth/IdentityRegisterService.cs) |
| gRPC service hosting | Expose DTO services and generated protobuf contracts over gRPC. | [GrpcFeature.cs](GrpcFeature.cs), [Grpc/GrpcServiceBase.cs](Grpc/GrpcServiceBase.cs), [Grpc/GrpcProtoGenerator.cs](Grpc/GrpcProtoGenerator.cs), [IStreamService.cs](IStreamService.cs) |
| Auth middleware and providers | Integrate Basic authentication, Apple Sign-In and fetch-metadata CSRF middleware. | [BasicAuthenticationHandler.cs](BasicAuthenticationHandler.cs), [Auth/AppleAuthProvider.cs](Auth/AppleAuthProvider.cs), [SecFetchSiteCsrfMiddleware.cs](SecFetchSiteCsrfMiddleware.cs) |
| Frontend development integration | Register Blazor clients and proxy requests to a Node development server. | [BlazorExtensions.cs](BlazorExtensions.cs), [NodeProxy.cs](NodeProxy.cs) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [auth/identity-auth.md](../../../../docs.servicestack.net/MyApp/_pages/auth/identity-auth.md)
- [auth/identity-aspnet.md](../../../../docs.servicestack.net/MyApp/_pages/auth/identity-aspnet.md)
- [auth/jwt-identity-auth.md](../../../../docs.servicestack.net/MyApp/_pages/auth/jwt-identity-auth.md)
- [auth/signin-with-apple.md](../../../../docs.servicestack.net/MyApp/_pages/auth/signin-with-apple.md)
- [auth/anti-forgery.md](../../../../docs.servicestack.net/MyApp/_pages/auth/anti-forgery.md)
- [grpc/index.md](../../../../docs.servicestack.net/MyApp/_pages/grpc/index.md)
- [grpc/csharp.md](../../../../docs.servicestack.net/MyApp/_pages/grpc/csharp.md)

## Test evidence and review limits

Related test files located (not run in this documentation pass; not proof that every capability or database dialect is covered):

- [ServiceStack.Extensions.Tests/IdentityJwtAuthProviderTests.cs](../../tests/ServiceStack.Extensions.Tests/IdentityJwtAuthProviderTests.cs)
- [ServiceStack.Extensions.Tests/GrpcIdentityAuthTests.cs](../../tests/ServiceStack.Extensions.Tests/GrpcIdentityAuthTests.cs)
- [ServiceStack.Extensions.Tests/GrpcTests.cs](../../tests/ServiceStack.Extensions.Tests/GrpcTests.cs)

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
