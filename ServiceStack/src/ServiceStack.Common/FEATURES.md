# ServiceStack.Common features

Shared VFS, #Script, configuration, process and application utility infrastructure.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net472;net6.0;net8.0;net10.0`; see [ServiceStack.Common.csproj](ServiceStack.Common.csproj) for conditional compilation and dependencies.
- Concrete database dialects, HTTP hosting and ServiceStack.Text serialization are owned elsewhere. Filesystem access and protected script methods retain their existing access boundaries.
- Project dependencies: [ServiceStack.Text](../../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj); [ServiceStack.Interfaces](../ServiceStack.Interfaces/ServiceStack.Interfaces.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| Virtual files | Use physical, embedded-resource, memory and Gist content through a shared API. | [IO/FileSystemVirtualFiles.cs](IO/FileSystemVirtualFiles.cs), [IO/MemoryVirtualFiles.cs](IO/MemoryVirtualFiles.cs), [IO/ResourceVirtualFiles.cs](IO/ResourceVirtualFiles.cs), [IO/GistVirtualFiles.cs](IO/GistVirtualFiles.cs), [IO/MultiVirtualFiles.cs](IO/MultiVirtualFiles.cs) |
| #Script runtime | Evaluate templates and registered script methods using a configurable context. | [Script/ScriptContext.cs](Script/ScriptContext.cs), [Script/ScriptMethods.cs](Script/ScriptMethods.cs) |
| Process and command utilities | Execute processes and concurrent commands with cancellation and result capture. | [ProcessUtils.cs](ProcessUtils.cs), [ExecUtils.cs](ExecUtils.cs), [CommandsUtils.cs](CommandsUtils.cs) |
| Configuration and startup | Compose a lightweight container, settings and application startup tasks. | [SimpleContainer.cs](SimpleContainer.cs), [SimpleAppSettings.cs](SimpleAppSettings.cs), [StartupTasks.cs](StartupTasks.cs), [AppTasks.cs](AppTasks.cs) |
| Inspection and presentation utilities | Inspect values and build structured text/HTML output. | [Inspect.cs](Inspect.cs), [StringUtils.cs](StringUtils.cs), [ViewUtils.cs](ViewUtils.cs), [SvgCreator.cs](SvgCreator.cs) |
| Cron expression calculation | Calculate schedule occurrences with time-zone-aware cron utilities. | [Cronos/CronExpression.cs](Cronos/CronExpression.cs), [Cronos/TimeZoneHelper.cs](Cronos/TimeZoneHelper.cs) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [virtual-file-system.md](../../../../docs.servicestack.net/MyApp/_pages/virtual-file-system.md)
- [config-api.md](../../../../docs.servicestack.net/MyApp/_pages/config-api.md)
- [simple-ioc.md](../../../../docs.servicestack.net/MyApp/_pages/simple-ioc.md)
- [app-tasks.md](../../../../docs.servicestack.net/MyApp/_pages/app-tasks.md)
- [startup-tasks.md](../../../../docs.servicestack.net/MyApp/_pages/startup-tasks.md)
- [dump-utils.md](../../../../docs.servicestack.net/MyApp/_pages/dump-utils.md)
- [reflection-utils.md](../../../../docs.servicestack.net/MyApp/_pages/reflection-utils.md)
- [jobs/recurring-tasks.md](../../../../docs.servicestack.net/MyApp/_pages/jobs/recurring-tasks.md)

## Test evidence and review limits

Related test files located (not run in this documentation pass; not proof that every capability or database dialect is covered):

- [ServiceStack.Extensions.Tests/CommonSecurityAndBugTests.cs](../../tests/ServiceStack.Extensions.Tests/CommonSecurityAndBugTests.cs)

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
