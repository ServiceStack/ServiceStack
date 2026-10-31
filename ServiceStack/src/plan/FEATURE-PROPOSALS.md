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

## Before making a release recommendation

- Finish the gap check against existing implementation, application recipes, third-party integrations and accepted plans.
- Validate customer reach and pain. Record dated evidence, alternative approaches, maintenance cost and fit with ServiceStack's DTO-first model.
- Define a bounded outcome and validation criteria; assess compatibility and migration requirements.
- Prefer improving or documenting an existing extension point when it solves the problem without a new public API.
- Reconcile the remaining sibling libraries and tools before declaring any proposal the best value across the entire suite.

See the [acceptance index](FEATURE-IMPROVEMENTS.md), [coverage report](FEATURE-COVERAGE.md) and [decision workflow](../FEATURES.md#planning-workflow). No proposal was accepted during this pass.
