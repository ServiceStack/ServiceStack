# ServiceStack.Interfaces features

Portable DTO contracts, attributes and provider interfaces shared by servers and clients.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net472;netstandard2.0;net6.0;net8.0;net10.0`; see [ServiceStack.Interfaces.csproj](ServiceStack.Interfaces.csproj) for conditional compilation and dependencies.
- This project supplies contracts rather than the concrete implementations. Contract additions have downstream consequences for generators, clients and providers; avoid introducing server dependencies.
- Project dependencies: No ProjectReference entries.

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| Typed requests and responses | Describe response types, service gateways and standard errors without a server dependency. | [IService.cs](IService.cs), [IServiceGateway.cs](IServiceGateway.cs), [ResponseStatus.cs](ResponseStatus.cs), [ResponseError.cs](ResponseError.cs) |
| Routing and API metadata | Declare routes, parameter descriptions and UI/metadata behavior on DTOs. | [RouteAttribute.cs](RouteAttribute.cs), [ApiMemberAttribute.cs](ApiMemberAttribute.cs), [ToolAttribute.cs](ToolAttribute.cs), [McpAttribute.cs](McpAttribute.cs) |
| Query and CRUD contracts | Declare database/data query and CRUD request shapes. | [IQuery.cs](IQuery.cs) |
| Provider abstractions | Share caching, authentication, VFS and messaging contracts. | [Caching](Caching), [Auth](Auth), [IO](IO), [Messaging](Messaging) |
| Jobs and commands | Describe durable work, schedules, dependencies and typed command execution. | [Jobs/IBackgroundJobs.cs](Jobs/IBackgroundJobs.cs), [Jobs/BackgroundJob.cs](Jobs/BackgroundJob.cs), [Jobs/ScheduledTasks.cs](Jobs/ScheduledTasks.cs), [Commands](Commands) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [api-design.md](../../../../docs.servicestack.net/MyApp/_pages/api-design.md)
- [design-message-based-apis.md](../../../../docs.servicestack.net/MyApp/_pages/design-message-based-apis.md)
- [routing.md](../../../../docs.servicestack.net/MyApp/_pages/routing.md)
- [error-handling.md](../../../../docs.servicestack.net/MyApp/_pages/error-handling.md)
- [autoquery/crud.md](../../../../docs.servicestack.net/MyApp/_pages/autoquery/crud.md)
- [jobs/usage.md](../../../../docs.servicestack.net/MyApp/_pages/jobs/usage.md)

## Test evidence and review limits

No focused test file has been mapped in this first pass. This does not establish that tests are absent; search integration tests and consuming applications before treating it as a coverage gap.

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
