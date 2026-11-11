# ServiceStack.AI feature proposals

Deep review: 2026-10-31. Source baseline `56101ff57ab51579497684cc81ab3fd36f4c43f7`; documentation baseline `51d3ae93b07fc965093f420a425cfb921cd8166f`. Source presence is not a confirmed published-release guarantee.

All active records below are **proposed**, not accepted. Priority is an engineering recommendation within these four reviewed projects, not a quantified ROI or customer-demand claim. Effort is relative (small = local adapter/recipe; medium = bounded cross-file API; large = durable/distributed or multi-provider contract), not a delivery estimate. No runtime tests, paid provider calls or application benchmarks were run for this documentation change; cited tests were inspected as evidence of current assertions.

See [existing capabilities](../FEATURES.md), [accepted improvements](FEATURE-IMPROVEMENTS.md) and [decision workflow](../../FEATURES.md#planning-workflow). The four-project review supersedes these files' first-pass candidate descriptions; the broad [suite index](../../plan/FEATURE-PROPOSALS.md) remains a first-pass index and should be reconciled in a separate suite-wide pass.

## Review findings

Read all five implementation files, the ITypeChat/ISpeechToText contracts, ProcessUtils.RunAsync, AiModernizationTests and the public AI guide. This is a small adapter library, so the best opportunities are concrete improvements to its existing contracts rather than another full AI application.

| Direct finding | Consequence |
| --- | --- |
| KernelTypeChat sends Prompt but does not consume Schema/UserMessage or validate JSON | AI-001 defines a real typed-result contract and a compatibility decision |
| Node and local Whisper accept tokens but the invoked process runner has no token parameter | AI-002 makes cancellation and temporary-output isolation explicit |
| Remote/local Whisper fix model/language settings | AI-003 exposes supported options and optional rich result fields |
| AiModernizationTests exists and covers guards/setup | Extend its useful baseline; the first inventory's “no mapped focused tests” was incomplete |

These are proposals, including reliability improvements identified while reading code. No claim is made that the observed behavior was reproduced with a live model or local CLI.

## Recommended order

| ID | Candidate | Disposition | Potential value / relative effort |
| --- | --- | --- | --- |
| [AI-001](#ai-001-typed-validated-structured-translation) | Typed, validated structured translation | Investigate next | High fit with ServiceStack DTO-first APIs; a small library improvement with a clear contract. / Medium; limit initial schema generation/validation scope. |
| [AI-002](#ai-002-cancellable-isolated-local-ai-process-execution) | Cancellable, isolated local AI process execution | Investigate next, alongside AI-001 | High reliability value for every local Whisper/Node deployment. / Small–medium for the shared runner; platform process-tree validation adds effort. |
| [AI-003](#ai-003-configurable-multilingual-transcription-and-optional-rich-results) | Configurable multilingual transcription and optional rich results | Next, after the process foundation | Medium–high for multilingual applications and subtitle/search use cases. / Medium; richer results must remain capability-specific. |

Choose one bounded outcome per candidate before implementation. Customer demand remains unvalidated unless a dated specific source is recorded; browsing the public Ideas category did not establish demand for these particular increments.

## AI-001: Typed, validated structured translation

- Recommendation: Investigate next.
- Potential value: High fit with ServiceStack DTO-first APIs; a small library improvement with a clear contract.
- Relative effort: Medium; limit initial schema generation/validation scope.
- Confidence: High direct source evidence; no live-model quality validation.
- Customer validation needed: confirm a representative application/workflow and its current workaround before acceptance.

### Problem and existing behavior

Application code must manually combine prompting, parsing, validation and correction before safely consuming a model result as a DTO. Choosing an ITypeChat provider currently changes how much of that behavior is supplied.

`TypeChatRequest` carries Schema, Prompt, UserMessage and Json/Program translator selection. `KernelTypeChat.TranslateMessageAsync` currently sends only Prompt in ChatHistory, sets ModelId, and returns the model's text; it does not apply Schema/UserMessage or validate the response. The public AI page explicitly says non-Node consumers should implement custom validation/correction. `NodeTypeChat` delegates validation to an application-supplied typechat.mjs. Existing AiModernizationTests covers guards/process setup, not equivalent schema-validation outcomes.

### Proposed outcome

A typed translation API returning either a validated T or structured validation/refusal/provider errors, with a provider-neutral validator and a bounded optional repair policy. Preserve the existing raw ITypeChat method.

### Bounded implementation approach

1. Define the input contract: instruction, user message, expected schema/type and translator mode. First add fake-provider conformance tests that reveal adapter differences; decide and document any compatibility change to KernelTypeChat's prompt composition.
2. Introduce an additive helper/service for JSON DTO translation, using a supported schema subset and local validation. TypeScript Schema is not JSON Schema: do not reinterpret one as the other silently.
3. Where the selected backend supports constrained output, use it as an optimization; still validate locally. Otherwise build a prompt from the schema plus distinct user content.
4. Optional repair is capped by attempts and total cancellation/deadline. Return field-path errors and raw output only under caller policy. Program translation and executing model-produced programs remain outside the typed JSON helper.

### Acceptance criteria if selected

- A fake IChatCompletionService receives the intended user message and schema; named ServiceId selection is preserved.
- Invalid JSON, valid JSON of the wrong shape, extra fields, enums, nullable values, refusal and truncated output have defined results.
- Repairs stop at the configured bound and never invoke application tools or business actions. Cancellation is not treated as validation failure.
- Existing ITypeChat callers compile and retain an explicit compatibility path. Run separate live checks only after a backend/model is selected.

### Alternatives and value check

Evaluate Microsoft's existing TypeChat.NET JsonTranslator and Semantic Kernel integration before implementing a validator/repair engine. An adapter or tested composition may deliver this proposal with much less maintenance. Pin and verify dependency/schema/serialization compatibility against this project's Semantic Kernel version; the upstream README alone is not compatibility evidence. Applications can also keep custom validation or the Node TypeChat script. The ServiceStack increment is a consistent DTO/result contract, not another agent orchestration layer.

### Compatibility, ownership and risks

Do not claim arbitrary TypeScript-to-.NET equivalence or universal provider schema support. Validator dependencies should be isolated behind a small abstraction or optional adapter; ServiceStack.AI must not depend on AI.Chat for its existing JsonSchema.Net reference. Treat prompt changes as observable behavior.

### Evidence

- [ServiceStack.AI/KernelTypeChatProvider.cs](../KernelTypeChatProvider.cs)
- [ServiceStack.AI/NodeTypeChatProvider.cs](../NodeTypeChatProvider.cs)
- [ServiceStack.Interfaces/AI/ITypeChat.cs](../../ServiceStack.Interfaces/AI/ITypeChat.cs)
- [Tests: ServiceStack.AiTests/AiModernizationTests.cs](../../../tests/ServiceStack.AiTests/AiModernizationTests.cs)
- [Public docs: servicestack-ai.md](../../../../../docs.servicestack.net/MyApp/_pages/servicestack-ai.md)

## AI-002: Cancellable, isolated local AI process execution

- Recommendation: Investigate next, alongside AI-001.
- Potential value: High reliability value for every local Whisper/Node deployment.
- Relative effort: Small–medium for the shared runner; platform process-tree validation adds effort.
- Confidence: High direct source evidence of cancellation and workspace limitations.
- Customer validation needed: confirm a representative application/workflow and its current workaround before acceptance.

### Problem and existing behavior

An application cancels a transcription/translation request, but the local executable can continue until its timeout. Concurrent jobs using the same audio basename can also share the CLI-generated output path.

Both ISpeechToText and ITypeChat accept CancellationToken. NodeTypeChat and WhisperLocalSpeechToText call ProcessUtils.RunAsync without passing a token; the inspected shared RunAsync overload itself accepts timeout and callbacks but no CancellationToken. Local Whisper derives output from WorkingDirectory plus the input basename. Node arguments are constructed as a single string and user-message quotes are replaced. Existing ProcessFilter remains a useful customization hook.

### Proposed outcome

A reusable process execution contract for these two adapters that honors cancellation, isolates temporary output and preserves arguments exactly. Include typed failure diagnostics rather than requiring callers to parse arbitrary stderr.

### Bounded implementation approach

1. Add an additive cancellable process-runner interface or overload in Common; evaluate reusing the bounded Git process pattern in AI.Chat without depending on that application package.
2. Prefer ProcessStartInfo.ArgumentList on supported targets. Supply schema/user content through safe arguments/stdin/files with a documented script contract, preserving quote/newline/unicode content.
3. Give each transcription its own temporary output directory and explicit input path. Kill/wait for the process tree on cancellation/timeout, then cleanup only runner-owned files.
4. Bound stdout/stderr capture and return exit code, timeout/cancel classification and redacted diagnostics. Keep ProcessFilter compatibility but make ownership of overrides explicit.

### Acceptance criteria if selected

- A fake executable blocks and spawns a child: caller cancellation and timeout terminate/reap both within a bounded interval on supported OSes.
- Concurrent recordings named recording.wav produce their own outputs. Paths with spaces and messages containing quotes/newlines/unicode round-trip exactly.
- Success with progress on stderr is not automatically failure; nonzero exit status remains diagnosable.
- Temporary files are removed after success, launch failure, timeout and cancellation; caller-owned schemas/audio are preserved. Compare existing ProcessUtils callers before changing shared defaults.

### Alternatives and value check

Continue using ProcessFilter for controlled one-off installations, or use remote transcription/translation to avoid child processes. If a shared runner is too broad, introduce it internally in AI first with an explicit path to Common reuse.

### Compatibility, ownership and risks

Process termination differs by OS and cannot undo writes already performed by a child. Treat this as adapter reliability and usability work, not a security sandbox. Do not replace the application's typechat.mjs protocol without a compatibility adapter and migration example.

### Evidence

- [ServiceStack.AI/NodeTypeChatProvider.cs](../NodeTypeChatProvider.cs)
- [ServiceStack.AI/WhisperLocalSpeechToText.cs](../WhisperLocalSpeechToText.cs)
- [ServiceStack.Common/ProcessUtils.cs](../../ServiceStack.Common/ProcessUtils.cs)
- [Tests: ServiceStack.AiTests/AiModernizationTests.cs](../../../tests/ServiceStack.AiTests/AiModernizationTests.cs)
- [Tests: ServiceStack.AiTests/AiChatMigrationGitTests.cs](../../../tests/ServiceStack.AiTests/AiChatMigrationGitTests.cs)
- [Public docs: servicestack-ai.md](../../../../../docs.servicestack.net/MyApp/_pages/servicestack-ai.md)

## AI-003: Configurable multilingual transcription and optional rich results

- Recommendation: Next, after the process foundation.
- Potential value: Medium–high for multilingual applications and subtitle/search use cases.
- Relative effort: Medium; richer results must remain capability-specific.
- Confidence: High fixed settings are visible in source; backend-specific formats require validation.
- Customer validation needed: confirm a representative application/workflow and its current workaround before acceptance.

### Problem and existing behavior

Applications must customize process arguments or fork an adapter to change the default language/model, and cannot consume timestamps or detected language through a normalized optional result even when a backend can supply them.

WhisperApiSpeechToText fixes model to whisper-1, language to en and response_format to json. WhisperLocalSpeechToText fixes small/English/JSON arguments. TranscriptResult exposes text, confidence, raw ApiResponse and ResponseStatus. The Google Cloud adapter has its own language/model configuration. AI.Chat VoiceExtension is a separate implementation, so it should not be described as already using these adapters.

### Proposed outcome

Add explicit provider options for language/model/task where supported, plus an additive rich-transcription capability for detected language, timed segments and optional words. Keep the minimal ISpeechToText interface usable.

### Bounded implementation approach

1. Start with configurable language/model and omission of language when auto-detection is requested. Preserve current defaults for existing callers unless a documented versioned change is chosen.
2. Add optional request/result capability types in Interfaces; implement only fields actually supplied by each backend. Keep unsupported confidence/timing fields absent rather than fabricated.
3. Allowlist provider-specific option mapping; keep credentials and arbitrary command fragments outside request DTOs. Reuse AI-002 for safe CLI argument handling.
4. Evaluate a duration/size preflight and a Background Jobs example for long recordings. Automatic chunking, diarization and live streaming are separate later proposals, not implied by this API.

### Acceptance criteria if selected

- A stub HTTP handler verifies model/language/format mapping, including auto-detection and provider errors; no paid requests are needed for contract tests.
- CLI fixtures verify explicit arguments and segments for non-English content; optional missing fields remain distinguishable from zero values.
- Both providers retain cancellation, caller-owned streams and old text-only consumers. Test disposal of HTTP response/content and bounded payload handling while touching this path.
- Validate one real backend for every advertised rich-output capability before documenting it as supported.

### Alternatives and value check

Expose provider configuration alone first; applications can continue parsing ApiResponse for uncommon fields. A rich result contract is worthwhile only if two adapters can share useful semantics without losing important differences.

### Compatibility, ownership and risks

No model catalog/pricing promise is made here. Different transcription backends support different format/task combinations. Do not treat zero confidence as an actual confidence score when the backend omitted it, or import AI.Chat's media pipeline into this small package.

### Evidence

- [ServiceStack.AI/WhisperApiSpeechToText.cs](../WhisperApiSpeechToText.cs)
- [ServiceStack.AI/WhisperLocalSpeechToText.cs](../WhisperLocalSpeechToText.cs)
- [ServiceStack.Interfaces/AI/ISpeechToText.cs](../../ServiceStack.Interfaces/AI/ISpeechToText.cs)
- [ServiceStack.GoogleCloud/GoogleCloudConfig.cs](../../ServiceStack.GoogleCloud/GoogleCloudConfig.cs)
- [Tests: ServiceStack.AiTests/AiModernizationTests.cs](../../../tests/ServiceStack.AiTests/AiModernizationTests.cs)
- [Public docs: servicestack-ai.md](../../../../../docs.servicestack.net/MyApp/_pages/servicestack-ai.md)

## Related work and deliberate exclusions

- [CHAT-002/003: durable budgets/tool policy belong to AI.Chat, not these simple adapters](../../ServiceStack.AI.Chat/plan/FEATURE-PROPOSALS.md).

## External evidence and interpretation

[Microsoft TypeChat](https://github.com/microsoft/TypeChat) describes schema validation and repair as part of its translation approach. [TypeChat.NET](https://github.com/microsoft/TypeChat.net) already supplies typed JSON translation/validation/repair and a Semantic Kernel integration; AI-001 should evaluate adapting it before building equivalent machinery. Compatibility with ServiceStack's current dependencies is not established by that README. The local AI guide distinguishes the Node implementation from custom .NET validation. TypeScript schemas are not silently treated as JSON Schema. Sources checked 2026-10-31.

## Decision handling

Accept by stable ID with an agreed outcome and scope. Move that complete record to [FEATURE-IMPROVEMENTS.md](FEATURE-IMPROVEMENTS.md), record the decision/date and validation criteria, and leave a short link here. Closed/deferred proposals stay here with their reason. Implementation, successful validation and confirmed release are separate later states. This analysis changes no accepted-improvement record.
