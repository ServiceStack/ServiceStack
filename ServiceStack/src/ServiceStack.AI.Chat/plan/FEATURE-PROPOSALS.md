# ServiceStack.AI.Chat feature proposals

Deep review: 2026-10-31. Source baseline `56101ff57ab51579497684cc81ab3fd36f4c43f7`; documentation baseline `51d3ae93b07fc965093f420a425cfb921cd8166f`. Source presence is not a confirmed published-release guarantee.

All active records below are **proposed**, not accepted. Priority is an engineering recommendation within these four reviewed projects, not a quantified ROI or customer-demand claim. Effort is relative (small = local adapter/recipe; medium = bounded cross-file API; large = durable/distributed or multi-provider contract), not a delivery estimate. No runtime tests, paid provider calls or application benchmarks were run for this documentation change; cited tests were inspected as evidence of current assertions.

See [existing capabilities](../FEATURES.md), [accepted improvements](FEATURE-IMPROVEMENTS.md) and [decision workflow](../../FEATURES.md#planning-workflow). The four-project review supersedes these files' first-pass candidate descriptions; the broad [suite index](../../plan/FEATURE-PROPOSALS.md) remains a first-pass index and should be reconciled in a separate suite-wide pass.

## Review findings

Reviewed the typed completion client, orchestrator/tool loop, durable run schema/admission/scheduler, accounting hooks, per-user storage/deletion, MCP bounds and current test fixtures. Read README, SECURITY, API_TOOLS and durable-agent contracts; checked matching llms-py orchestration/app code for shared behavior.

| Already present | Proposal boundary |
| --- | --- |
| Durable runs, checkpointed messages, leases, compaction and refresh recovery | No proposal to “add durable agents” |
| MCP client/server, contextual tools, schema checks and approval/confirmation protocols | No proposal to “add MCP” or bypass those controls |
| MaxSteps, MaxIterations, client timeouts and usage/cost analytics | CHAT-002 adds persisted admission/consumption policy, not another dashboard |
| MCP per-principal/server gates and workspace exclusion | CHAT-003 composes these with a generic local-tool bound |
| IChatClient completions/decisions and browser run endpoints | CHAT-004 adds a typed durable application facade, with distributed workers explicitly gated |
| Scripted fake providers, Jev examples/history and replay filters | CHAT-001 starts with reusable headless fixtures, not a duplicate Decision Studio |
| Individual deletes and several export/retention paths | CHAT-005 coordinates lifecycle scope instead of claiming these are absent |

Shared provider/orchestration/UI changes must retain llms-py parity; shared UI is edited upstream and synchronized. This analysis edits only C# planning documents. No sync, live provider call, schema migration or runtime change is needed.

## Recommended order

| ID | Candidate | Disposition | Potential value / relative effort |
| --- | --- | --- | --- |
| [CHAT-002](#chat-002-persisted-run-budgets-and-admission-policy) | Persisted run budgets and admission policy | Investigate next | High for hosts paying for multi-user agent execution. / Medium–large; durable accounting and policy scope matter. |
| [CHAT-003](#chat-003-bounded-tool-scheduling-with-explicit-concurrency-policy) | Bounded tool scheduling with explicit concurrency policy | Investigate next | High operational value for hosts exposing application, filesystem and remote tools. / Medium; rollout must preserve result order and approval semantics. |
| [CHAT-004](#chat-004-typed-durable-agent-run-api-for-application-workflows) | Typed durable agent-run API for application workflows | Next candidates; single-host API before worker distribution | High fit for embedding AI in existing DTO-first applications beyond the browser. / Medium for the API; large for an optional distributed-worker phase. |
| [CHAT-001](#chat-001-reusable-offline-evaluation-fixtures-for-providertool-contracts) | Reusable offline evaluation fixtures for provider/tool contracts | Next candidates, after runner policy boundaries | Medium–high for maintainers and applications customizing providers/tools. / Medium; an end-user evaluation studio is a separate larger scope. |
| [CHAT-005](#chat-005-owner-scoped-export-and-coordinated-retention) | Owner-scoped export and coordinated retention | Later, prioritize if storage/administration is a customer pain | Medium for long-lived multi-user installations. / Medium–large; extension-owned data and shared media require careful lifecycle rules. |

Choose one bounded outcome per candidate before implementation. Customer demand remains unvalidated unless a dated specific source is recorded; browsing the public Ideas category did not establish demand for these particular increments.

## CHAT-002: Persisted run budgets and admission policy

- Recommendation: Investigate next.
- Potential value: High for hosts paying for multi-user agent execution.
- Relative effort: Medium–large; durable accounting and policy scope matter.
- Confidence: High current limits/accounting exist but are distinct from persisted spend/time policy; no billing reconciliation performed.
- Customer validation needed: confirm a representative application/workflow and its current workaround before acceptance.

### Problem and existing behavior

An operator can see usage after a run, yet needs a durable rule that prevents the next model/tool operation once a user or run has exhausted its allowed work. Restarting or resuming a run must not reset the allowance.

ChatLimits has client timeout/size, retries and MaxIterations. AgentRun has MaxSteps, context usage and leases; AppDurableAgents checks the step budget. ChatOrchestrator accumulates usage and AppExtension records cost/token analytics. MCP client concurrency limits also exist. These are not a persisted per-run/per-principal admission ledger, and context-window tokens are not cumulative billed tokens. Decisions intentionally bypass chat history/filter accounting.

### Proposed outcome

Opt-in host-owned run policies for elapsed execution time, model-call count, tool-call count and estimated token/cost ceilings, persisted with the run and surfaced in status. Start with enforceable call/time bounds; treat money as estimated usage control.

### Bounded implementation approach

1. Resolve policy from trusted host/user context at run admission, not from arbitrary metadata alone. Persist the chosen limits, consumption and policy version; users may request stricter limits, not raise their host ceiling.
2. Check before each provider/tool operation and after response accounting. Include retries, tool-loop continuations and compaction in the defined accounting policy. Reserve capacity atomically where concurrent runs share an allowance.
3. Expose a distinct exhausted state/reason and remaining allowance. Define whether approval waits count toward a deadline, and who may extend a paused budget.
4. Record missing price/usage as unknown; use configured conservative behavior rather than zero-cost inference. Integrate decisions separately or explicitly exclude them from a “chat-only” budget. Preserve no-replay behavior for uncertain paid requests.

### Acceptance criteria if selected

- Resume after restart without resetting counters; concurrent admissions cannot both spend the same reserved allowance.
- Fake providers drive tool loops, retries and compaction across a boundary; the next operation is denied at the specified point.
- Approval pause, cancellation and deadline exhaustion remain distinguishable. Unknown pricing/usage is visible and follows configured policy.
- Existing MaxSteps/MaxIterations behavior remains compatible by default. No claim of an exact vendor billing cap or rollback of an in-flight charge.

### Alternatives and value check

Use provider account quotas and application rate limiting where those are sufficient. A ChatRequestFilter can reject an initial request, but alone does not enforce a durable budget across slices and all subsequent operations.

### Compatibility, ownership and risks

Usage may arrive only at completion, so an in-flight request can overshoot an estimate. Shared behavior/configuration/UI changes must be implemented in llms-py first or together; C# persistence uses additive OrmLite schema. Do not log credentials/prompts in the budget ledger.

### Evidence

- [ServiceStack.AI.Chat/ChatFeature.cs](../ChatFeature.cs)
- [ServiceStack.AI.Chat/Db/DurableAgentTables.cs](../Db/DurableAgentTables.cs)
- [ServiceStack.AI.Chat/Extensions/App/AppDurableAgents.cs](../Extensions/App/AppDurableAgents.cs)
- [ServiceStack.AI.Chat/Pipeline/ChatOrchestrator.cs](../Pipeline/ChatOrchestrator.cs)
- [ServiceStack.AI.Chat/Extensions/App/AppExtension.cs](../Extensions/App/AppExtension.cs)
- [ServiceStack.AI.Chat/README.md](../README.md)
- [Tests: ServiceStack.AiTests/AiChatThreadTests.cs](../../../tests/ServiceStack.AiTests/AiChatThreadTests.cs)
- [Public docs: chat/analytics.md](../../../../../docs.servicestack.net/MyApp/_pages/chat/analytics.md)

## CHAT-003: Bounded tool scheduling with explicit concurrency policy

- Recommendation: Investigate next.
- Potential value: High operational value for hosts exposing application, filesystem and remote tools.
- Relative effort: Medium; rollout must preserve result order and approval semantics.
- Confidence: High direct evidence of broad Task.WhenAll execution; remote MCP already has its own bounds.
- Customer validation needed: confirm a representative application/workflow and its current workaround before acceptance.

### Problem and existing behavior

A model can request several tools in one turn. Unlimited fan-out can consume resources, and two writes to the same workspace or external resource may require serialization even though independent read tools should stay parallel.

ChatOrchestrator builds execTasks for non-pending calls and awaits Task.WhenAll. ChatTool has Safety, Source and ApprovalHandler, but no generic concurrency/serialization policy. McpClientLimits already bounds calls per principal/server and queue length; WorkspaceOperations protects specific workspace operations. Those protections must be retained rather than replaced with a second incompatible queue.

### Proposed outcome

A central bounded executor for ordinary chat tool calls, with optional host/tool-specific serialization keys and observable waiting status. Preserve parallel execution where allowed.

### Bounded implementation approach

1. Introduce host-level maximum active/queued tool calls, cancellation and per-call timeout policy. Allow registration to declare a concurrency key or explicit ordered execution requirement.
2. Reuse or compose existing MCP gates and workspace exclusion; define acquisition order to avoid nested-gate deadlocks. Safety annotations inform defaults but never grant authority or prove that a tool is safe to parallelize.
3. Preserve input/result ordering and the canonical assistant/tool result group even when completion order differs. Pending approvals remain outside executable work until explicitly continued.
4. Surface queue wait separately from execution duration. Direct Tools UI calls and inbound MCP should adopt the executor only after their separate authorization/confirmation contracts are mapped.

### Acceptance criteria if selected

- A fake tool batch never exceeds configured concurrency; keyed writes serialize while unrelated reads progress.
- Cancellation while waiting does not execute the handler later; timeout and partial-batch failures produce one result per call with stable correlation.
- Existing MCP call limits, edited approval arguments and no-auto-replay invariants still hold.
- Stress concurrent runs and prove no lock-order deadlock; unchanged settings retain current compatibility defaults until rollout policy is agreed.

### Alternatives and value check

Applications can already wrap handlers with semaphores, and the MCP transport already handles its own bounds. Prefer a common helper if a global executor would complicate extension compatibility.

### Compatibility, ownership and risks

Timeout cannot undo a side effect or forcibly stop arbitrary in-process code. Never automatically retry a non-idempotent tool after an unknown outcome. Implement shared orchestration semantics in both C# and llms-py; shared UI edits originate upstream.

### Evidence

- [ServiceStack.AI.Chat/Pipeline/ChatOrchestrator.cs](../Pipeline/ChatOrchestrator.cs)
- [ServiceStack.AI.Chat/Pipeline/ToolRegistry.cs](../Pipeline/ToolRegistry.cs)
- [ServiceStack.AI.Chat/Pipeline/ToolApproval.cs](../Pipeline/ToolApproval.cs)
- [ServiceStack.AI.Chat/Pipeline/WorkspaceOperations.cs](../Pipeline/WorkspaceOperations.cs)
- [ServiceStack.AI.Chat/Extensions/McpClient/McpClientConfiguration.cs](../Extensions/McpClient/McpClientConfiguration.cs)
- [ServiceStack.AI.Chat/Extensions/McpClient/McpClientConnectionManager.cs](../Extensions/McpClient/McpClientConnectionManager.cs)
- [Tests: ServiceStack.AiTests/AiChatClientTests.cs](../../../tests/ServiceStack.AiTests/AiChatClientTests.cs)
- [Tests: ServiceStack.AiTests/McpClientTests.cs](../../../tests/ServiceStack.AiTests/McpClientTests.cs)
- [Public docs: chat/tools.md](../../../../../docs.servicestack.net/MyApp/_pages/chat/tools.md)

## CHAT-004: Typed durable agent-run API for application workflows

- Recommendation: Next candidates; single-host API before worker distribution.
- Potential value: High fit for embedding AI in existing DTO-first applications beyond the browser.
- Relative effort: Medium for the API; large for an optional distributed-worker phase.
- Confidence: High gap between existing completion client and durable browser run lifecycle; distributed guarantees are deliberately limited.
- Customer validation needed: confirm a representative application/workflow and its current workaround before acceptance.

### Problem and existing behavior

An application can call IChatClient.ChatAsync, but integrating a durable run with “start now, inspect later, cancel or wait for approval” requires using the chat extension's JSON/thread endpoints and reproducing lifecycle details.

IChatClient provides typed completions and decisions, not typed durable run submission/status. The app extension already queues threads/{id}/chat, exposes updates/cancellation and persists AgentRun/AgentStep/canonical messages. AgentScheduler runs in-process. Its startup calls RequeueInterruptedAgentRuns, which requeues running rows; generic UpdateAgentRun writes are not ownership-fenced. The durable architecture explicitly names expired-lease recovery/locking as future high-scale work. Existing RDBMS persistence is not sufficient evidence for arbitrary multi-host failover.

### Proposed outcome

An additive IAgentClient (illustrative name) and DTO endpoints for owner-scoped submit/status/messages/cancel/await, built over the existing durable coordinator. Return stable run/thread references and explicit approval/budget states.

### Bounded implementation approach

1. Extract a shared admission/lifecycle service from QueueChatAsync rather than calling internal HTTP handlers or directly inserting AgentRun rows. Preserve project/workspace capture, submission exclusion and current authorization.
2. Accept an explicit trusted user identity through server integration or derive it from IRequest; do not treat caller metadata.user as authentication for external APIs. Keep ownership checks consistent for status and results.
3. Add bounded wait/progress abstractions using existing updates and paging; cancellation of waiting is separate from cancellation of the durable run.
4. Keep v1 single-host and store-compatible. An optional Background Jobs/dedicated-worker phase requires explicit lease fencing on state/checkpoint writes, expired-owner recovery, cross-process wakeups and a workspace ownership design. Do not add a second scheduler as a shortcut.

### Acceptance criteria if selected

- Starting through the typed API and browser path produces the same persisted history/workspace/approval semantics and handles duplicate submissions.
- A request disconnect does not cancel an admitted durable run; explicit cancellation survives refresh/restart.
- A different user cannot observe status, approval data or results. Await timeouts do not mark the run failed.
- Any later distributed-worker acceptance must include two-process startup/lease-loss tests proving a live run is not stolen, a stale worker cannot commit, and uncertain side effects are not auto-replayed.

### Alternatives and value check

Document the existing extension endpoints first, or wrap them in application DTOs. If that satisfies integrations, a small typed SDK can precede any new public server interface. Reuse ServiceStack Jobs only at a deliberately designed execution boundary.

### Compatibility, ownership and risks

Shared admission semantics require llms-py coordination; a C# typed facade can be host-specific. App_Data/workspace operations currently assume one owning process. Thread history, provider-native signed state and approval identity must remain canonical and must not be serialized as an arbitrary job payload.

### Evidence

- [ServiceStack.AI.Chat/IChatClient.cs](../IChatClient.cs)
- [ServiceStack.AI.Chat/Extensions/App/AppDurableAgents.cs](../Extensions/App/AppDurableAgents.cs)
- [ServiceStack.AI.Chat/Extensions/App/AgentScheduler.cs](../Extensions/App/AgentScheduler.cs)
- [ServiceStack.AI.Chat/Db/ChatDbDurable.cs](../Db/ChatDbDurable.cs)
- [ServiceStack.AI.Chat/Extensions/App/DURABLE_AGENTS.md](../Extensions/App/DURABLE_AGENTS.md)
- [ServiceStack.AI.Chat/Pipeline/WorkspaceOperations.cs](../Pipeline/WorkspaceOperations.cs)
- [Tests: ServiceStack.AiTests/AiChatMigrationAppTests.cs](../../../tests/ServiceStack.AiTests/AiChatMigrationAppTests.cs)
- [Tests: ServiceStack.AiTests/AiChatThreadTests.cs](../../../tests/ServiceStack.AiTests/AiChatThreadTests.cs)
- [Public docs: chat/api.md](../../../../../docs.servicestack.net/MyApp/_pages/chat/api.md)

## CHAT-001: Reusable offline evaluation fixtures for provider/tool contracts

- Recommendation: Next candidates, after runner policy boundaries.
- Potential value: Medium–high for maintainers and applications customizing providers/tools.
- Relative effort: Medium; an end-user evaluation studio is a separate larger scope.
- Confidence: High reusable fake-provider fixtures already exist; product demand unknown.
- Customer validation needed: confirm a representative application/workflow and its current workaround before acceptance.

### Problem and existing behavior

Extensions and provider changes need repeatable checks for tool selection, approval and translated history. Existing regression tests help maintainers, but application authors cannot easily package a reusable redacted scenario without building a test harness.

AiChatClientTests already has FakeChatProvider and ScriptedChatProvider; Google tool-combination and replay-filter tests exercise provider-specific history. Jev Decision Studio already has examples/history for its narrow decision workflow, so “add an evaluation UI” is too broad a new-feature claim. ProviderReplayFilters adapt outbound native history; they do not replay tool execution safely.

### Proposed outcome

A versioned fixture format and small headless evaluation runner for scripted provider responses and stubbed tools, with structured assertions and opt-in live comparison later.

### Bounded implementation approach

1. Extract the reusable scripted-provider idea into supported test helpers. Describe messages, model metadata, stub tool outcomes and expected lifecycle events; capture no credentials by default.
2. Run offline with a network-denying transport and isolated in-memory/temp state. Do not mount the production ToolRegistry or invoke real API/file/process tools.
3. Assert schemas, identity boundaries, approval decisions, provider projection and final DTO/error shape. Keep nondeterministic natural-language quality scores outside deterministic pass/fail checks.
4. Live model evaluations require an explicit model/cost selection and separate data-use policy. A later UI should reuse existing extension/Decision Studio concepts only where their semantics fit.

### Acceptance criteria if selected

- Replaying a fixture produces identical structured results and performs zero live tool/provider calls.
- Fixtures cover cross-user isolation, approval editing, interrupted slices and Google signed-history projection without altering canonical history.
- Redaction removes tokens, private file URLs and user payloads selected for exclusion; fixtures record version/model metadata.
- Existing tests remain independent; helpers reduce duplicated setup without encoding the implementation under test as the assertion.

### Alternatives and value check

Continue ordinary NUnit fixtures or use external evaluation tooling. The proposal is justified only if shared helpers/fixture interchange remove recurring application setup. Avoid building a benchmark platform before proving that benefit.

### Compatibility, ownership and risks

A sanitized fixture is not production authorization. Never reuse approval tokens, re-execute unsafe tools, or retry uncertain paid provider calls. Shared fixture semantics should align with Python, while storage/test adapters can differ.

### Evidence

- [ServiceStack.AI.Chat/Pipeline/ProviderReplayContext.cs](../Pipeline/ProviderReplayContext.cs)
- [ServiceStack.AI.Chat/Pipeline/ToolRegistry.cs](../Pipeline/ToolRegistry.cs)
- [ServiceStack.AI.Chat/Extensions/Jev/README.md](../Extensions/Jev/README.md)
- [Tests: ServiceStack.AiTests/AiChatClientTests.cs](../../../tests/ServiceStack.AiTests/AiChatClientTests.cs)
- [Tests: ServiceStack.AiTests/AiChatProviderReplayFilterTests.cs](../../../tests/ServiceStack.AiTests/AiChatProviderReplayFilterTests.cs)
- [Tests: ServiceStack.AiTests/AiChatGoogleToolCombinationTests.cs](../../../tests/ServiceStack.AiTests/AiChatGoogleToolCombinationTests.cs)
- [Public docs: chat/decision-studio.md](../../../../../docs.servicestack.net/MyApp/_pages/chat/decision-studio.md)

## CHAT-005: Owner-scoped export and coordinated retention

- Recommendation: Later, prioritize if storage/administration is a customer pain.
- Potential value: Medium for long-lived multi-user installations.
- Relative effort: Medium–large; extension-owned data and shared media require careful lifecycle rules.
- Confidence: Medium: individual deletion/export paths exist; a complete per-account lifecycle service was not found in the inspected core paths.
- Customer validation needed: confirm a representative application/workflow and its current workaround before acceptance.

### Problem and existing behavior

Administrators need to control database/media growth and users need a private portable history export. Deleting one thread, deleting one gallery row and exporting a published project do not describe a complete account-data lifecycle.

AppExtension deletes threads and durable thread data; ChatDb has individual request/media deletion methods. ShareStatic exports public/static project output, Jev has recipe exports, MCP has credential deletion, and Gemini has some analytics retention. Chat files also live in App_Data while canonical runs/messages live in OrmLite. These existing mechanisms should be coordinated, not replaced or described as nonexistent.

### Proposed outcome

A private owner-authorized export and dry-run retention service with extension participation. Start with explicit opt-in retention of terminal run/request history and unused media; never enable destructive cleanup by default.

### Bounded implementation approach

1. Define an export manifest of canonical messages, threads, run summaries and owner-referenced media, excluding credentials, approval secrets and host configuration. Stream/page bounded output rather than materializing an entire account.
2. Add extension lifecycle hooks to enumerate owned artifacts and references. Report unsupported extension data instead of claiming a complete export.
3. Offer a dry-run cleanup plan, terminal-state/age filters, bounded batches and an execution audit. Shared content-addressed blobs require verified reference checks; a user's deletion must not remove another user's content.
4. Recheck active-run/workspace/approval leases before cleanup. Define DB-plus-file recovery and backup policy before adding whole-account erasure; do not start with a cross-store “atomic delete” claim.

### Acceptance criteria if selected

- Export includes full canonical paged history, not merely the selected UI head/tail window, and cannot include another user's data.
- Secrets and signed approval grants are excluded. Published/static exports remain separate operations.
- Dry-run is read-only; executing a plan skips changed/active resources and resumes safely after partial failure.
- Shared-media references survive other-user cleanup; extension omissions are visible. Retention does not silently remove data still needed to resume a run.

### Alternatives and value check

Keep backups and application-managed retention if that is sufficient. A simple request-history retention option may deliver most value before generalized lifecycle hooks. The feature should not promise regulatory compliance or complete erasure from external providers/backups.

### Compatibility, ownership and risks

Database rows, local files, remote Gemini stores and published copies have distinct ownership/lifetimes. Do not use the default user as an administrative wildcard. Shared UI and extension lifecycle changes must be coordinated upstream; C# host credentials remain C#-owned.

### Evidence

- [ServiceStack.AI.Chat/Extensions/App/AppExtension.cs](../Extensions/App/AppExtension.cs)
- [ServiceStack.AI.Chat/Db/ChatDb.cs](../Db/ChatDb.cs)
- [ServiceStack.AI.Chat/Db/ChatDbDurable.cs](../Db/ChatDbDurable.cs)
- [ServiceStack.AI.Chat/Extensions/Gallery/GalleryExtension.cs](../Extensions/Gallery/GalleryExtension.cs)
- [ServiceStack.AI.Chat/Configuration/ChatAppData.cs](../Configuration/ChatAppData.cs)
- [ServiceStack.AI.Chat/SECURITY.md](../SECURITY.md)
- [Public docs: chat/data.md](../../../../../docs.servicestack.net/MyApp/_pages/chat/data.md)
- [Public docs: chat/publishing.md](../../../../../docs.servicestack.net/MyApp/_pages/chat/publishing.md)

## Related work and deliberate exclusions

- [AI-001/002/003: reusable translation/transcription adapters are a separate library](../../ServiceStack.AI/plan/FEATURE-PROPOSALS.md).
- [SERVER-001/003: durable delivery and transaction integration may support application workflows](../../ServiceStack.Server/plan/FEATURE-PROPOSALS.md).
- Deferred a general multi-host agent service, shared team workspaces and automatic provider/tool replay. They require new ownership and execution guarantees beyond the bounded proposals above. No new MCP, RAG, PDF Studio or model-provider integration is claimed necessary merely because a first-pass summary omitted it.

## External evidence and interpretation

No new provider-specific API, model availability or price claim is required by these proposals. Priorities are derived from inspected implementation paths and their reuse potential. Provider compatibility still requires selected-backend contract/live validation if a proposal is accepted.

## Decision handling

Accept by stable ID with an agreed outcome and scope. Move that complete record to [FEATURE-IMPROVEMENTS.md](FEATURE-IMPROVEMENTS.md), record the decision/date and validation criteria, and leave a short link here. Closed/deferred proposals stay here with their reason. Implementation, successful validation and confirmed release are separate later states. This analysis changes no accepted-improvement record.
