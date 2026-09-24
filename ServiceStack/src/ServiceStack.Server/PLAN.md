# Background Jobs improvements

This document records the Background Jobs improvements currently present in the unstaged working tree. The changes apply the same job contract and Admin UI/API to the local SQLite provider and the RDBMS provider. The RDBMS provider adds stronger guarantees through database leases and compare-and-set updates.

## Customer-facing improvements

- **Safer retries:** retry policies support fixed, linear, exponential, and exponential-jitter backoff, with configurable base and maximum delays. Jitter prevents many servers from retrying together after an outage, and never schedules a retry for immediately.
- **Queue control:** jobs carry a queue and priority. Default and named queues have bounded concurrency, and higher-priority jobs are selected first. Each dispatch picks the least busy worker in the queue, so one slow job can't block the jobs behind it while sibling workers sit idle.
- **Idempotent enqueue:** callers can request an existing job by `RefId`; duplicate requests validate the original payload and return the existing job instead of creating duplicate work.
- **Singleton jobs:** a job can carry a `SingletonKey` so only one job with that key is queued or running at a time, enforced by a unique index rather than by a check. Recurring tasks with `OverlapPolicy.Skip` use it, so an occurrence can't overlap the previous run even across nodes.
- **Reliable RDBMS processing:** jobs are claimed with an owner, token, and expiry. Heartbeats renew active claims, another node can recover an expired claim, and completion/retry/archive writes are fenced by the current lease token.
- **Clear cancellation:** queued jobs are cancelled immediately; running jobs receive a cancellation request and their completion cannot overwrite the cancelled state. A running job is cancelled on whichever node is executing it, because a node that loses its lease (to a cancellation or to another node) cancels its local execution. Dependent jobs are cancelled with their parent.
- **Enforced timeouts:** a job that ignores its cancellation token stops having its lease renewed once it exceeds its timeout, so it releases its worker and another node can recover it.
- **Bounded diagnostics:** job logs have a configurable character limit and expose whether truncation occurred. Status updates also refresh activity and leases. Writes that are fenced off (a lost lease, a cancelled job, a completion this node no longer owns) are logged rather than dropped silently.
- **Durable schedules:** schedules persist enabled state, next run, time zone, misfire policy, overlap policy, and the last scheduling error. Scheduled occurrences use deterministic reference IDs to avoid duplicate jobs after crashes or across RDBMS nodes.
- **Schedule operations:** schedules can be paused, resumed and run on demand through the shared API/Admin UI, addressed by Id so they can be controlled from any node. Nodes reload schedules periodically, so a change made on one node reaches the others. Each schedule records the outcome of its last run. Invalid schedules are reported per task without preventing other schedules from running.
- **Shared operations UI:** SQLite and RDBMS jobs expose the same queue, priority, retry, cancellation, log, schedule, and capability information. RDBMS-only lease details are shown when supported.
- **Retention:** `JobSummary` rows can be given a retention period, after which completed history is purged.
- **Concurrency keys:** Jobs sharing a `ConcurrencyKey` execute one at a time while different keys run in parallel, enforced by a `JobConcurrencyLock` table whose primary key is the key itself. Slots expire so a node that dies can't hold a key indefinitely.
- **Fan-in:** `DependsOnBatch` holds a Job until every Job in a Batch has finished.
- **Distributed tracing:** a Job records the `traceparent` of whatever queued it and continues that trace when it executes, instead of starting an orphan root.
- **Node registry:** each App Server heartbeats into `JobNode` with its machine, process, version, concurrency and running Job count, and records a clean shutdown.
- **Health checks:** `JobsHealthCheck` reports Degraded/Unhealthy on backlog size, oldest wait time, or no live node against a non-empty queue.
- **Queue rate limits:** `JobQueue.RateLimit`/`RateLimitSecs` cap how often a queue may start Jobs, for quota-bound work. Counted per node, so the cluster ceiling is nodes x RateLimit.
- **SKIP LOCKED claiming:** used on PostgreSQL and MySQL 8 so competing nodes read disjoint Job sets, falling back to an ordinary select elsewhere.
- **Payload limits:** `MaxRequestBodyChars` rejects an oversized Request at enqueue (truncating would make it undeserializable); `MaxResponseBodyChars` drops an oversized Response and records the omission in the Job's Meta.
- **Archive retention:** `ArchiveRetention` drops monthly CompletedJob/FailedJob archives, which JobSummaryRetention doesn't cover.
- **Job batches:** jobs sharing a `BatchId` are tracked in a `JobBatch` row with SQL-incremented counters, progress against a known total, and a completion callback that exactly one node queues.
- **Queue controls:** a `JobQueue` row lets a queue be paused, resumed, or re-throttled at runtime from any node, without a redeploy. Paused queues leave their jobs queued rather than failing them.
- **Job expiry:** `ExpiresAt`/`ExpiresIn` cancel a job that was never started before its deadline with the `JobExpired` error code, rather than running it late.
- **ReplyTo delivery:** a completed job's result is posted to an http(s) `ReplyTo` URL or published to it as an MQ queue name, replaceable via `OnJobReplyTo`.
- **Awaiting durable jobs:** `WaitForJobAsync` polls with backoff until a queued job finishes and returns its result.
- **Observability:** a `ServiceStack.Jobs` `ActivitySource` and `Meter` for OpenTelemetry, plus `WriteJobBefore/After/Error` diagnostic events and a `ProfileSource.Jobs` flag.
- **Graceful shutdown:** `StopAsync` drains running jobs within `ShutdownTimeoutSecs` and releases the leases of jobs that never started, so failover is immediate instead of waiting out a lease period. Registered automatically as a hosted service so existing apps get it without code changes.

