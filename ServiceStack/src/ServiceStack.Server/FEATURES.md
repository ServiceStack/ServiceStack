# Background Jobs features

The Background Jobs improvements provide practical benefits to existing ServiceStack customers while keeping the SQLite and RDBMS providers available through the same APIs and Admin UI.

## More predictable job delivery

- **Retry backoff with jitter** spreads retries over time after a dependency or service outage. This reduces retry storms and gives failing dependencies time to recover. Jitter is applied to half the calculated delay, so a retry is never scheduled to run immediately.
- **Bounded retry delays** prevent a job from retrying either too aggressively or indefinitely far into the future.
- **Queue and priority support** lets applications separate workloads and ensure urgent jobs are handled before routine work.
- **Configurable queue concurrency** prevents a busy queue from consuming all application resources. Queues without an explicit `QueueConcurrency` entry use `MaxConcurrentJobs`, so adding a named queue doesn't quietly restrict it to one job at a time.
- **Balanced queue workers** assign each job to the least busy worker in its queue. A single slow job holds up only itself instead of every job that happened to be routed behind it while other workers sit idle.
- **Idempotent enqueue by reference ID** prevents duplicate work when clients retry a request after a timeout or when multiple processes submit the same logical job. A reference ID that's already in use by a different job now raises a typed `DuplicateRefIdException` instead of a database-specific error.
- **Singleton jobs** let a job declare a `SingletonKey` so only one job with that key can be queued or running at a time. Queueing a second one returns the job that's already active rather than duplicating the work. This is enforced by a unique database index, so it holds even when several application instances submit at the same moment.
- **Reliable worker startup** ensures a job queued at the moment a worker finishes its last job is picked up immediately, rather than waiting for the next dispatch cycle.
- **Job expiry** lets a job declare a deadline with `ExpiresAt` or `ExpiresIn`. A job still waiting when its deadline passes is cancelled with the `JobExpired` error code instead of running late, which for work like reminders and cache warm-ups is the wrong outcome anyway.
- **Queue Jobs in your own transaction.** On the RDBMS provider, `jobs.EnqueueCommand<T>(db, request)` and `jobs.EnqueueApi(db, request)` write the job with the caller's connection, inside its transaction (the "transactional outbox"). A job is only queued if the work that queued it commits, so an order and its confirmation email can't disagree. These jobs are picked up by the next tick instead of starting immediately, since they can't run before the commit.
- **Dependency policy.** `DependsOnPolicy = JobDependencyPolicy.OnFinished` runs a dependent job once its parent finishes in any state, e.g. a cleanup job that must run whether or not the work succeeded. The default, `OnSuccess`, still cancels the dependent when its parent fails, including when it was queued after its parent had already failed.
- **Tenant tagging.** `TenantId` is recorded on each job and indexed on its summary, for multi-tenant filtering and reporting.
- **Accurate estimated durations** report the *average* duration of previous runs of a command or API. The estimates shown in the Admin UI previously accumulated the total time of all prior runs, so they grew steadily as a job was used.

## Safer operations

