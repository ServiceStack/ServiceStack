# Feature proposals: suite index

All entries below are unaccepted proposals. These are preliminary investigation priorities based on the inspected implementation; they are not a delivery roadmap, quantified ROI model or claim that a feature is absent everywhere in ServiceStack.

## Recommended investigation order

Start with bounded work that clarifies existing capabilities: RabbitMQ quorum configuration, job-provider conformance/deployment guidance and OpenAPI adapter conformance. Develop the API contract snapshot design in parallel conceptually, but confirm client compatibility requirements before implementation. Larger runtime additions need customer/problem validation first.

Value is potential benefit, effort is a relative design estimate, and confidence describes the evidence. None substitutes for acceptance or validated demand.

| ID | Canonical proposal | Initial disposition | Relative effort | Customer evidence |
| --- | --- | --- | --- | --- |
| CORE-001 | [API contract snapshots and compatibility reports](../ServiceStack/plan/FEATURE-PROPOSALS.md) | Investigate next | Medium–large | Not yet validated |
| JOBS-001 | [Executable provider capability matrix and deployment checks](../ServiceStack.Jobs/plan/FEATURE-PROPOSALS.md) | Investigate next | Small–medium | Not yet validated |
| OPENAPI-001 | [Shared OpenAPI integration conformance fixtures](../ServiceStack.OpenApi.Microsoft/plan/FEATURE-PROPOSALS.md) | Investigate next | Medium | Not yet validated |
| RABBIT-001 | [Documented quorum queue configuration with topology validation](../ServiceStack.RabbitMq/plan/FEATURE-PROPOSALS.md) | Investigate next | Small–medium for a recipe; medium for presets and broker integration tests | One public request; broader demand unknown |
| CHAT-001 | [Deterministic evaluation fixtures for tools and provider behavior](../ServiceStack.AI.Chat/plan/FEATURE-PROPOSALS.md) | Validate demand first | Medium–large | Not yet validated |
| CORE-002 | [Opt-in request idempotency for mutation APIs](../ServiceStack/plan/FEATURE-PROPOSALS.md) | Validate demand first | Large | Not yet validated |
| CORE-003 | [Correlated operational view across requests and background work](../ServiceStack/plan/FEATURE-PROPOSALS.md) | Validate demand first | Medium | Not yet validated |
| HTTP-001 | [Documented opt-in resilience composition for typed clients](../ServiceStack.HttpClient/plan/FEATURE-PROPOSALS.md) | Validate demand first | Small for recipes; medium if additional hooks are required | Not yet validated |
| SERVER-001 | [Durable outbound webhook delivery records and operations](../ServiceStack.Server/plan/FEATURE-PROPOSALS.md) | Validate demand first | Large | Not yet validated |
| IMAGE-001 | [Shared image output options across image providers](../ServiceStack.Skia/plan/FEATURE-PROPOSALS.md) | Later exploration | Medium | Not yet validated |

## Sibling project investigations

The [ServiceStack.Blazor review](../../../ServiceStack.Blazor/src/ServiceStack.Blazor/FEATURES.md) on 2026-11-11 records eight [project-owned proposals](../../../ServiceStack.Blazor/src/ServiceStack.Blazor/plan/FEATURE-PROPOSALS.md). Its investigation order is local to that package; it does not replace the suite priorities above or accept product work. Cross-project participation links to these canonical records:

