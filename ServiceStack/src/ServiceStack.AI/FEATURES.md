# ServiceStack.AI features

Speech transcription and schema-directed natural-language translation building blocks.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net8.0;net10.0`; see [ServiceStack.AI.csproj](ServiceStack.AI.csproj) for conditional compilation and dependencies.
- This small library does not own the AI.Chat application. Local Whisper and Node TypeChat require external executables; KernelTypeChatProvider depends on Semantic Kernel configuration.
- Project dependencies: [ServiceStack.Text](../../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj); [ServiceStack.Interfaces](../ServiceStack.Interfaces/ServiceStack.Interfaces.csproj); [ServiceStack.Common](../ServiceStack.Common/ServiceStack.Common.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| Remote and local transcription | Transcribe audio through a Whisper-compatible API or a local Whisper process. | [WhisperApiSpeechToText.cs](WhisperApiSpeechToText.cs), [WhisperLocalSpeechToText.cs](WhisperLocalSpeechToText.cs) |
| Typed language translation | Translate natural language into schema-directed responses with Semantic Kernel or Node TypeChat. | [KernelTypeChatProvider.cs](KernelTypeChatProvider.cs), [NodeTypeChatProvider.cs](NodeTypeChatProvider.cs) |
| HTTP payload helpers | Build and send AI-related multipart requests. | [HttpClientUtils.cs](HttpClientUtils.cs) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [servicestack-ai.md](../../../../docs.servicestack.net/MyApp/_pages/servicestack-ai.md)

## Test evidence and review limits

No focused test file has been mapped in this first pass. This does not establish that tests are absent; search integration tests and consuming applications before treating it as a coverage gap.

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