- **Clear cancellation state** distinguishes a queued cancellation from a running cancellation request, so operators can see what happened and jobs cannot later overwrite a cancelled result.
- **Cancellation reaches the running instance.** Cancelling a running job stops it on whichever node is actually executing it, because a node that loses its lease — whether to a cancellation or to another node reclaiming the job — cancels its local execution.
- **Enforced job timeouts** stop a job that ignores its cancellation token from occupying a worker indefinitely. Once it exceeds its timeout it releases its worker, and on the RDBMS provider the job becomes available for another node to recover.
- **Dependent job cancellation** stops downstream work when its prerequisite fails or is cancelled.
- **Recovery of interrupted jobs** returns jobs left in a running state by a process that stopped mid-claim back to the queue, instead of leaving them stranded.
- **Cleanly requeued failures.** Requeueing a failed job clears its previous run state — start and completion times, attempts, duration, error, and any retry delay — and updates its summary, so the Admin UI shows a genuinely queued job rather than a mix of old and new state.
- **Archive failures are no longer silent.** If a completed or failed job can't be written to its archive, the error surfaces instead of the job being quietly dropped.
- **Bounded job logs** keep unusually verbose jobs from growing database rows without limit, while showing when output was truncated.
- **Improved status activity** keeps progress, last activity, and running state more useful for diagnosing slow or stuck jobs.
- **Queue draining when workers are replaced** reduces lost in-memory work when a worker is restarted or timed out.
- **Failed attempt history.** Every failed attempt is recorded in `JobAttempt` with its attempt number, server, start time, duration and error, available from `jobs.GetJobAttempts(id)` and the `AdminGetJobAttempts` API. Previously each retry overwrote the last error, which made intermittent failures hard to diagnose.
- **Poison jobs are failed, not retried forever.** A job abandoned mid-execution (its server crashed, or it ignored its timeout) counts as an attempt, and fails with the `LeaseExpired` error code once it exceeds its `RetryLimit`.
- **Diagnosable ownership changes.** When a status update, completion, retry, or archive is discarded because the job was cancelled or is owned by another node, it's logged with the job and lease so operators can explain what happened rather than seeing work disappear.

## Job batches

Bulk work — importing a file, fanning out notifications, reprocessing a day of records — is queued as many jobs sharing a `BatchId`. Batches make that group addressable:

- **Batch progress** counts queued, completed, failed, and cancelled jobs, and reports progress against the batch `Total` when it's known. Counters are incremented in SQL, so concurrent submitters and multiple nodes can't lose an update.
- **Completion callbacks** run a command once every job in the batch has finished, so "email the report when the import completes" doesn't need a polling loop. Only one node ever queues the callback, decided by a compare-and-set.
- **Success callbacks.** `OnSuccess` runs a command only when every job in the batch completed, alongside `Callback`, which runs however the jobs finished.
- **Batch cancellation.** `jobs.CancelJobBatch(id)` cancels every active job in a batch and records `CancelledDate`, after which no more jobs can be added to it. `ParentBatchId` groups related batches.
- **Batch visibility** shows a batch's progress on any of its jobs in the Admin UI, alongside the real state counts read back from the jobs themselves.
- **Bulk actions by batch** cancel or requeue every job in a batch in one call.

## Ordering work with concurrency keys

- **Per-key serialisation.** A job can declare a `ConcurrencyKey` so jobs sharing that key run one at a time, while different keys still run in parallel — "process each tenant's jobs in order, but all tenants at once". Named workers can only do this for a fixed, known set of names, and `SingletonKey` refuses the duplicate rather than queueing it.
- Enforced by a lock table whose primary key is the concurrency key, so two nodes can't both believe they hold it. Slots carry an expiry, so a node that dies can't block a key indefinitely.

## Fan-in on a batch

- **`DependsOnBatch`** holds a job until every job in a batch has finished, completing the workflow story alongside `DependsOn` (a single parent) and batch callbacks.

## Queue controls

Queue behaviour was previously fixed at startup. It can now be changed while the application is running, from the Admin UI or in code, and takes effect on every node:

- **Pause and resume a queue** to stop dispatching its jobs while a downstream dependency is unavailable. Paused jobs stay queued and resume where they left off — nothing is lost or failed.
- **Change queue concurrency at runtime** to throttle a noisy workload or give a backlog more capacity, without a redeploy.
- **Queue status** shows the backlog depth, jobs currently running, and how long the oldest job has been waiting, per queue.
- **Rate limiting** caps how many jobs a queue may *start* per interval, which concurrency can't express: a fast job at concurrency 1 will still burn a third-party quota. Set it with `jobs.SetJobQueueRateLimit(queue, limit, window)` or the `AdminUpdateJobQueue` API. On the RDBMS provider the count is kept in the `JobQueue` row, so the limit applies across every node rather than to each one.
- **Bounded prefetch.** On the RDBMS provider a node claims at most its queue's concurrency plus `MaxPrefetchJobs` (default 100) jobs per queue, so one node can't hoard a backlog that other nodes could be running.