- [BLAZOR-001: scoped UI lookup state](../../../ServiceStack.Blazor/src/ServiceStack.Blazor/plan/FEATURE-PROPOSALS.md#blazor-001-scope-and-bound-ui-lookup-state) — Blazor owns lookup/UI consumption; any generic metadata-cache change needs ServiceStack.Client ownership participation.
- [BLAZOR-003: cancellation and stale-result protection](../../../ServiceStack.Blazor/src/ServiceStack.Blazor/plan/FEATURE-PROPOSALS.md#blazor-003-cancellable-component-requests-and-stale-result-protection) — Blazor owns component lifetime and helper forwarding; core/Client participate only for transport gaps.
- [BLAZOR-004: keyboard/focus lifetimes](../../../ServiceStack.Blazor/src/ServiceStack.Blazor/plan/FEATURE-PROPOSALS.md#blazor-004-accessible-overlays-comboboxes-and-keyboard-lifetimes) — Blazor owns component integration; core ServiceStack owns the companion JS asset.
- [BLAZOR-007: render-mode initialization](../../../ServiceStack.Blazor/src/ServiceStack.Blazor/plan/FEATURE-PROPOSALS.md#blazor-007-explicit-render-mode-and-prerender-initialization-contract) — Blazor owns its component contract; core/Extensions/template owners participate in host and Identity examples.

The [ServiceStack.Aws review](../../../ServiceStack.Aws/src/ServiceStack.Aws/FEATURES.md) on 2026-11-11 records eight [project-owned proposals](../../../ServiceStack.Aws/src/ServiceStack.Aws/plan/FEATURE-PROPOSALS.md). Its first priorities are S3 traversal/deletion correctness, atomic cache conditions and cancellable retry budgets. This is a package-local investigation order, with no product work accepted. Cross-project participation links to the canonical AWS records:

- [AWS-001: safe S3 bulk operations](../../../ServiceStack.Aws/src/ServiceStack.Aws/plan/FEATURE-PROPOSALS.md#aws-001-complete-prefix-safe-cancellable-s3-bulk-operations) — AWS owns S3/R2 implementations; Common/core participate only for optional VFS contracts and core FilesUploadFeature integration.
- [AWS-004: SQS batch outcomes](../../../ServiceStack.Aws/src/ServiceStack.Aws/plan/FEATURE-PROPOSALS.md#aws-004-explicit-sqs-batch-outcomes-and-bounded-publishing) — AWS owns buffering and broker confirmation; core messaging participates only for optional shared capabilities.
- [AWS-006: typed DynamoDB conditionals/transactions](../../../ServiceStack.Aws/src/ServiceStack.Aws/plan/FEATURE-PROPOSALS.md#aws-006-typed-dynamodb-conditionals-and-transactions) — AWS owns PocoDynamo; cache/auth adoption and any shared-contract change require separate acceptance.
- [AWS-007: query pages/read-cost controls](../../../ServiceStack.Aws/src/ServiceStack.Aws/plan/FEATURE-PROPOSALS.md#aws-007-typed-query-pages-and-read-cost-controls) — AWS owns provider pages and budgets; core ServiceStack participates for optional AutoQuery Data integration.
- [AWS-008: Transcribe lifecycle/retrieval](../../../ServiceStack.Aws/src/ServiceStack.Aws/plan/FEATURE-PROPOSALS.md#aws-008-bounded-transcribe-jobs-and-correct-result-retrieval) — AWS owns the provider; shared speech/VFS owners participate only where optional cross-provider contracts are justified.

The [ServiceStack.Azure review](../../../ServiceStack.Azure/src/ServiceStack.Azure/FEATURES.md) on 2026-11-11 records eight [project-owned proposals](../../../ServiceStack.Azure/src/ServiceStack.Azure/plan/FEATURE-PROPOSALS.md). Its first priorities are Service Bus settlement, Table cache expiry safety and broker lifecycle/handler isolation; injected clients/passwordless configuration is its strongest broadly useful additive capability. This is a package-local investigation order, with no product work accepted. Cross-project participation links to the canonical Azure records:

- [AZURE-001: settlement/message context](../../../ServiceStack.Azure/src/ServiceStack.Azure/plan/FEATURE-PROPOSALS.md#azure-001-explicit-service-bus-settlement-and-message-context) — Azure owns broker settlement; core MessageHandler participates only if an optional processing-outcome capability is needed.
- [AZURE-003: client injection/provisioning](../../../ServiceStack.Azure/src/ServiceStack.Azure/plan/FEATURE-PROPOSALS.md#azure-003-injected-clients-and-passwordless-provisioning-options) — Azure owns adapter construction/resource policy; core/Extensions participate only for optional host-registration recipes.
- [AZURE-004: Blob VFS conformance/async I/O](../../../ServiceStack.Azure/src/ServiceStack.Azure/plan/FEATURE-PROPOSALS.md#azure-004-consistent-blob-vfs-and-native-async-io) — Azure owns block/append storage behavior; Common/core participate for optional VFS gaps and FilesUploadFeature validation.
- [AZURE-005: broker lifetime/handler isolation](../../../ServiceStack.Azure/src/ServiceStack.Azure/plan/FEATURE-PROPOSALS.md#azure-005-restartable-service-bus-lifetimes-and-isolated-handlers) — Azure owns processor allocation/lifetimes; generic handler changes remain core-owned.
- [AZURE-008: speech cancellation/full recordings](../../../ServiceStack.Azure/src/ServiceStack.Azure/plan/FEATURE-PROPOSALS.md#azure-008-cancellable-speech-and-complete-recording-transcription) — Azure owns its recognizer adapter; shared speech/VFS owners participate only for optional cross-provider contracts.

## Before making a release recommendation

- Finish the gap check against existing implementation, application recipes, third-party integrations and accepted plans.
- Validate customer reach and pain. Record dated evidence, alternative approaches, maintenance cost and fit with ServiceStack's DTO-first model.
- Define a bounded outcome and validation criteria; assess compatibility and migration requirements.
- Prefer improving or documenting an existing extension point when it solves the problem without a new public API.
- Reconcile the remaining sibling libraries and tools before declaring any proposal the best value across the entire suite.

See the [acceptance index](FEATURE-IMPROVEMENTS.md), [coverage report](FEATURE-COVERAGE.md) and [decision workflow](../FEATURES.md#planning-workflow). No proposal was accepted during this pass.
