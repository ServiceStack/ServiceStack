# ServiceStack.Kestrel features

Direct Kestrel self-hosting and compatibility host base classes.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net6.0;net8.0;net10.0`; see [ServiceStack.Kestrel.csproj](ServiceStack.Kestrel.csproj) for conditional compilation and dependencies.
- A hosting alternative to integrating ServiceStack into a separately configured ASP.NET Core application. Binding and path-base semantics must be checked against the deployment host.
- Project dependencies: [ServiceStack.Text](../../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj); [ServiceStack.Interfaces](../ServiceStack.Interfaces/ServiceStack.Interfaces.csproj); [ServiceStack.Common](../ServiceStack.Common/ServiceStack.Common.csproj); [ServiceStack.Client](../ServiceStack.Client/ServiceStack.Client.csproj); [ServiceStack](../ServiceStack/ServiceStack.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| Self-hosted application lifecycle | Configure, start, bind and stop a Kestrel-backed ServiceStack host. | [AppSelfHostBase.cs](AppSelfHostBase.cs) |
| Endpoint and middleware integration | Map service endpoints and configure ASP.NET Core request dispatch. | [AppSelfHostBase.cs](AppSelfHostBase.cs) |
| Compatibility host classes | Adapt HttpListener-style host base names to the modern host. | [AppHostHttpListenerBase.NetCore.cs](AppHostHttpListenerBase.NetCore.cs) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [self-hosting.md](../../../../docs.servicestack.net/MyApp/_pages/self-hosting.md)
- [netcore.md](../../../../docs.servicestack.net/MyApp/_pages/netcore.md)
- [endpoint-routing.md](../../../../docs.servicestack.net/MyApp/_pages/endpoint-routing.md)

## Test evidence and review limits

No focused test file has been mapped in this first pass. This does not establish that tests are absent; search integration tests and consuming applications before treating it as a coverage gap.

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