## Job results delivered to ReplyTo

A job's `ReplyTo` is now acted on rather than just recorded:

- **HTTP callbacks** POST the job's result to an `http://` or `https://` `ReplyTo` URL, with `X-Job-Id`, `X-Job-RefId`, `X-Job-BatchId`, `X-Job-Tag`, and `X-Job-State` headers so the receiver can correlate the result without parsing the body.
- **MQ replies** publish the result to `ReplyTo` as a queue name when an MQ server is registered.
- **Customisable delivery** through `OnJobReplyTo`, for signing requests, adding auth headers, or routing somewhere else entirely.
- **ReplyTo validation.** `ValidateReplyTo` rejects a job whose `ReplyTo` isn't allowed when it's queued, and is checked again before the result is sent. `JobReplyTo.AllowUrlPrefixes(["https://hooks.example.org/"])` only allows URLs under those prefixes, comparing the parsed scheme, host and port so a look-alike host can't pass. Use it whenever a `ReplyTo` can come from an end user, so the server can't be used to POST to internal addresses.
- A delivery failure is logged and doesn't fail the job, which has already run successfully.

## Awaiting a job's result

- **`WaitForJobAsync`** waits for a durable job to finish and returns its result, so an API can accept a request, queue the work, and return the result on the same connection. It backs off from 100ms to 1s while polling, and accepts a timeout and cancellation token. Previously only transient commands could be awaited.

## Knowing which servers are running

- **Node registry.** Each app server records a heartbeat in `JobNode` with its machine, process, version, configured concurrency and how many jobs it's running. Operators can see how many nodes are alive and which one stopped reporting, instead of inferring it from job ownership. A clean shutdown is recorded, so a deploy doesn't look like a crash.
- **Draining a node.** `jobs.SetJobNodeDraining(serverId, true)` or the `AdminUpdateJobNode` API has a server finish the jobs it has without taking any more, e.g. before taking it out of service. `AdminGetJobNodes` lists the servers.
- **Queue affinity.** `DatabaseJobFeature.Queues` restricts a server to the queues it should process, e.g. only running GPU work on the servers that have one.
- **Health checks.** `JobsHealthCheck` reports Background Jobs to ASP.NET health checks, going Degraded or Unhealthy on backlog size, how long the oldest job has been waiting, or no live node processing a non-empty queue:

```csharp
services.AddHealthChecks()
    .AddCheck<JobsHealthCheck>("background-jobs");
```

## Protecting the database from oversized payloads

- **`MaxRequestBodyChars`** rejects a job whose serialized request exceeds the limit. Rejected rather than truncated, because a truncated request can't be deserialized — the job would fail on every attempt. The database is a work queue, not a blob store; pass a reference instead.
- **`MaxResponseBodyChars`** drops a result too large to store rather than persisting it, recording the omission in the job's `Meta` so it's clear why the result is missing. Logs were already bounded; request and response bodies no longer aren't.

## Observability

- **OpenTelemetry support** through a `ServiceStack.Jobs` `ActivitySource` and `Meter`. Each job execution starts an `Activity` tagged with its id, queue, type, attempt, batch, and worker, and counters and histograms record jobs queued, started, completed, failed, retried, and cancelled, along with execution duration and queue wait time. Nothing is allocated when no listener is configured.
- **Distributed tracing continues across the queue.** A job records the `traceparent` of whatever queued it and starts its execution Activity as a child of that trace, so an API request and the work it queued appear in one trace instead of two unrelated ones.
- **Profiling integration** records each job execution in the existing Profiling UI, showing the command, queue, batch, worker, attempt and duration alongside APIs, the gateway, OrmLite and Redis. `ProfileSource.Jobs` turns it on or off independently and is included in `ProfileSource.All`.
- **Wait times on the dashboard** report the average and longest time jobs spent waiting to start, plus how long the oldest job is still waiting right now. A backlog building up shows here before jobs start timing out.
- **Per-queue statistics** break the dashboard's totals down by queue, next to the existing command, API, and worker stats.

