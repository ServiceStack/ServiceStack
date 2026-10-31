# ServiceStack.Mvc features

MVC/Razor integration, service-aware controllers and Razor static-site generation.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net472;net6.0;net8.0;net10.0`; see [ServiceStack.Mvc.csproj](ServiceStack.Mvc.csproj) for conditional compilation and dependencies.
- Source includes target-framework conditional MVC and ASP.NET Core implementations; do not assume every API exists on every target. Standalone legacy Razor compilation is in ServiceStack.Razor.
- Project dependencies: [ServiceStack.Text](../../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj); [ServiceStack.Interfaces](../ServiceStack.Interfaces/ServiceStack.Interfaces.csproj); [ServiceStack.Client](../ServiceStack.Client/ServiceStack.Client.csproj); [ServiceStack.Common](../ServiceStack.Common/ServiceStack.Common.csproj); [ServiceStack](../ServiceStack/ServiceStack.csproj); [ServiceStack.NetFramework](../ServiceStack.NetFramework/ServiceStack.NetFramework.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| Service-aware controllers | Access ServiceStack sessions, dependencies and services from MVC controllers. | [ServiceStackController.cs](ServiceStackController.cs), [MvcPageResult.cs](MvcPageResult.cs) |
| Razor rendering | Render Razor pages/views with ServiceStack request context. | [RazorFormat.cs](RazorFormat.cs), [RazorPage.cs](RazorPage.cs), [RazorPagesEngine.cs](RazorPagesEngine.cs) |
| Static-site generation | Render annotated Razor content into static output. | [RazorSsg.cs](RazorSsg.cs) |
| Legacy MVC validation and profiling | Bridge MVC validation, filters and profiling on applicable compilation paths. | [ExecuteServiceStackFiltersAttribute.cs](ExecuteServiceStackFiltersAttribute.cs), [FluentValidation](FluentValidation), [MiniProfiler](MiniProfiler) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [mvc-integration.md](../../../../docs.servicestack.net/MyApp/_pages/mvc-integration.md)
- [netcore-razor.md](../../../../docs.servicestack.net/MyApp/_pages/netcore-razor.md)
- [razor-views-vs-content-pages.md](../../../../docs.servicestack.net/MyApp/_pages/razor-views-vs-content-pages.md)
- [razor-press](../../../../docs.servicestack.net/MyApp/_pages/razor-press)

## Test evidence and review limits

No focused test file has been mapped in this first pass. This does not establish that tests are absent; search integration tests and consuming applications before treating it as a coverage gap.

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