## Upgrade and compatibility contract

- Existing databases are upgraded at startup using additive column changes only, plus the indexes
  the new queries need. `CreateTableIfNotExists()` only creates indexes for new tables, so upgraded
  databases get them explicitly, guarded per dialect (`IF NOT EXISTS` on SQLite/PostgreSQL, an
  `information_schema`/`sys.indexes` check on MySQL/SQL Server).
- Indexes that only improve performance don't fail startup if they can't be created; the unique
  `SingletonKey` index does, because a guarantee depends on it. SQL Server gets a filtered index
  because it treats NULLs as equal in a unique index.
- **All queued jobs should be completed before upgrading.** The upgrade deliberately starts with an
  empty execution queue, which the Release Notes call out.
- The active execution queue is intentionally cleared on the first upgrade that detects the old schema. Incomplete jobs are preserved in `JobSummary` as cancelled with error code `QueueClearedOnUpgrade`; completed history remains available.
- The upgrade is repeatable and safe when multiple application instances start concurrently.
- New schedule columns use database-compatible defaults, including PostgreSQL boolean and text enum defaults.
- The existing `IBackgroundJobs` contract remains source-compatible. Schedule controls are exposed through an optional scheduler capability and extension method.

## Files changed

### Shared contracts and client metadata

- `ServiceStack.Interfaces/Jobs/BackgroundJob.cs` — queue, priority, retry, log, cancellation, singleton, and RDBMS lease fields; job summary fields; duplicate-reference behavior. `Queue` and `Priority` are non-nullable with database defaults, so ordering by priority can't depend on a dialect's NULL sort order. `LeaseOwner` is on the base type so the executing server is retained in the archive.
- `ServiceStack.Interfaces/Jobs/IBackgroundJobs.cs` — optional scheduling capability and shared schedule-control extensions.
- `ServiceStack.Interfaces/Jobs/ScheduledTasks.cs` — persisted schedule controls, time zone, next-run, error fields, policies, and schedule defaults.
- `ServiceStack.Client/JobDtos.cs` — shared job/schedule DTOs, options, enums, and Admin update contracts.
- `ServiceStack.Client/AuthDtos.cs` — generated/client metadata required by the Admin surface.

### SQLite job provider

- `ServiceStack.Jobs/BackgroundJobs.cs` — idempotent enqueue, retry/cancellation behavior, bounded logs, queue/priority handling, dependent-job handling, and status updates.
- `ServiceStack.Jobs/BackgroundJobs.ScheduledTasks.cs` — durable schedule fields, time-zone-aware calculation, misfire/overlap handling, deterministic scheduled references, and pause/resume.
- `ServiceStack.Jobs/BackgroundJobsWorker.cs` — priority selection, bounded workers, queue draining, and corrected asynchronous worker startup.
- `ServiceStack.Jobs/BackgroundsJobFeature.cs` — retry, log, concurrency, and queue configuration defaults.
- `ServiceStack.Jobs/AdminJobServices.cs` — shared Admin job/schedule capability data and schedule controls.