## Graceful shutdown

- **Jobs are drained on shutdown.** The plugin registers its own shutdown hook, so existing applications get this without changing their own jobs hosted service. Running jobs are given `ShutdownTimeoutSecs` to finish and their progress is flushed.
- **Leases are released immediately** on the RDBMS provider for jobs that were claimed but never started, and for any job still running when the shutdown budget runs out. Another node picks them up straight away instead of waiting out the lease duration on every deploy.

## History retention

- **Archive retention** optionally drops monthly `CompletedJob`/`FailedJob` archives older than `ArchiveRetention`, which summary retention alone doesn't cover.
- **Job summary retention** optionally deletes completed job history older than a configured period, so long-running applications don't accumulate summary rows indefinitely. It's off by default, and only ever removes jobs that have finished. Note that a job's reference ID becomes reusable once its summary has been deleted.

## Stronger RDBMS processing

Customers using `DatabaseJobFeature` can run multiple application instances against the same database with coordinated job ownership:

- **Leased job claims** ensure only one node owns a queued job at a time.
- **Lease heartbeats** keep long-running jobs from being mistaken for abandoned work.
- **Automatic recovery of expired claims** lets another node continue work after a process or machine failure.
- **Fenced completion and retry updates** prevent an old process from completing or modifying a job after its lease has been recovered by another node.
- **Efficient job claiming** selects only the jobs eligible to run next, in batches, and resolves job dependencies together. Applications with a large backlog no longer read every incomplete job — including its request, response, and log contents — on every polling cycle.
- **`SKIP LOCKED` claiming** on PostgreSQL and MySQL 8 lets competing nodes read disjoint sets of jobs in one query instead of racing for the same rows, so claiming keeps scaling as app servers are added. Falls back to an ordinary select where it isn't supported.
- **RDBMS capability reporting** makes these guarantees visible to the Admin UI and API consumers.

SQLite remains a lightweight local option for applications whose jobs do not require distributed ownership or failover guarantees.

`DatabaseJobFeature` also runs on SQLite, which is what its test suite uses — the lease, fencing and failover behaviour above is verified on every build rather than only against a live RDBMS.

## Durable schedules

- **Persisted next-run state** makes schedules visible and recoverable across application restarts.
- **Time-zone-aware schedules** allow jobs to run at the intended local time instead of relying on server time.
- **Misfire policies** let applications choose whether a missed occurrence should run once after recovery or be skipped.
- **Overlap policies** prevent a new occurrence from starting while the previous occurrence is still queued or running when that behavior is undesirable. This is enforced by the same unique index that backs singleton jobs, so an occurrence can't overlap the previous run even when several nodes evaluate the schedule together.
- **Corrected `Schedule.Yearly`** now runs once a year on 1 January. It previously used a monthly cron expression and ran on the first of every month.
- **Deterministic scheduled references** prevent duplicate occurrences after a crash or when multiple RDBMS nodes evaluate the same schedule.
- **Per-schedule error reporting** records invalid expressions, time-zone errors, and enqueue failures without disabling unrelated schedules.
- **Pause and resume controls** allow operators to temporarily stop a schedule without deleting its configuration.
- **Run on demand** triggers a schedule's next occurrence immediately without altering its ongoing schedule — useful for verifying a change or re-running after a failure.
- **Schedule changes reach every node.** Schedules are addressed by Id and reloaded periodically, so pausing, resuming, or triggering a task from one application instance takes effect on the others, and tasks deleted elsewhere are dropped.
- **Last run outcome** records the result and duration of the job each schedule most recently enqueued, so the schedule list shows whether the last occurrence succeeded.
- **Schedule audit dates** record when a schedule was first created and last modified.
- **Schedule bounds.** `Schedule.StartDate`, `EndDate` and `MaxRuns` limit when and how often a task runs. A task that reaches its `EndDate` or `MaxRuns` is disabled, and its `RunCount` is kept when the task is registered again on the next startup.
- **Whole-second schedule times** keep `NextRun` exact on MySQL and SQL Server, whose default `DATETIME` columns drop or round sub-second precision. Otherwise the compare-and-set that claims an occurrence could miss and queue it twice.

