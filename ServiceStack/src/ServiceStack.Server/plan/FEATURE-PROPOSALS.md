# ServiceStack.Server feature proposals

Deep review: 2026-10-31. Source baseline `56101ff57ab51579497684cc81ab3fd36f4c43f7`; documentation baseline `51d3ae93b07fc965093f420a425cfb921cd8166f`. Source presence is not a confirmed published-release guarantee.

All active records below are **proposed**, not accepted. Priority is an engineering recommendation within these four reviewed projects, not a quantified ROI or customer-demand claim. Effort is relative (small = local adapter/recipe; medium = bounded cross-file API; large = durable/distributed or multi-provider contract), not a delivery estimate. No runtime tests, paid provider calls or application benchmarks were run for this documentation change; cited tests were inspected as evidence of current assertions.

See [existing capabilities](../FEATURES.md), [accepted improvements](FEATURE-IMPROVEMENTS.md) and [decision workflow](../../FEATURES.md#planning-workflow). The four-project review supersedes these files' first-pass candidate descriptions; the broad [suite index](../../plan/FEATURE-PROPOSALS.md) remains a first-pass index and should be reconciled in a separate suite-wide pass.

## Review findings

Reviewed AutoQuery AppendLimits/query filters, AutoCrud transaction/after-hook and RowVersion paths, CrudEvents recording/replay, transactional DbJobs enqueue, ReplyTo delivery and related tests. Also checked Admin schema diff and query explanation to avoid reproposing them.

| Already present | Evidence from the reviewed implementation/tests | Not proposed as new |
| --- | --- | --- |
| Paging and automatic primary-key ordering | AutoQuery AppendLimits; AutoQueryTests paging cases | Basic pagination |
| Optimistic concurrency | AutoQueryCrudTests asserts OptimisticConcurrencyException and new RowVersion | Generic concurrency protection |
| Executable CRUD audit history | ICrudEvents and CrudEventsExecutor; public audit-log guide | Event replay or audit logging itself |
| Transactional job enqueue | DbJobs caller-connection overloads; commit/rollback test | A second generic outbox |
| ReplyTo result delivery | DbJobs tests assert delivery and completed-job behavior on callback failure | HTTP callbacks themselves |
| Schema migration generation and query explain | AdminSchemaDiffService and AdminQueryService | Generic schema diff/SQL explain |

DatabaseJobFeature remains a NET8_0_OR_GREATER feature in this checkout. Designs relying on it must not imply availability on every Server target framework.

## Recommended order

| ID | Candidate | Disposition | Potential value / relative effort |
| --- | --- | --- | --- |
| [SERVER-002](#server-002-opt-in-cursor-pagination-for-autoquery) | Opt-in cursor pagination for AutoQuery | Investigate next design spike | High for APIs enumerating large tables or feeds. / Medium–large; restrict supported query shapes first. |
| [SERVER-003](#server-003-transaction-safe-autocrud-job-emission) | Transaction-safe AutoCrud job emission | Investigate next recipe, then decide API | High potential: make a common data-change → durable-work workflow reliable with existing infrastructure. / Small for a supported recipe; medium for an additive integration hook. |
| [SERVER-001](#server-001-durable-outbound-webhook-delivery-records) | Durable outbound webhook delivery records | Next candidates, after one integration use case | High for SaaS integrations; not needed by every ServiceStack application. / Large; reusable substrate exists, subscriber operations still substantial. |
| [SERVER-004](#server-004-optional-field-change-audit-history-for-autocrud) | Optional field-change audit history for AutoCrud | Later, after audit-user interviews | Medium for support/audit workflows where a request log is insufficient. / Medium–large; capture correctness and storage growth matter. |

Choose one bounded outcome per candidate before implementation. Customer demand remains unvalidated unless a dated specific source is recorded; browsing the public Ideas category did not establish demand for these particular increments.

## SERVER-002: Opt-in cursor pagination for AutoQuery

- Recommendation: Investigate next design spike.
- Potential value: High for APIs enumerating large tables or feeds.
- Relative effort: Medium–large; restrict supported query shapes first.
- Confidence: High that inspected AutoQuery paging is Skip/Take; demand and performance benefit need a representative workload.
- Customer validation needed: confirm a representative application/workflow and its current workaround before acceptance.

### Problem and existing behavior

Deep offset pages become expensive and inserts/deletes can shift later pages while a client enumerates a changing result set. Developers currently need a custom query service to express consistent seek pagination conventions.

`QueryBase`/`IQuery` expose Skip, Take and ordering. `TypedQuery.AppendLimits` calls `q.Limit(dto.Skip, take)` and can add primary-key ordering when no order was provided. Registered query filters and OrmLite expressions can already implement custom seek predicates. This proposal packages a reusable opt-in contract; it does not add pagination to a framework that lacks it.

### Proposed outcome

A query capability with an opaque next-page cursor and stable seek ordering, initially for simple single-table queries with a unique non-null key.

### Bounded implementation approach

1. Add optional capability interfaces/DTOs instead of changing required members on every IQuery implementation. Preserve current QueryResponse and offset clients; decide an additive next-cursor shape.
2. Resolve allowed sort fields from metadata, append a unique tie-breaker and fetch Take+1 rows. Generate parameterized lexicographic predicates through the dialect layer.
3. Bind a versioned protected cursor to query identity, direction, sort tuple and relevant filter/security scope. Reapply all query/tenant filters on every page. Signing is integrity, not secrecy; encrypt or use server-held cursor state if keys reveal sensitive values.
4. Initially reject nullable/custom SQL sorts, grouped/aggregate queries and ambiguous joins. Expand only with explicit dialect semantics; Total remains optional and may be expensive.

### Acceptance criteria if selected

- Enumerate duplicate sort values without missing/repeating unchanged rows; test ascending/descending order and a unique tie-breaker.
- Reject tampered, expired, cross-query and cross-tenant cursors. Authorization/filter changes are reapplied.
- Benchmark first/deep pages on a realistically indexed table across SQLite, PostgreSQL, MySQL and SQL Server before claiming a speedup.
- Document that moving sort values and concurrent edits do not provide snapshot isolation. Existing Skip/Take tests keep passing.

### Alternatives and value check

Publish a recipe using RegisterQueryFilter and a custom DTO first. If most users need only primary-key enumeration, a small supported helper may deliver the value without a general cursor framework.

### Compatibility, ownership and risks

Index design determines performance. SQL null ordering, collations, composite keys and precision differ by dialect. Do not replace offsets needed for random page navigation or promise a consistent database snapshot across requests.

### Evidence

- [ServiceStack.Server/AutoQueryFeature.cs](../AutoQueryFeature.cs)
- [ServiceStack.Interfaces/IQuery.cs](../../ServiceStack.Interfaces/IQuery.cs)
- [Tests: ServiceStack.WebHost.Endpoints.Tests/AutoQueryTests.cs](../../../tests/ServiceStack.WebHost.Endpoints.Tests/AutoQueryTests.cs)
- [Public docs: autoquery/rdbms.md](../../../../../docs.servicestack.net/MyApp/_pages/autoquery/rdbms.md)

## SERVER-003: Transaction-safe AutoCrud job emission

- Recommendation: Investigate next recipe, then decide API.
- Potential value: High potential: make a common data-change → durable-work workflow reliable with existing infrastructure.
- Relative effort: Small for a supported recipe; medium for an additive integration hook.
- Confidence: High reuse potential; exact hook contract requires a prototype.
- Customer validation needed: confirm a representative application/workflow and its current workaround before acceptance.

### Problem and existing behavior

A developer sends email or starts downstream processing from an AutoCrud after-hook. If the database mutation later rolls back, the external action can still have happened; if enqueue happens after commit, a crash can lose the job.

AutoCrud already has before/after hooks and ICrudEvents. `ExecAndReturnResponse`/Async opens a transaction when recording events and no transaction exists; recording occurs before its commit. `DbJobs.EnqueueCommand(IDbConnection, ...)` and EnqueueApi use the caller's connection, and the DbJobs test explicitly covers rollback vs commit. This is already an outbox building block. A hook named OnAfter is not by itself a documented after-commit delivery guarantee.

### Proposed outcome

A tested, discoverable way to map successful AutoCrud operations to durable jobs inside the same local database transaction. Do not create a second outbox engine.

### Bounded implementation approach

1. Prototype a decorator/composition of ICrudEvents that records the audit event and enqueues using CrudContext.Db. Verify behavior with and without auditing enabled, and with a caller-owned transaction.
2. If composition is insufficient, add an explicit transaction-bound post-mutation callback; make transaction creation an opt-in rule independent of audit logging. A failure to enqueue rolls back the supported mutation.
3. Use stable event/reference identities for downstream deduplication, capture only required fields, and propagate tenant/correlation metadata.
4. Limit v1 to the same database/connection that stores jobs. Make named-connection mismatch fail clearly. Executable audit replay should not emit new jobs by default.

### Acceptance criteria if selected

- Mutation, audit record where enabled, and queued work all commit or all roll back; enqueue failure cannot leave a committed mutation.
- Handle outer transactions, nested operations, batch rollback and optimistic-concurrency failures.
- Replaying CrudEvents with IgnoreEvent does not resend email or create duplicate jobs unless explicitly opted in.
- Jobs are not dispatched before commit. An unsupported cross-database pairing fails before the mutation.

### Alternatives and value check

A documented custom service with an explicit transaction and DbJobs enqueue may be enough. Prefer the recipe if a new callback would duplicate existing composition capabilities. Do not introduce distributed transactions to support arbitrary named databases.

### Compatibility, ownership and risks

DatabaseJobFeature requires modern .NET in this checkout. Existing SQLite/local jobs cannot silently inherit relational transactional guarantees. Durable job delivery is at-least-once for business effects; downstream commands still need idempotency. Coordinate with SERVER-001 for delivery jobs, not duplicate stores.

### Evidence

- [ServiceStack.Server/AutoQueryFeature.AutoCrud.cs](../AutoQueryFeature.AutoCrud.cs)
- [ServiceStack.Server/CrudEvents.cs](../CrudEvents.cs)
- [ServiceStack.Server/DbJobs.cs](../DbJobs.cs)
- [Tests: ServiceStack.Extensions.Tests/DbJobsTests.cs](../../../tests/ServiceStack.Extensions.Tests/DbJobsTests.cs)
- [Tests: ServiceStack.Extensions.Tests/AutoQueryCrudTests.cs](../../../tests/ServiceStack.Extensions.Tests/AutoQueryCrudTests.cs)
- [Public docs: autoquery/audit-log.md](../../../../../docs.servicestack.net/MyApp/_pages/autoquery/audit-log.md)
- [Public docs: jobs/rdbms.md](../../../../../docs.servicestack.net/MyApp/_pages/jobs/rdbms.md)

## SERVER-001: Durable outbound webhook delivery records

- Recommendation: Next candidates, after one integration use case.
- Potential value: High for SaaS integrations; not needed by every ServiceStack application.
- Relative effort: Large; reusable substrate exists, subscriber operations still substantial.
- Confidence: High current ReplyTo boundary is demonstrated in tests; demand for first-party product scope unknown.
- Customer validation needed: confirm a representative application/workflow and its current workaround before acceptance.

### Problem and existing behavior

A business job completes, but posting its result to a subscriber fails. Operators need to retry the delivery without re-executing the business command and to inspect the delivery attempts independently.

`JobReplyTo.SendAsync` already posts results or publishes MQ replies; providers have an OnJobReplyTo hook and destination validation. `DbJobsTests.A_failing_ReplyTo_does_not_fail_the_Job` asserts a completed job remains completed when the callback throws. The public webhooks page describes the separate third-party ServiceStack.Webhooks integration. This proposal is about first-party durable delivery operations, not basic webhook support.

### Proposed outcome

Persist an immutable delivery intent before dispatch, retry that delivery through existing durable jobs, and expose attempt/status/redrive APIs. Do not silently change current ReplyTo semantics.

### Bounded implementation approach

1. Define event ID, delivery ID, destination reference, payload version and subscription configuration. Reuse the existing job provider for scheduling/backoff rather than building another worker loop.
2. Record every attempt, bounded response diagnostics, next-attempt time and terminal disposition. Redrive references the same logical event and records an operator action.
3. Supply an opt-in signing convention with key IDs/rotation and documented receiver deduplication. Resolve secrets from host configuration, not serialized job bodies.
4. Use SERVER-003 for same-transaction AutoCrud events where supported. For completion replies, explicitly define the crash window between business completion and delivery-intent persistence; do not imply atomicity across separate archives/connections.

### Acceptance criteria if selected

- Subscriber outage does not rerun the business command. A persisted pending delivery survives restart and exhausts a bounded retry policy predictably.
- Rollback, duplicate queueing, concurrent senders, stale workers, redrive and retention preserve event identity and diagnosable history.
- Validate destinations on enqueue and send, including redirect behavior; separate timeout and cancellation. Test signature/key rotation and redacted diagnostics.
- Existing ReplyTo behavior remains unchanged unless the durable mode is explicitly selected.

### Alternatives and value check

Use the documented third-party plugin or write a delivery command with DbJobs. Start with an example that proves which missing operations justify a library feature; integration and maintenance of an existing package may be better than a new one.

### Compatibility, ownership and risks

At-least-once delivery requires idempotent receivers. A successful HTTP response is not proof of business acceptance. Signing, destination policy, payload retention and retryable HTTP statuses need explicit contracts. No exactly-once guarantee or implicit trust of user-controlled URLs.

### Evidence

- [ServiceStack/Jobs/JobReplyTo.cs](../../ServiceStack/Jobs/JobReplyTo.cs)
- [ServiceStack.Server/BackgroundJobsProviderBase.cs](../BackgroundJobsProviderBase.cs)
- [ServiceStack.Server/DbJobs.cs](../DbJobs.cs)
- [Tests: ServiceStack.Extensions.Tests/DbJobsTests.cs](../../../tests/ServiceStack.Extensions.Tests/DbJobsTests.cs)
- [Public docs: webhooks.md](../../../../../docs.servicestack.net/MyApp/_pages/webhooks.md)
- [Public docs: jobs/workflows.md](../../../../../docs.servicestack.net/MyApp/_pages/jobs/workflows.md)

## SERVER-004: Optional field-change audit history for AutoCrud

- Recommendation: Later, after audit-user interviews.
- Potential value: Medium for support/audit workflows where a request log is insufficient.
- Relative effort: Medium–large; capture correctness and storage growth matter.
- Confidence: High baseline records request events; reliable old-value capture needs a focused prototype.
- Customer validation needed: confirm a representative application/workflow and its current workaround before acceptance.

### Problem and existing behavior

Support can see who issued a PATCH and what they requested, but answering “what did this field change from?” may require replaying history under today's service implementation or consulting application-specific audit tables.

`CrudEventsBase.ToEvent` serializes the request DTO and identity/model metadata. `CrudEventsExecutor` can replay historical requests, and the docs explicitly condition reconstruction on compatible service behavior and all relevant writes being captured. RowVersion optimistic concurrency already exists; this is not a proposal to add it. Current generic event creation does not capture authoritative before/after row images.

### Proposed outcome

Opt-in, redacted field-level changes attached to an audit event, with a schema/version marker and an entity history endpoint. Keep executable request history separate from descriptive row changes.

### Bounded implementation approach

1. Start with single-entity keyed update/patch/delete operations on selected models. Read and compare actual persisted values within a transaction with a documented isolation strategy; apply converters and database-generated values deliberately.
2. Capture only allowlisted fields with per-field redaction or omission; cap event size. Do not derive old values from the incoming DTO or assume a partial PATCH is a full row.
3. Provide a read-only diff/history projection with row version and actor information. Do not add one-click undo: restoring state must be a newly authorized mutation with concurrency checks.
4. Report uncaptured bulk writes/direct SQL/trigger changes as limitations rather than presenting the log as a complete database audit.

### Acceptance criteria if selected

- Field diffs reflect persisted changes, including defaults/nulls, converter output, soft delete and optimistic-concurrency failure.
- Rollback leaves no misleading change record; simultaneous updates cannot capture stale “before” values without an explicit limitation.
- Sensitive fields never enter event payloads, exports or Admin responses; schema evolution remains readable.
- Measure extra reads, locks and storage per audited model before enabling in a production-like workload.

### Alternatives and value check

Use database-native audit/CDC or an application-specific audit table when all writes, including direct SQL and external applications, must be observed. For simpler applications, existing request history is sufficient and should remain the default.

### Compatibility, ownership and risks

This is not complete event sourcing, guaranteed point-in-time reconstruction or compliance certification. Capturing values can increase data-retention risk and cost. Avoid expanding the proposal into a generic CDC engine or destructive restore tool.

### Evidence

- [ServiceStack.Server/CrudEvents.cs](../CrudEvents.cs)
- [ServiceStack.Server/AutoQueryFeature.AutoCrud.cs](../AutoQueryFeature.AutoCrud.cs)
- [ServiceStack.Client/AuthDtos.cs](../../ServiceStack.Client/AuthDtos.cs)
- [Tests: ServiceStack.Extensions.Tests/AutoQueryCrudTests.cs](../../../tests/ServiceStack.Extensions.Tests/AutoQueryCrudTests.cs)
- [Public docs: autoquery/audit-log.md](../../../../../docs.servicestack.net/MyApp/_pages/autoquery/audit-log.md)
- [Public docs: autoquery/crud.md](../../../../../docs.servicestack.net/MyApp/_pages/autoquery/crud.md)

## Related work and deliberate exclusions

- [CORE-002: request idempotency is a separate client retry contract](../../ServiceStack/plan/FEATURE-PROPOSALS.md).
- [JOBS-001: existing provider-capability proposal remains owned by Jobs](../../ServiceStack.Jobs/plan/FEATURE-PROPOSALS.md).
- No new proposal for job retries, queues, batches, leases, schema diff, generated migrations, SQL explain or optimistic concurrency: these already have implementation paths.

## External evidence and interpretation

[PostgreSQL's LIMIT/OFFSET documentation](https://www.postgresql.org/docs/current/queries-limit.html) explains both the need for a unique ordering and the cost of computing skipped rows. This supports investigating SERVER-002; it does not prove a particular query/index will be faster. Source checked 2026-10-31.

## Decision handling

Accept by stable ID with an agreed outcome and scope. Move that complete record to [FEATURE-IMPROVEMENTS.md](FEATURE-IMPROVEMENTS.md), record the decision/date and validation criteria, and leave a short link here. Closed/deferred proposals stay here with their reason. Implementation, successful validation and confirmed release are separate later states. This analysis changes no accepted-improvement record.
