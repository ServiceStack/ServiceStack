# ServiceStack feature proposals

Deep review: 2026-10-31. Source baseline `56101ff57ab51579497684cc81ab3fd36f4c43f7`; documentation baseline `51d3ae93b07fc965093f420a425cfb921cd8166f`. Source presence is not a confirmed published-release guarantee.

All active records below are **proposed**, not accepted. Priority is an engineering recommendation within these four reviewed projects, not a quantified ROI or customer-demand claim. Effort is relative (small = local adapter/recipe; medium = bounded cross-file API; large = durable/distributed or multi-provider contract), not a delivery estimate. No runtime tests, paid provider calls or application benchmarks were run for this documentation change; cited tests were inspected as evidence of current assertions.

See [existing capabilities](../FEATURES.md), [accepted improvements](FEATURE-IMPROVEMENTS.md) and [decision workflow](../../FEATURES.md#planning-workflow). The four-project review supersedes these files' first-pass candidate descriptions; the broad [suite index](../../plan/FEATURE-PROPOSALS.md) remains a first-pass index and should be reconciled in a separate suite-wide pass.

## Review findings

Reviewed the request/command pipeline, native metadata generation and schema routes, upload handling, SSE subscription identity, and operation/MQ/job telemetry with associated tests. Existing capabilities materially narrow the original proposal list.

| Finding | Proposal consequence |
| --- | --- |
| Metadata and JSON-schema export already exist | CORE-001 adds comparison/canonicalization, not another exporter |
| Command retries and job deduplication already exist | CORE-002 specifies a distinct opt-in mutation result/ownership contract |
| Upload limits, access checks, transforms and whole-file endpoints already exist | CORE-004 adds staged session/resume semantics only |
| SSE subscription counters and Redis live fan-out exist | CORE-005 requires a separate durable cursor; reconnect alone is not replay |
| W3C propagation, operation/MQ metrics, local trace filtering/view and external trace links already exist | CORE-003 is closed as substantially covered, below |

## Recommended order

| ID | Candidate | Disposition | Potential value / relative effort |
| --- | --- | --- | --- |
| [CORE-001](#core-001-api-contract-snapshots-and-compatibility-reports) | API contract snapshots and compatibility reports | Investigate next | High potential reach: every application distributing DTOs or exposing APIs. / Medium; source-language compatibility rules add a later large phase. |
| [CORE-002](#core-002-opt-in-mutation-request-idempotency) | Opt-in mutation request idempotency | Design after demand validation | High for order/payment/provisioning APIs; narrower applicability than contract tooling. / Large; durable provider and transaction semantics dominate. |
| [CORE-004](#core-004-resumable-upload-sessions-over-configured-upload-locations) | Resumable upload sessions over configured upload locations | Later, validate large-file use cases | Medium–high for media/import workloads on unreliable connections. / Large; backend capability and client work are significant. |
| [CORE-005](#core-005-optional-replayable-notification-feeds-for-server-events) | Optional replayable notification feeds for Server Events | Later, after a concrete notification workflow | Medium for disconnected dashboards and user notifications; live invalidation users may not need it. / Large; durable storage, replay ordering and client changes. |
| CORE-003 | Correlated operational view | Closed: substantially covered by current implementation | Preserve ID; no new work accepted |

Choose one bounded outcome per candidate before implementation. Customer demand remains unvalidated unless a dated specific source is recorded; browsing the public Ideas category did not establish demand for these particular increments.

### Recommended starting points across the four reviewed projects

This order favors a demonstrable improvement with a bounded first release. It is not a commitment to implement all candidates, and it does not rank customer demand that has not been measured.

| Order | Candidate | Why start here | First decision/output |
| --- | --- | --- | --- |
| 1 | [AI-002: local process execution](../../ServiceStack.AI/plan/FEATURE-PROPOSALS.md#ai-002-cancellable-isolated-local-ai-process-execution) | Directly observed cancellation/argument/output-isolation limitations; reusable for two small adapters | A runner contract and fake-executable proof of cancellation/isolation |
| 2 | [SERVER-003: transaction-safe AutoCrud jobs](../../ServiceStack.Server/plan/FEATURE-PROPOSALS.md#server-003-transaction-safe-autocrud-job-emission) | Connects existing AutoCrud and DbJobs capabilities with a small recipe first | A rollback/commit example proving whether any public API addition is needed |
| 3 | [CHAT-003: bounded tool scheduling](../../ServiceStack.AI.Chat/plan/FEATURE-PROPOSALS.md#chat-003-bounded-tool-scheduling-with-explicit-concurrency-policy) | A concrete Task.WhenAll execution boundary; complements existing MCP/workspace protections | Concurrency policy plus scripted cancellation/order scenarios |
| 4 | [AI-001: validated typed translation](../../ServiceStack.AI/plan/FEATURE-PROPOSALS.md#ai-001-typed-validated-structured-translation) | Strong DTO-first fit with a verified adapter-contract mismatch | Agree the schema format and raw-method compatibility behavior |
| 5 | [CORE-001: contract compatibility reports](#core-001-api-contract-snapshots-and-compatibility-reports) | Broad framework value from metadata ServiceStack already owns | A deterministic snapshot prototype and a small reviewed compatibility rule set |
| 6 | [CHAT-002: durable run budgets](../../ServiceStack.AI.Chat/plan/FEATURE-PROPOSALS.md#chat-002-persisted-run-budgets-and-admission-policy) | Builds on existing durable runs/accounting, with meaningful host operations value | Define enforceable call/time limits before estimated spend controls |

SERVER-002 cursor paging is the next performance-oriented design spike once a representative table/index/workload is available. Durable webhook delivery, request idempotency and distributed agent workers have larger semantics and should follow a concrete user workflow.

## CORE-001: API contract snapshots and compatibility reports

- Recommendation: Investigate next.
- Potential value: High potential reach: every application distributing DTOs or exposing APIs.
- Relative effort: Medium; source-language compatibility rules add a later large phase.
- Confidence: High technical fit; no validated customer/adoption estimate.
- Customer validation needed: confirm a representative application/workflow and its current workaround before acceptance.

### Problem and existing behavior

A DTO refactor can silently change a public route, serialized member or generated client contract. Today a team can export metadata, but still needs its own policy to answer whether the new API is compatible with a previously deployed version.

`NativeTypesService` already serves `/types/metadata`; `NativeTypesMetadata.GetMetadataTypes` walks operations and DTOs. `MetadataSchema.cs` serves API and AutoQuery JSON schemas, with customization callbacks. `ServiceMetadata` already supplies visibility and authorization information. These are export/discovery facilities, not a versioned breaking-change report. A targeted search of the core and relevant tests did not locate a built-in API compatibility comparison engine. Database schema diff in Server solves a different problem.

### Proposed outcome

An offline, deterministic contract snapshot plus a structured compatibility report that CI and developer tools can consume. Keep wire compatibility, client-source compatibility and access-policy changes as separate report categories.

### Bounded implementation approach

1. Build a versioned canonical snapshot from existing metadata, with stable sorting, fully qualified identities and serializer naming/settings recorded. Separate request and response reachability; do not compare arbitrary implementation types.
2. Start with removed operations/routes, verb changes, serialized-name/type changes and field/enum changes. Report unknown compatibility rather than guess for custom serializers, unions, generics or unknown attributes.
3. Support explicit baselines/suppressions with reasons and JSON plus human-readable reports. Keep the first implementation an offline library API; coordinate any `x`/CLI command in its actual owning repository.
4. Add generated C#/TypeScript compilation checks later. Sharing schema-policy fixtures with OpenAPI adapters is useful, but adapter document equality is not the compatibility definition.

### Acceptance criteria if selected

- Exporting the same host twice produces byte-identical snapshots; operation registration order does not change the result.
- Fixtures distinguish removed request fields from removed response fields, requiredness changes, enum additions/removals, renamed wire members, route changes and deliberately ignored fields.
- Compare a frozen old consumer against new responses and a new consumer against old responses; JSON compatibility and generated-source compilation have independent outcomes.
- Restricted metadata stays restricted. An offline trusted snapshot must not be exposed through a new anonymous endpoint.

### Alternatives and value check

Use an external OpenAPI diff tool where the OpenAPI document captures the full contract. The additional value here is ServiceStack-native DTO metadata, client generation and serializer awareness. If that distinction is not needed by users, ship a tested OpenAPI-diff recipe instead.

### Compatibility, ownership and risks

Serializer settings and custom filters can change behavior beyond what metadata expresses. Record assumptions and snapshot format versions. Do not claim that schema compatibility proves behavioral compatibility or that an auth change is a type-system break.

### Evidence

- [ServiceStack/NativeTypes/NativeTypesService.cs](../NativeTypes/NativeTypesService.cs)
- [ServiceStack/NativeTypes/NativeTypesMetadata.cs](../NativeTypes/NativeTypesMetadata.cs)
- [ServiceStack/MetadataSchema.cs](../MetadataSchema.cs)
- [ServiceStack.Client/MetadataTypes.cs](../../ServiceStack.Client/MetadataTypes.cs)
- [Tests: ServiceStack.WebHost.Endpoints.Tests/MetadataModernizationTests.cs](../../../tests/ServiceStack.WebHost.Endpoints.Tests/MetadataModernizationTests.cs)
- [Public docs: api-schema.md](../../../../../docs.servicestack.net/MyApp/_pages/api-schema.md)
- [Public docs: dto-langs.md](../../../../../docs.servicestack.net/MyApp/_pages/dto-langs.md)

## CORE-002: Opt-in mutation request idempotency

- Recommendation: Design after demand validation.
- Potential value: High for order/payment/provisioning APIs; narrower applicability than contract tooling.
- Relative effort: Large; durable provider and transaction semantics dominate.
- Confidence: Medium gap confidence in inspected paths; no proof that every application recipe lacks it.
- Customer validation needed: confirm a representative application/workflow and its current workaround before acceptance.

### Problem and existing behavior

A client times out after a successful mutation and retries. Request retries, response caching and command retries do not by themselves prevent the mutation from running twice.

The host has request/response filters, typed service execution and structured errors. `CommandsFeature` already has retry policies, including `[Retry]`. Background Jobs already supports reference-based deduplication and transactional enqueue. A targeted search of core/Server/contracts did not find a general HTTP idempotency store with payload conflict detection and response replay. Existing cache-response/ETag handling is principally response validation/caching, not mutation ownership.

### Proposed outcome

An opt-in request execution policy and pluggable durable store with explicit guarantees. The first supported scope should be bounded JSON DTO mutations, with idempotency limited to the authenticated principal, operation and key.

### Bounded implementation approach

1. Specify the state machine before adding attributes: reserved, executing, completed and outcome-unknown. Bind keys to operation identity, principal/tenant scope and a canonical payload fingerprint.
2. Provide atomic create/claim and fencing semantics in the store. Duplicates either receive the stored result or an explicit in-progress/unknown response; different payloads conflict.
3. Define a same-database integration in Server for operations whose business mutation and completion record can share a transaction. Separate that stronger guarantee from arbitrary external side effects.
4. Store bounded response/status data and approved replay headers; authorize every replay. Include key TTL, cleanup and diagnostics. Client helpers are optional follow-up work, not a prerequisite.

### Acceptance criteria if selected

- Concurrent same-key submissions execute one supported mutation; differing payloads are rejected deterministically.
- Kill execution before claim, during the transaction and after commit but before the HTTP response; document the observable outcome of each window.
- User/tenant isolation, permission changes, expired keys and store outages cannot cause data disclosure or accidental execution.
- Explicitly reject unsupported streaming/file/custom-result cases in the first version. Preserve cancellation and normal error shapes.

### Alternatives and value check

Use a business-level unique constraint, external gateway support or an application-specific transaction/outbox where it solves the workflow. These may be sufficient and cheaper. Prototype one concrete customer workflow before standardizing a framework-wide abstraction.

### Compatibility, ownership and risks

No exactly-once promise for external side effects. An expired lock must not authorize replay of an uncertain operation. Logical request identity is not always a hash of serialized bytes; hashing rules must preserve absent-vs-null and relevant header semantics. The core package must not acquire a hard OrmLite dependency.

### Evidence

- [ServiceStack/ServiceStackHost.cs](../ServiceStackHost.cs)
- [ServiceStack/Host/ServiceRunner.cs](../Host/ServiceRunner.cs)
- [ServiceStack/CommandsFeature.cs](../CommandsFeature.cs)
- [ServiceStack/HttpCacheFeature.cs](../HttpCacheFeature.cs)
- [ServiceStack.Server/DbJobs.cs](../../ServiceStack.Server/DbJobs.cs)
- [Tests: ServiceStack.Extensions.Tests/DbJobsTests.cs](../../../tests/ServiceStack.Extensions.Tests/DbJobsTests.cs)

## CORE-004: Resumable upload sessions over configured upload locations

- Recommendation: Later, validate large-file use cases.
- Potential value: Medium–high for media/import workloads on unreliable connections.
- Relative effort: Large; backend capability and client work are significant.
- Confidence: High that the inspected upload path processes whole files; demand and backend support unvalidated.
- Customer validation needed: confirm a representative application/workflow and its current workaround before acceptance.

### Problem and existing behavior

A failed large file transfer has to be restarted even when most of its bytes arrived. Application developers also need a consistent way to expire abandoned uploads and validate the completed object.

`FilesUploadFeature` already owns upload locations, role/API-key checks, path resolution, byte/count/extension limits and `TransformFileAsync`. `StoreFileUploadService` consumes ordinary request files. The inspected plugin has no upload-session/part-commit protocol. VFS support does not imply append, atomic rename or multipart support on every backend.

### Proposed outcome

An optional upload-session API that reuses location policy and publishes a file only after completion validation. Existing one-request uploads remain the default.

### Bounded implementation approach

1. Define create/status/write-part/complete/abort operations with opaque owner-bound session IDs, expected length, ordered offsets or part identifiers, expiry and optional integrity metadata.
2. Introduce an optional staging/multipart capability rather than extending every IVirtualFiles implementation with false guarantees. Start with a physical staging adapter; evaluate cloud-native multipart adapters separately.
3. Apply maximum size/quota checks while staging, recheck authorization at completion, run transforms once on a complete file and use the existing final path policy.
4. Supply a browser resume helper only after server semantics are stable. Treat abandoned-part cleanup and deployment across nodes as part of the feature.

### Acceptance criteria if selected

- Resume after client interruption, repeat a completed part and reject conflicting/out-of-order parts under the chosen protocol.
- Two completion requests publish one final object; incomplete uploads never become downloadable through the ordinary upload endpoint.
- Enforce aggregate size, session ownership, expiry and safe paths; cleanup cannot delete a completed object.
- Run existing upload security/compatibility tests unchanged, plus backend-specific staging/commit failures.

### Alternatives and value check

Use direct object-storage multipart uploads or integrate a standard resumable-upload server. Prefer a documented composition if that meets user needs without creating another protocol. Evaluate standard interoperability before choosing ServiceStack-specific DTOs.

### Compatibility, ownership and risks

Do not buffer entire uploads into RAM, promise atomic publish on unsupported VFS backends or assume all nodes share staging files. AV/content inspection and progress transport remain host policy. A generic upload adapter must not silently bypass location authorization.

### Evidence

- [ServiceStack/FilesUploadFeature.cs](../FilesUploadFeature.cs)
- [Tests: ServiceStack.Extensions.Tests/FileUploadCompatibilityTests.cs](../../../tests/ServiceStack.Extensions.Tests/FileUploadCompatibilityTests.cs)
- [Tests: ServiceStack.Extensions.Tests/FileUploadSecurityTests.cs](../../../tests/ServiceStack.Extensions.Tests/FileUploadSecurityTests.cs)
- [Tests: ServiceStack.WebHost.Endpoints.Tests/FileUploadTests.cs](../../../tests/ServiceStack.WebHost.Endpoints.Tests/FileUploadTests.cs)
- [Public docs: virtual-file-system.md](../../../../../docs.servicestack.net/MyApp/_pages/virtual-file-system.md)

## CORE-005: Optional replayable notification feeds for Server Events

- Recommendation: Later, after a concrete notification workflow.
- Potential value: Medium for disconnected dashboards and user notifications; live invalidation users may not need it.
- Relative effort: Large; durable storage, replay ordering and client changes.
- Confidence: High distinction between current live subscription IDs and a durable feed cursor; demand unvalidated.
- Customer validation needed: confirm a representative application/workflow and its current workaround before acceptance.

### Problem and existing behavior

Reconnecting an SSE client restores the live connection but does not itself recover notifications missed during an outage. Applications must build their own durable notification log and catch-up query.

`ServerEventsFeature` has subscription lifecycle hooks, authentication gates, heartbeats and throttling. `EventSubscription` increments a message ID for that subscription. `RedisServerEvents` distributes notifications through its live pub/sub backplane. The inspected handler does not consume `Last-Event-ID` to replay a retained event log; per-connection IDs must not be mistaken for a cross-reconnect durable cursor. AI.Chat's separate canonical-history/update system is not a general Server Events replay provider.

### Proposed outcome

A separate opt-in replay feed contract for application-selected notifications, with retention, authorized catch-up and an explicit cursor-too-old response. Keep ordinary Server Events lightweight.

### Bounded implementation approach

1. Define a durable event identity scoped to a feed and authorization audience; support bounded range reads and retention metadata through an optional store.
2. Close the race between replay and live delivery using a high-watermark or equivalent protocol. Deduplicate by durable event identity and document ordering per feed.
3. Recheck current authorization on reconnect. Return a resync-required state when a cursor expired, instead of pretending every missed event can be replayed.
4. Keep connection-control/heartbeat events outside durable application feeds. Core owns protocol/client behavior; a future Server adapter can own RDBMS storage. Do not change existing numeric client EventId semantics silently.

### Acceptance criteria if selected

- Publish during replay, disconnect between events and reconnect on another node without silent gaps within the retained range.
- Revoked channel access blocks historical content; expired cursors require a full refresh.
- Bound catch-up pages and slow-client memory. Existing live-only clients remain wire compatible.
- Validate with actual browser EventSource behavior and the .NET SSE client; no claim of exactly-once UI processing.

### Alternatives and value check

For cache invalidation and dashboards, reconnect-and-refresh from an authoritative API is often enough. Prefer that recipe unless customers need retained individual events. A dedicated notification service is another valid application-level solution.

### Compatibility, ownership and risks

A cursor is not authorization, SSE is not a durable broker, and Redis pub/sub alone is not history. Channel membership changes, retention and user targeting affect replay semantics. This should follow demand validation rather than compete with higher-confidence improvements.

### Evidence

- [ServiceStack/ServerEventsFeature.cs](../ServerEventsFeature.cs)
- [ServiceStack.Server/RedisServerEvents.cs](../../ServiceStack.Server/RedisServerEvents.cs)
- [ServiceStack.Client/ServerEventsClient.cs](../../ServiceStack.Client/ServerEventsClient.cs)
- [Public docs: server-events.md](../../../../../docs.servicestack.net/MyApp/_pages/server-events.md)
- [Public docs: redis-server-events.md](../../../../../docs.servicestack.net/MyApp/_pages/redis-server-events.md)

## CORE-003: Correlated operational view across requests and background work

Disposition: **closed as substantially covered by existing implementation**, not accepted or newly implemented by this analysis. Keep this ID for the original proposal's history.

The first pass underestimated existing functionality. OperationDiagnostics already exposes operation spans/metrics and optional detailed phases. MessagingDiagnostics propagates W3C context, JobsDiagnostics continues queue traces, and ProfilingFeature captures trace/span ancestry and an ExternalTraceUrlTemplate. The Admin Profiling component already has Trace Id filtering, a trace view and an external-backend link. OpenTelemetryTests/AppHostTests assert propagation, no-listener behavior, metric outcomes, remote parents and safe trace links.

Do not prioritize another generic tracing/dashboard project from this record. A future measured gap (for example a missing provider-specific span) should receive a new narrowly scoped proposal with an end-to-end failing scenario. The existing docs explicitly state that this package does not add OrmLite/Redis provider spans; that is a boundary, not a reason to duplicate platform instrumentation without checking existing provider integrations.

Evidence:

- [ServiceStack/Telemetry/README.md](../Telemetry/README.md)
- [ServiceStack/Telemetry/OperationDiagnostics.cs](../Telemetry/OperationDiagnostics.cs)
- [ServiceStack.Client/Messaging/MessagingDiagnostics.cs](../../ServiceStack.Client/Messaging/MessagingDiagnostics.cs)
- [ServiceStack/Jobs/JobsDiagnostics.cs](../Jobs/JobsDiagnostics.cs)
- [ServiceStack/ProfilingFeature.cs](../ProfilingFeature.cs)
- [ServiceStack/modules/admin-ui/components/Profiling.mjs](../modules/admin-ui/components/Profiling.mjs)
- [Tests: ServiceStack.Extensions.Tests/OpenTelemetryTests.cs](../../../tests/ServiceStack.Extensions.Tests/OpenTelemetryTests.cs)
- [Tests: ServiceStack.Extensions.Tests/OpenTelemetryAppHostTests.cs](../../../tests/ServiceStack.Extensions.Tests/OpenTelemetryAppHostTests.cs)

## Related work and deliberate exclusions

- [SERVER-002/003/001: data enumeration, transaction-bound jobs and durable delivery](../../ServiceStack.Server/plan/FEATURE-PROPOSALS.md).
- [CHAT-004: typed durable agent workflows](../../ServiceStack.AI.Chat/plan/FEATURE-PROPOSALS.md).
- Removed the first-pass IMAGE-001 planning link: its owning project no longer has active planning documents. No deprecated project is assigned future work here.

## External evidence and interpretation

The [HTML SSE standard](https://html.spec.whatwg.org/multipage/server-sent-events.html#the-last-event-id-header) defines reconnect cursor transport via Last-Event-ID. It does not provide server history storage; CORE-005's retention/replay guarantees would be additional ServiceStack behavior. Source checked 2026-10-31.

## Decision handling

Accept by stable ID with an agreed outcome and scope. Move that complete record to [FEATURE-IMPROVEMENTS.md](FEATURE-IMPROVEMENTS.md), record the decision/date and validation criteria, and leave a short link here. Closed/deferred proposals stay here with their reason. Implementation, successful validation and confirmed release are separate later states. This analysis changes no accepted-improvement record.