## Shared Admin experience

The existing Background Jobs Admin page now presents the same operational model for SQLite and RDBMS jobs:

- Provider and capability information shows which guarantees are active.
- Queue, priority, retry, run-after, expiry, cancellation, singleton key, batch, and log truncation details are visible from the job list and detail view.
- A **Queues** tab lists every queue with its backlog, running jobs, oldest waiting job, and pause/resume and concurrency controls.
- A **Replay** action queues a completed job again with the same arguments, for when a downstream system lost the result.
- Jobs in a batch show their batch's progress, and jobs can be cancelled or requeued in bulk by queue, tag, or batch.
- RDBMS lease owner, token, and expiry details are shown when supported.
- Schedule pages show enabled state, next run, time zone, misfire/overlap policies, last run result and duration, last error, and pause/resume and run-now actions.

## Upgrade experience for existing databases

- Startup upgrades add the required columns without requiring a customer-authored migration script.
- **Indexes are added to existing databases.** New indexes are normally only created with a new table, so an upgraded database would otherwise keep the old indexes and run the new queries unindexed. The upgrade creates them explicitly, using each database's supported syntax.
- Indexes that only improve performance don't prevent the application from starting if they can't be created, for example when the connection's user lacks schema permissions. The unique index behind singleton jobs is required, because a guarantee depends on it.
- The first upgrade detects the old job schema and clears the active queue as part of the agreed upgrade contract. **Queued jobs should be allowed to complete before upgrading.** Incomplete jobs remain visible in `JobSummary` with `QueueClearedOnUpgrade`; completed history is preserved.
- The upgrade is repeatable, uses database-compatible defaults, and keeps the existing `IBackgroundJobs` API source-compatible.

## Configuration examples

```csharp
services.AddPlugin(new DatabaseJobFeature {
    NamedConnection = "northwind",
    DefaultRetryBackoff = RetryBackoff.ExponentialJitter,
    DefaultRetryDelayMs = 5_000,
    DefaultMaxRetryDelayMs = 300_000,
    // Concurrency for the default queue and any queue not listed below
    MaxConcurrentJobs = 8,
    QueueConcurrency = {
        ["emails"] = 2,
        ["reports"] = 1,
    },
    MaxJobLogChars = 100_000,
    JobSummaryRetention = TimeSpan.FromDays(90),
    LeaseDurationSecs = 60,
    // Give running Jobs this long to finish when the App shuts down
    ShutdownTimeoutSecs = 30,
});
```

Keep each tenant's work in order without serialising everyone:

```csharp
jobs.EnqueueCommand<ProcessOrder>(new ProcessOrder { OrderId = orderId },
    new() { ConcurrencyKey = $"tenant:{tenantId}" });
```

Run a job only after an entire batch has finished:

```csharp
jobs.EnqueueCommand<PublishReport>(new PublishReport(),
    new() { DependsOnBatch = batchId });
```

Throttle a queue bound by a third-party quota, at runtime:

```csharp
jobs.AssertQueues().SetJobQueue("emails", concurrency:4);
// 100 sends per minute, regardless of how fast each one is
using var db = jobs.OpenDb();
db.UpdateOnly(() => new JobQueue { RateLimit = 100, RateLimitSecs = 60 },
    where: x => x.Name == "emails");
```