### RDBMS job provider

- `ServiceStack.Server/BackgroundJobSchema.cs` — shared additive schema upgrade, index creation, queue-clear migration behavior, archive upgrades, and concurrent startup handling.
- `ServiceStack.Server/DbJobs.cs` — leased claims, heartbeats, failover, fenced writes, retries, cancellation, bounded logs, archive handling, queue routing, and worker management. The claim query filters and limits in SQL instead of reading every incomplete row each tick, and resolves job dependencies in one batch instead of a lookup per job.
- `ServiceStack.Server/DbJobs.ScheduledTasks.cs` — persisted schedules, time zones, policies, deterministic occurrence references, schedule error isolation, and pause/resume.
- `ServiceStack.Server/DbJobsFeature.cs` — RDBMS configuration defaults for leases, claims, retries, logs, queues, and concurrency.
- `ServiceStack.Server/DbJobsProvider.cs` — startup schema integration for normal and PostgreSQL partitioned archive storage.
- `ServiceStack.Server/DbJobsWorker.cs` — leased worker state, queue routing, priority selection, and lease visibility.
- `ServiceStack.Server/DbJobsAdminServices.cs` — RDBMS provider metadata and schedule-control endpoint.

### Job batches, queue controls and observability

- `ServiceStack.Interfaces/Jobs/BackgroundJob.cs` — `JobBatch` and `JobQueue` tables, `SingletonKey`, `ExpiresAt`, and shared `JobErrorCodes`.
- `ServiceStack.Jobs/BackgroundJobs.Batches.cs` and `ServiceStack.Server/DbJobs.Batches.cs` — batch counters and completion callbacks, queue pause/concurrency controls, the expiry sweep, and ReplyTo delivery.
- `ServiceStack/Jobs/JobReplyTo.cs` — default HTTP/MQ delivery of a completed job's result.
- `ServiceStack/Jobs/JobsDiagnostics.cs` — ActivitySource, Meter, and diagnostic events.
- `ServiceStack/Jobs/JobsShutdownHostedService.cs` — drains jobs on App shutdown.

### Shared retry utilities

- `ServiceStack/Jobs/JobUtils.cs` — bounded retry delay calculation, exponential jitter, and job copying/mapping support.

### Admin UI and generated client metadata

- `ServiceStack/modules/admin-ui/components/BackgroundJobs.mjs` — provider-aware job and schedule views, queue/priority/retry/cancellation/log fields, lease fields, and pause/resume controls.
- `ServiceStack/modules/admin-ui/lib/dtos.mjs` — client DTOs and enums for the shared job/schedule contract.
- `../tests/NorthwindAuto/dtos.ts` and `../tests/NorthwindAuto/wwwroot/mjs/dtos.mjs` — generated NorthwindAuto DTO updates from the shared contract.

### Tests

- `../tests/ServiceStack.Extensions.Tests/BackgroundJobsTests.cs` — SQLite provider: retry jitter, additive upgrades, index creation, queue clearing, schedule persistence/policies, pause/resume, deterministic scheduled references, singleton uniqueness, per-dialect index SQL, dashboard wait times, and idempotent enqueue coverage.
- `../tests/ServiceStack.Extensions.Tests/DbJobsTests.cs` — 53 tests covering the RDBMS provider end
  to end, run against SQLite because `DbJobsProvider.Create()` falls through to the portable provider
  for it. Ordered by how much depends on them:
  - **Distributed guarantees:** two real `DbJobs` nodes competing over one database run each Job
    exactly once; a renewed lease can't be stolen; Jobs stranded `Started` without a lease and Jobs
    with expired leases are recovered; completion, failure, archive and status updates are all fenced
    off once a lease is lost; shutdown releases unstarted leases; incomplete Jobs resume after restart.
  - **Execution semantics:** command and API Jobs, retries and failure archiving, cancelling queued
    and *running* Jobs, Job timeouts that stop renewing a lease when the Command ignores its token,
    dependent Jobs waiting on their parent (with `ParentJob` populated), Callbacks, named Worker
    serialisation, `RunAfter` deferral, priority ordering, and transient Commands staying unpersisted.
  - **Features:** batches and their once-only callback, batch counters under concurrent submitters,
    queue pause/resume and runtime concurrency, Job expiry, ReplyTo delivery and its failure
    isolation, awaiting a durable Job, idempotent enqueue, singletons, bounded logs, schedule
    execution/controls/reload, retention purging (and never purging unfinished Jobs), and the Admin APIs.

