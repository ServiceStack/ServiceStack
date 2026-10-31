# ServiceStack.Caching.Memcached features

Enyim-backed Memcached adapter for ServiceStack caching contracts.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net472`; see [ServiceStack.Caching.Memcached.csproj](ServiceStack.Caching.Memcached.csproj) for conditional compilation and dependencies.
- Targets net472 only in this checkout; it is not a modern .NET caching provider. CAS operations and payload wrappers are provider-specific.
- Project dependencies: [ServiceStack.Text](../../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj); [ServiceStack.Interfaces](../ServiceStack.Interfaces/ServiceStack.Interfaces.csproj); [ServiceStack.Common](../ServiceStack.Common/ServiceStack.Common.csproj); [ServiceStack.Client](../ServiceStack.Client/ServiceStack.Client.csproj); [ServiceStack](../ServiceStack/ServiceStack.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| Distributed cache adapter | Read/write cached objects, expirations, counters and bulk values through ICacheClient. | [MemcachedClientCache.cs](MemcachedClientCache.cs) |
| CAS and serialization envelope | Use compare-and-swap operations and wrapped cached values. | [MemcachedClientCache.cs](MemcachedClientCache.cs), [MemcachedValueWrapper.cs](MemcachedValueWrapper.cs) |
| Logging integration | Route Enyim diagnostics into ServiceStack logging. | [EnyimLogFactoryWrapper.cs](EnyimLogFactoryWrapper.cs), [EnyimLoggerWarpper.cs](EnyimLoggerWarpper.cs) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [caching.md](../../../../docs.servicestack.net/MyApp/_pages/caching.md)

## Test evidence and review limits

Related test files located (not run in this documentation pass; not proof that every capability or database dialect is covered):

- [ServiceStack.WebHost.Endpoints.Tests/MemcachedClientCacheUnitTests.cs](../../tests/ServiceStack.WebHost.Endpoints.Tests/MemcachedClientCacheUnitTests.cs)

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
