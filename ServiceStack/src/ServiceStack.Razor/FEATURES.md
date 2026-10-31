# ServiceStack.Razor features

Standalone legacy .NET Framework Razor view engine, compilation and HTML helpers.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net472`; see [ServiceStack.Razor.csproj](ServiceStack.Razor.csproj) for conditional compilation and dependencies.
- Targets net472 only. This is distinct from modern Razor Pages integration in ServiceStack.Mvc; prioritize maintenance and migration clarity over assuming modern framework support.
- Project dependencies: [ServiceStack.Text](../../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj); [ServiceStack.OrmLite](../../../ServiceStack.OrmLite/src/ServiceStack.OrmLite/ServiceStack.OrmLite.csproj); [ServiceStack.Client](../ServiceStack.Client/ServiceStack.Client.csproj); [ServiceStack.Common](../ServiceStack.Common/ServiceStack.Common.csproj); [ServiceStack](../ServiceStack/ServiceStack.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| View discovery and rendering | Resolve views, models, partials and layouts through a Razor plugin. | [RazorFormat.cs](RazorFormat.cs), [RazorHandler.cs](RazorHandler.cs), [ViewPageBase.cs](ViewPageBase.cs) |
| Runtime compilation | Compile C# Razor views with compiler/host integration. | [Compilation/RazorPageHost.cs](Compilation/RazorPageHost.cs), [CSharpRazorBuildProvider.cs](CSharpRazorBuildProvider.cs) |
| HTML helpers and form protection | Build HTML controls and validate anti-forgery tokens. | [Html](Html), [Html/AntiXsrf](Html/AntiXsrf) |
| Markdown rendering | Integrate Markdown content in the legacy view system. | [Formats/MarkdownFormat.cs](Formats/MarkdownFormat.cs) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [razor-notes.md](../../../../docs.servicestack.net/MyApp/_pages/razor-notes.md)
- [compiled-razor-views.md](../../../../docs.servicestack.net/MyApp/_pages/compiled-razor-views.md)
- [markdown-razor.md](../../../../docs.servicestack.net/MyApp/_pages/markdown-razor.md)
- [view-and-template-selection.md](../../../../docs.servicestack.net/MyApp/_pages/view-and-template-selection.md)

## Test evidence and review limits

Related test files located (not run in this documentation pass; not proof that every capability or database dialect is covered):

- [RazorRockstars.Web.Tests/RazorSecurityAndHardeningTests.cs](../../tests/RazorRockstars.Web.Tests/RazorSecurityAndHardeningTests.cs)

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