## Bugs found by the RDBMS test suite

Adding `DbJobsTests` (which runs the RDBMS provider against SQLite, since `DbJobsProvider.Create()`
falls through to the portable provider for it) surfaced two defects that no amount of review had:

- **A write transaction was held open across a second connection's write.** `DbJobs.FailJob()`
  archived the failed Job through `OpenMonthDb()` while an explicit transaction was open on the main
  connection. For the default provider the archive lives in the *same* database, so this deadlocked
  until the command timeout. The terminal branch now claims the Job with a fenced compare-and-set
  first and does the remaining writes without wrapping them in a transaction, which is what the
  fencing already guarantees.
- **The RDBMS enqueue path bypassed the new claim gating.** `RecordAndDispatchJob()` dispatches a
  Job inline when it's immediately runnable, which skipped the `ConcurrencyKey` and `DependsOnBatch`
  checks that only ran in `DispatchPendingJobs()`. A keyed Job never took its slot and a Job waiting
  on a Batch ran straight away. Inline dispatch now defers those Jobs to the next tick.
- **Requeue by Tag/BatchId couldn't find Jobs in months with no completed Jobs.**
  `AdminRequeueFailedJobs` derived the months to search from `GetTableMonths()`, which reports
  months containing *completed* Jobs. A month whose only Jobs had failed was never searched, so
  requeueing by Tag or BatchId silently matched nothing. It now matches against `JobSummary`,
  which has a row for every Job regardless of outcome.
- **Database work ran inside a `catch when` exception filter.** The singleton/duplicate-RefId
  recovery in `RecordAndDispatchJob()` queried from an exception filter, which runs during the first
  pass of exception handling — before the failed transaction has been unwound. That deadlocks SQLite
  and would leave PostgreSQL refusing commands on an aborted transaction. Recovery now runs in the
  catch body, after the transaction is disposed. The same pattern was removed from
  `BackgroundJobSchema.AddMissingColumns()`.

## Validation completed

- Focused SQLite Background Jobs suite: **40 passed**, including schema upgrade, index creation,
  singleton uniqueness, per-dialect index SQL, retry backoff, batch progress, queue pause and
  runtime concurrency, job expiry, and awaiting a durable job.
- Full `ServiceStack.sln` build: **0 errors**.
- Both `FEATURES.md` code samples and the configuration example compile against the real API.
- NorthwindAuto configured with PostgreSQL and `NamedConnection = "northwind"`: startup completed and recurring jobs executed.
- Isolated PostgreSQL checks: **12 passed**, covering schema upgrades, repeated upgrades, history preservation, PostgreSQL defaults, competing claims, lease renewal, failover, stale completion rejection, completion/archive, retry delay, and cancellation.
- `git diff --check`: passed; only existing line-ending warnings were reported.

## Follow-up validation

Before release, run the full repository test matrix and repeat the PostgreSQL checks against each
supported PostgreSQL version and representative MySQL/SQL Server databases. The RDBMS lease contract
provides failover for ordinary queued jobs; globally serialized named workers are not advertised as a
cluster-wide mutex.

**Breaking change for the Release Notes:** `IBackgroundJobs` gained `StopAsync`, so custom
implementations of the interface need to add it. Both in-repo test doubles were updated.

Specifically still to be verified against a live RDBMS:

- The `LogsTruncated` update is now parameterised; confirm on PostgreSQL (`boolean`) and SQL Server (`bit`).
- Index creation and the `information_schema`/`sys.indexes` existence checks on MySQL and SQL Server.
- The filtered unique `SingletonKey` index on SQL Server, i.e. that many jobs without a singleton key
  can be queued at once.
- The claim query's `Take()` and predicate translation on each dialect.
- `FOR UPDATE SKIP LOCKED` against live PostgreSQL and MySQL 8. The clause ordering (after `LIMIT`)
  and the provider flags are covered by offline tests built with the MySQL dialect, but the
  statement has never been executed by a server.
- The SQL-incremented `JobBatch` counters and the wait-time SQL, which uses `JULIANDAY()` and needs
  a dialect-specific equivalent on PostgreSQL, MySQL and SQL Server.