See which App Servers are processing Jobs:

```csharp
foreach (var node in jobs.AssertQueues().GetJobNodes())
{
    Console.WriteLine($"{node.ServerId} running {node.RunningJobs}/{node.Concurrency} " +
        $"alive:{node.IsAlive(TimeSpan.FromMinutes(1))}");
}
```

Only run one import at a time and give it a deadline:

```csharp
jobs.EnqueueCommand<ImportCatalog>(new ImportCatalog(), new() {
    SingletonKey = "import-catalog",
    ExpiresIn = TimeSpan.FromHours(1),
});
```

Queue a batch of work and run a command when all of it has finished:

```csharp
var batchId = $"import-{fileId}";
jobs.CreateJobBatch<NotifyImportComplete>(batchId, total:rows.Count, description:"Import products");

foreach (var row in rows)
{
    jobs.EnqueueCommand<ImportRow>(row, new() { BatchId = batchId, Queue = "imports" });
}

// Progress at any time
var batch = jobs.GetJobBatch(batchId);
Console.WriteLine($"{batch.Finished}/{batch.Total} ({batch.Progress:P0})");
```

Pause a queue while a dependency is down, then resume it:

```csharp
jobs.PauseJobQueue("emails");
jobs.SetJobQueueConcurrency("imports", 2);
jobs.ResumeJobQueue("emails");
```

Send a Job's result to a webhook or an MQ queue when it completes:

```csharp
jobs.EnqueueCommand<BuildReport>(new BuildReport { Id = id },
    new() { ReplyTo = "https://example.org/hooks/report-ready" });

jobs.EnqueueCommand<BuildReport>(new BuildReport { Id = id },
    new() { ReplyTo = "report.ready" }); // MQ Queue Name
```

Queue work and return its result on the same request:

```csharp
var jobRef = jobs.EnqueueCommand<BuildReport>(new BuildReport { Id = id });
var result = await jobs.WaitForJobAsync(jobRef, TimeSpan.FromSeconds(30));
return jobs.CreateResponse(result.Job);
```

Export Background Jobs metrics and traces with OpenTelemetry:

```csharp
services.AddOpenTelemetry()
    .WithTracing(x => x.AddSource(JobsDiagnostics.Name))
    .WithMetrics(x => x.AddMeter(JobsDiagnostics.Name));
```

Queue a job on a named queue, ahead of routine work:

```csharp
jobs.EnqueueCommand<SendWelcomeEmail>(new SendWelcomeEmail { UserId = userId },
    new() {
        Queue = "emails",
        Priority = 10,
        RetryDelay = TimeSpan.FromSeconds(10),
    });
```

Only ever run one import at a time — a second request returns the job already in flight:

```csharp
var jobRef = jobs.EnqueueCommand<ImportCatalog>(new ImportCatalog(),
    new() { SingletonKey = "import-catalog" });
```

Queue a job at most once per logical request, even if the client retries:

```csharp
jobs.EnqueueCommand<ProcessOrder>(new ProcessOrder { OrderId = orderId },
    new() {
        RefId = $"order-{orderId}",
        DuplicateRefIdBehavior = DuplicateRefIdBehavior.ReturnExisting,
    });
```

```csharp
jobs.RecurringCommand(
    "Nightly report",
    new Schedule("0 2 * * *") {
        TimeZoneId = "Australia/Perth",
        MisfirePolicy = ScheduleMisfirePolicy.RunOnce,
        OverlapPolicy = ScheduleOverlapPolicy.Skip,
    },
    nameof(BuildReport),
    new BuildReport());
```

Pause, resume, and trigger schedules from code, in addition to the Admin UI:

```csharp
jobs.SetRecurringTaskEnabled("Nightly report", enabled:false);
jobs.SetRecurringTaskEnabled("Nightly report", enabled:true);
jobs.RunRecurringTaskNow("Nightly report");
```
