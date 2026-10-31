# ServiceStack.Jobs features

SQLite-backed background jobs, scheduling and partitioned request analytics.

First-pass source inventory: 2026-10-31. Baseline and review policy: [root catalog](../FEATURES.md).
Status: implementation entry points located in this checkout; behavioral verification is partial and published-version availability is not established. This is a capability map, not an exhaustive public API reference.

## Package and boundaries

- Declared target frameworks: `net8.0;net10.0`; see [ServiceStack.Jobs.csproj](ServiceStack.Jobs.csproj) for conditional compilation and dependencies.
- SQLite BackgroundJobs is the local provider, not a claim of distributed ownership or failover parity with DatabaseJobFeature. Shared contracts and diagnostics live in other projects.
- Project dependencies: [ServiceStack](../ServiceStack/ServiceStack.csproj); [ServiceStack.OrmLite](../../../ServiceStack.OrmLite/src/ServiceStack.OrmLite/ServiceStack.OrmLite.csproj); [ServiceStack.OrmLite.Sqlite.Data](../../../ServiceStack.OrmLite/src/ServiceStack.OrmLite.Sqlite.Data/ServiceStack.OrmLite.Sqlite.Data.csproj); [ServiceStack.Server](../ServiceStack.Server/ServiceStack.Server.csproj)

## Existing capabilities

| Capability | Customer outcome | Implementation entry points |
| --- | --- | --- |
| Durable local jobs | Queue API requests and commands with retries, cancellation, expiry and status. | [BackgroundsJobFeature.cs](BackgroundsJobFeature.cs), [BackgroundJobs.cs](BackgroundJobs.cs), [BackgroundJobsWorker.cs](BackgroundJobsWorker.cs) |
| Recurring schedules | Persist schedules and apply time zones, overlap/misfire policies and administrative controls. | [BackgroundJobs.ScheduledTasks.cs](BackgroundJobs.ScheduledTasks.cs) |
| Batches, dependencies and queues | Track grouped work and control dispatch through shared job contracts. | [BackgroundJobs.Batches.cs](BackgroundJobs.Batches.cs), [BackgroundJobs.cs](BackgroundJobs.cs) |
| Admin APIs | Inspect, cancel, requeue and monitor local jobs and schedules. | [AdminJobServices.cs](AdminJobServices.cs) |
| SQLite request logging | Store request analytics in partitioned SQLite files. | [SqliteRequestLogger.cs](SqliteRequestLogger.cs), [SqliteDataExtensions.cs](SqliteDataExtensions.cs) |

## Public documentation

These are related local documentation sources, not a claim that every provider detail is documented or current.

- [jobs/sqlite.md](../../../../docs.servicestack.net/MyApp/_pages/jobs/sqlite.md)
- [jobs/usage.md](../../../../docs.servicestack.net/MyApp/_pages/jobs/usage.md)
- [jobs/reliability.md](../../../../docs.servicestack.net/MyApp/_pages/jobs/reliability.md)
- [jobs/recurring-tasks.md](../../../../docs.servicestack.net/MyApp/_pages/jobs/recurring-tasks.md)
- [jobs/workflows.md](../../../../docs.servicestack.net/MyApp/_pages/jobs/workflows.md)
- [jobs/queues.md](../../../../docs.servicestack.net/MyApp/_pages/jobs/queues.md)
- [jobs/monitoring.md](../../../../docs.servicestack.net/MyApp/_pages/jobs/monitoring.md)
- [sqlite-request-logs.md](../../../../docs.servicestack.net/MyApp/_pages/sqlite-request-logs.md)

## Test evidence and review limits

Related test files located (not run in this documentation pass; not proof that every capability or database dialect is covered):

- [ServiceStack.Extensions.Tests/BackgroundJobsTests.cs](../../tests/ServiceStack.Extensions.Tests/BackgroundJobsTests.cs)

Follow-up: review per-capability assertions, target-framework/provider behavior, documentation examples and the published release that first supports each capability. Source links identify where to begin that review.

## Planning

- [Unaccepted feature proposals](plan/FEATURE-PROPOSALS.md)
- [Accepted improvements and implementation tracking](plan/FEATURE-IMPROVEMENTS.md)
- [Suite-wide prioritization and scope](../plan/FEATURE-PROPOSALS.md)
