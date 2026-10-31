# ServiceStack.Desktop features

Embedded desktop assets and Windows-native operations for hosted web desktop applications.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net6.0;net8.0;net10.0`; see [ServiceStack.Desktop.csproj](ServiceStack.Desktop.csproj) for conditional compilation and dependencies.
- Modern .NET targets do not imply cross-platform native operation support: NativeWin and System.Drawing paths are Windows-oriented. File/process APIs require host access controls.
- Project dependencies: [ServiceStack.Text](../../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj); [ServiceStack.Interfaces](../ServiceStack.Interfaces/ServiceStack.Interfaces.csproj); [ServiceStack.Client](../ServiceStack.Client/ServiceStack.Client.csproj); [ServiceStack.Common](../ServiceStack.Common/ServiceStack.Common.csproj); [ServiceStack](../ServiceStack/ServiceStack.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| Desktop host integration | Register desktop assets, script operations and file endpoints. | [DesktopFeature.cs](DesktopFeature.cs), [DesktopConfig.cs](DesktopConfig.cs), [DesktopAssets.cs](DesktopAssets.cs) |
| Native Windows operations | Access desktop/window/clipboard capabilities through script bindings. | [NativeWin.cs](NativeWin.cs), [DesktopScripts.cs](DesktopScripts.cs) |
| Drawing-based image provider | Resize and crop images using the Windows-oriented drawing implementation. | [ImageProvider.cs](ImageProvider.cs) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [netcore-windows-desktop.md](../../../../docs.servicestack.net/MyApp/_pages/netcore-windows-desktop.md)

## Test evidence and review limits

No focused test file has been mapped in this first pass. This does not establish that tests are absent; search integration tests and consuming applications before treating it as a coverage gap.

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
