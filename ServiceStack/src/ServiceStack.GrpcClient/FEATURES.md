# ServiceStack.GrpcClient features

Typed .NET gRPC transport for ServiceStack services.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net6.0;net8.0;net10.0`; see [ServiceStack.GrpcClient.csproj](ServiceStack.GrpcClient.csproj) for conditional compilation and dependencies.
- Pairs with GrpcFeature in ServiceStack.Extensions. Channel/certificate configuration and streaming semantics differ from HTTP clients.
- Project dependencies: [ServiceStack.Text](../../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj); [ServiceStack.Interfaces](../ServiceStack.Interfaces/ServiceStack.Interfaces.csproj); [ServiceStack.Client](../ServiceStack.Client/ServiceStack.Client.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| DTO request dispatch | Call ServiceStack unary and streaming services through a typed client. | [GrpcServiceClient.cs](GrpcServiceClient.cs), [GrpcServiceStack.cs](GrpcServiceStack.cs) |
| Authentication and metadata | Configure sessions, bearer/refresh tokens and per-call metadata filters. | [GrpcClientConfig.cs](GrpcClientConfig.cs), [GrpcUtils.cs](GrpcUtils.cs) |
| Code-first contract mapping | Configure protobuf DTO registration and service naming. | [GrpcConfig.cs](GrpcConfig.cs) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [grpc/csharp.md](../../../../docs.servicestack.net/MyApp/_pages/grpc/csharp.md)
- [grpc/generic.md](../../../../docs.servicestack.net/MyApp/_pages/grpc/generic.md)
- [grpc/ssl.md](../../../../docs.servicestack.net/MyApp/_pages/grpc/ssl.md)
- [server-events-grpc.md](../../../../docs.servicestack.net/MyApp/_pages/server-events-grpc.md)

## Test evidence and review limits

Related test files located (not run in this documentation pass; not proof that every capability or database dialect is covered):

- [ServiceStack.Extensions.Tests/GrpcTests.cs](../../tests/ServiceStack.Extensions.Tests/GrpcTests.cs)
- [ServiceStack.Extensions.Tests/GrpcAuthTests.cs](../../tests/ServiceStack.Extensions.Tests/GrpcAuthTests.cs)
- [ServiceStack.Extensions.Tests/GrpcServerEventsTests.cs](../../tests/ServiceStack.Extensions.Tests/GrpcServerEventsTests.cs)

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
