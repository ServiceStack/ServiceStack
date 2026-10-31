# ServiceStack.GoogleCloud features

Google Cloud Storage virtual files and Google speech transcription.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net472;net6.0;net8.0;net10.0`; see [ServiceStack.GoogleCloud.csproj](ServiceStack.GoogleCloud.csproj) for conditional compilation and dependencies.
- Requires Google credentials and configured cloud resources. Storage and speech operations depend on Google SDKs; this catalog does not establish live service compatibility.
- Project dependencies: [ServiceStack.Text](../../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj); [ServiceStack.Common](../ServiceStack.Common/ServiceStack.Common.csproj); [ServiceStack.Interfaces](../ServiceStack.Interfaces/ServiceStack.Interfaces.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| Cloud Storage VFS | Read, write and enumerate bucket content through virtual files. | [GoogleCloudVirtualFiles.cs](GoogleCloudVirtualFiles.cs), [GoogleCloudVirtualFile.cs](GoogleCloudVirtualFile.cs), [GoogleCloudVirtualDirectory.cs](GoogleCloudVirtualDirectory.cs) |
| Speech transcription | Configure speech recognition and transcribe recordings through the common speech interface. | [GoogleCloudSpeechToText.cs](GoogleCloudSpeechToText.cs), [GoogleCloudConfig.cs](GoogleCloudConfig.cs) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [virtual-file-system.md](../../../../docs.servicestack.net/MyApp/_pages/virtual-file-system.md)
- [servicestack-ai.md](../../../../docs.servicestack.net/MyApp/_pages/servicestack-ai.md)

## Test evidence and review limits

No focused test file has been mapped in this first pass. This does not establish that tests are absent; search integration tests and consuming applications before treating it as a coverage gap.

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
