# ServiceStack.NetFramework features

Legacy .NET Framework profiling and SmartThreadPool self-host integration.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net472`; see [ServiceStack.NetFramework.csproj](ServiceStack.NetFramework.csproj) for conditional compilation and dependencies.
- Targets net472 only. Modern profiling and Kestrel hosting are owned by other projects.
- Project dependencies: [ServiceStack.Text](../../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj); [ServiceStack.OrmLite](../../../ServiceStack.OrmLite/src/ServiceStack.OrmLite/ServiceStack.OrmLite.csproj); [ServiceStack.Interfaces](../ServiceStack.Interfaces/ServiceStack.Interfaces.csproj); [ServiceStack.Client](../ServiceStack.Client/ServiceStack.Client.csproj); [ServiceStack.Common](../ServiceStack.Common/ServiceStack.Common.csproj); [ServiceStack](../ServiceStack/ServiceStack.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| MiniProfiler integration | Collect and render request and SQL timings with profiling providers/storage. | [MiniProfilerFeature.cs](MiniProfilerFeature.cs), [MiniProfiler](MiniProfiler) |
| Thread-pool self-hosting | Use SmartThreadPool-backed HttpListener hosting. | [AppHostHttpListenerSmartPoolBase.cs](AppHostHttpListenerSmartPoolBase.cs), [SmartThreadPool](SmartThreadPool) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [built-in-profiling.md](../../../../docs.servicestack.net/MyApp/_pages/built-in-profiling.md)
- [self-hosting.md](../../../../docs.servicestack.net/MyApp/_pages/self-hosting.md)

## Test evidence and review limits

Related test files located (not run in this documentation pass; not proof that every capability or database dialect is covered):

- [ServiceStack.Auth.Tests/MiniProfilerTests.cs](../../tests/ServiceStack.Auth.Tests/MiniProfilerTests.cs)

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
