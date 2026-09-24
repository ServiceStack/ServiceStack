using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using System.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.Jobs;

public class AdminJobServices(ILogger<AdminJobServices> log, IBackgroundJobs jobs, IAutoQueryDb autoQuery) : Service
{
    private BackgroundsJobFeature AssertRequiredRole()
    {
        var feature = AssertPlugin<BackgroundsJobFeature>();
        if (!string.IsNullOrEmpty(feature.AccessRole) && feature.AccessRole != RoleNames.AllowAnon)
            RequiredRoleAttribute.AssertRequiredRoles(Request, feature.AccessRole);
        return feature;
    }

    public object Any(AdminJobDashboard request)
    {
        var feature = AssertRequiredRole();
        request ??= new AdminJobDashboard();

        var to = new AdminJobDashboardResponse();
        using var db = jobs.OpenDb();
        var finishedStates = new[] { BackgroundJobState.Completed, BackgroundJobState.Failed, BackgroundJobState.Cancelled };
        Expression<Func<JobSummary,bool>> dateFilter = request is { From: not null, To: not null } 
            ? x => x.CreatedDate >= request.From && x.CreatedDate < request.To
            : request.From != null 
                ? x => x.CreatedDate >= request.From
                : request.To != null
                    ? x => x.CreatedDate < request.To
                    : x => true;
        to.Commands = db.SqlList<JobStat>(db.From<JobSummary>()
            .Where(x => x.Command != null && finishedStates.Contains(x.State))
            .And(dateFilter)
            .GroupBy(x => new { x.Command, x.State, Retries = "Retries" })
            .Select(x => new {
                Name = x.Command,
                x.State,
                Retries = Sql.Custom("IIF(Attempts>1,1,0)"),
                Count = Sql.Count("*")
            })
        ).ToSummaries();
        to.Apis = db.SqlList<JobStat>(db.From<JobSummary>()
            .Where(x => x.Command == null && finishedStates.Contains(x.State))
            .And(dateFilter)
            .GroupBy(x => new { x.Request, x.State, Retries = "Retries" })
            .Select(x => new {
                Name = x.Request,
                x.State,
                Retries = Sql.Custom("IIF(Attempts>1,1,0)"),
                Count = Sql.Count("*")
            })
        ).ToSummaries();
        to.Workers = db.SqlList<JobStat>(db.From<JobSummary>()
            .Where(x => x.Worker != null && finishedStates.Contains(x.State))
            .And(dateFilter)
            .GroupBy(x => new { x.Worker, x.State, Retries = "Retries" })
            .Select(x => new {
                Name = x.Worker,
                x.State,
                Retries = Sql.Custom("IIF(Attempts>1,1,0)"),
                Count = Sql.Count("*")
            })
        ).ToSummaries();

        to.Queues = db.SqlList<JobStat>(db.From<JobSummary>()
            .Where(x => finishedStates.Contains(x.State))
            .And(dateFilter)
            .GroupBy(x => new { x.Queue, x.State, Retries = "Retries" })
            .Select(x => new {
                Name = x.Queue,
                x.State,
                Retries = Sql.Custom("IIF(Attempts>1,1,0)"),
                Count = Sql.Count("*")
            })
        ).ToSummaries();

        to.WaitTimes = GetWaitTimes(db, dateFilter);

        var yesterday = DateTime.UtcNow.AddDays(-1); //Sql.Custom<DateTime>("datetime('now','-24 hours')")
        var hourCounts = db.SqlList<HourStat>(db.From<JobSummary>()
            .Where(x => x.CreatedDate >= yesterday)
            .GroupBy(x => new { Hour="Hour", x.State })
            .OrderByDescending(x => x.CreatedDate)
            .Select(x => new {
                Hour = Sql.Custom("strftime('%Y-%m-%d %H:00',CreatedDate)"),
                x.State,
                Count = Sql.Count("*"),
            })
        );

        var hourSummaries = hourCounts.ToSummaries();
        var first = hourSummaries.FirstOrDefault();

        DateTime ToDate(string hour)
        {
            if (string.IsNullOrEmpty(hour)) return DateTime.UtcNow;
            if (DateTime.TryParse(hour, out var dt)) return dt;
            var ymd = hour.LeftPart(' ').Split('-');
            var hm = hour.RightPart(' ').Split(':');
            if (ymd.Length >= 3 && hm.Length >= 2 &&
                int.TryParse(ymd[0], out var y) && int.TryParse(ymd[1], out var m) && int.TryParse(ymd[2], out var d) &&
                int.TryParse(hm[0], out var hr) && int.TryParse(hm[1], out var min))
            {
                return new DateTime(y, m, d, hr, min, 0);
            }
            return DateTime.UtcNow;
        }
        if (first != null)
        {
            var firstDate = ToDate(first.Hour);
            for (var i = 0; i < 24; i++)
            {
                var expected = firstDate.AddHours(-i);
                var hour = expected.ToString("yyyy-MM-dd HH:mm");
                var summary = hourSummaries.FirstOrDefault(x => x.Hour == hour)
                    ?? new HourSummary { Hour = hour };
                summary.Hour = expected.ToString("MMM dd HH:mm");
                to.Today.Add(summary);
            }
            to.Today.Reverse();
        }

        return to;
    }

    public object Any(AdminJobInfo request)
    {
        var feature = AssertRequiredRole();
        using var db = jobs.OpenDb();
        var dialect = db.GetDialectProvider();

        var to = new AdminJobInfoResponse
        {
            Provider = "sqlite",
            Capabilities = ["queues", "queue-controls", "priority", "retry-backoff", "idempotent-enqueue",
                "singleton-jobs", "job-expiry", "job-batches", "reply-to", "bounded-logs",
                "durable-schedules", "schedule-controls", "telemetry", "graceful-shutdown"],
            MonthDbs = feature.GetTableMonths(db)
        };

        var tables = new (string Label, Type Type)[] 
        {
            (nameof(BackgroundJob), typeof(BackgroundJob)),
            (nameof(JobSummary),    typeof(JobSummary)),
            (nameof(ScheduledTask), typeof(ScheduledTask)),
        };
        var totalSql = tables.Map(x => $"SELECT '{x.Label}', COUNT(*) FROM {dialect.GetQuotedTableName(x.Type.GetModelMetadata())}")
            .Join(" UNION ");
        to.TableCounts = db.Dictionary<string,int>(totalSql);

        var monthTables = new (string Label, Type Type)[] 
        {
            (nameof(CompletedJob), typeof(CompletedJob)),
            (nameof(FailedJob),    typeof(FailedJob)),
        };
        using var monthDb = jobs.OpenMonthDb(request.Month ?? DateTime.UtcNow);
        var monthCounts = monthDb.Dictionary<string, int>(monthTables
            .Map(x => $"SELECT '{x.Label}', COUNT(*) FROM {dialect.GetQuotedTableName(x.Type.GetModelMetadata())}")
            .Join(" UNION "));
        foreach (var entry in monthCounts)
        {
            to.TableCounts[entry.Key] = entry.Value;
        }

        to.WorkerStats = jobs.GetWorkerStats();
        to.QueueCounts = jobs.GetWorkerQueueCounts();

        var jobWorkerCounts = db.Select<(string? worker, int count)>(
            db.From<BackgroundJob>()
                .GroupBy(x => x.Worker)
                .Select(x => new { Worker = x.Worker, Count = Sql.Count("*") }));
        to.WorkerCounts = new Dictionary<string, int>();
        foreach (var entry in jobWorkerCounts)
        {
            to.WorkerCounts[entry.worker ?? "None"] = entry.count;
        }
        
        to.StateCounts = db.Dictionary<BackgroundJobState, int>(
            db.From<BackgroundJob>()
                .GroupBy(x => x.State)
                .Select(x => new { State = x.State, Count = Sql.Count("*") }));
        
        return to;
    }
    
    public object Any(AdminGetJob request)
    {
        var feature = AssertRequiredRole();
        if (request.Id == null && request.RefId == null)
            throw new ArgumentNullException(nameof(request.Id));

        var jobResult = request.Id != null
            ? jobs.GetJob(request.Id.Value)
            : jobs.GetJobByRefId(request.RefId!);
        
        if (jobResult == null)
            throw HttpError.NotFound("Job not found");

        return new AdminGetJobResponse
        {
            Result = jobResult.Summary,
            Queued = jobResult.Queued,
            Completed = jobResult.Completed,
            Failed = jobResult.Failed,
        };
    }

    public object Any(AdminGetJobProgress request)
    {
        var feature = AssertRequiredRole();
        using var db = jobs.OpenDb();
        var job = db.SingleById<BackgroundJob>(request.Id);
        if (job == null)
        {
            var summary = db.SingleById<JobSummary>(request.Id)
                          ?? throw HttpError.NotFound("Job does not exist");
            return new AdminGetJobProgressResponse
            {
                State = summary.State,
                LogsTruncated = summary.LogsTruncated,
                Error = summary.ErrorCode != null
                    ? new() { ErrorCode = summary.ErrorCode, Message = summary.ErrorMessage }
                    : null
            };
        }

        var logs = request.LogStart != null && job.Logs != null
            ? job.Logs[Math.Clamp(request.LogStart.Value, 0, job.Logs.Length)..]
            : job.Logs;
        var durationMs = (int)(DateTime.UtcNow - job.StartedDate.GetValueOrDefault(job.CreatedDate)).TotalMilliseconds;

        var progress = job.Progress;
        if (job.Progress is null or 0 && job.StartedDate != null)
        {
            var lastDuration = job.Command != null
                ? jobs.GetCommandEstimatedDurationMs(job.Command, job.Worker)
                : jobs.GetApiEstimatedDurationMs(job.Request, job.Worker);
            if (lastDuration is > 0)
            {
                progress = Math.Min(1.0, Math.Round(durationMs / (double)lastDuration.Value, 2));
            }
        }
        
        return new AdminGetJobProgressResponse {
            State = job.State,
            Progress = progress,
            Status = job.Status,
            Logs = logs,
            LogsTruncated = job.LogsTruncated,
            Error = job.Error,
            DurationMs = durationMs,
        };
    }
    
    public object Any(AdminQueryBackgroundJobs request)
    {
        _ = AssertRequiredRole();
        using var db = jobs.OpenDb();
        var q = autoQuery.CreateQuery(request, base.Request, db);
        var response = autoQuery.Execute(request, q, base.Request, db);
        foreach (var job in response.Results)
        {
            if (job.Progress is null or 0 && job.StartedDate != null)
            {
                var lastDuration = job.Command != null
                    ? jobs.GetCommandEstimatedDurationMs(job.Command, job.Worker)
                    : jobs.GetApiEstimatedDurationMs(job.Request, job.Worker);
                if (lastDuration is > 0)
                {
                    job.DurationMs = (int)(DateTime.UtcNow - job.StartedDate.Value).TotalMilliseconds;
                    job.Progress = Math.Min(1.0, Math.Round(job.DurationMs / (double)lastDuration.Value, 2));
                    // log.LogInformation("progress {Current} / {LastDuration} = {Progress}", 
                    //     currentMs, lastDuration.Value, job.Progress);
                }
            }
        }
        return response;
    }

    public object Any(AdminQueryJobSummary request)
    {
        var feature = AssertRequiredRole();
        using var db = jobs.OpenDb();
        var q = autoQuery.CreateQuery(request, base.Request, db);
        return autoQuery.Execute(request, q, base.Request, db);        
    }

    public object Any(AdminQueryScheduledTasks request)
    {
        var feature = AssertRequiredRole();
        using var db = jobs.OpenDb();
        var q = autoQuery.CreateQuery(request, base.Request, db);
        return autoQuery.Execute(request, q, base.Request, db);        
    }

    public object Post(AdminUpdateScheduledTask request)
    {
        AssertRequiredRole();
        using var db = jobs.OpenDb();
        if (db.SingleById<ScheduledTask>(request.Id) == null)
            throw HttpError.NotFound($"Scheduled Task '{request.Id}' does not exist");

        if (request.Enabled != null && !jobs.SetRecurringTaskEnabled(request.Id, request.Enabled.Value))
            throw HttpError.Conflict($"Could not update Scheduled Task '{request.Id}'");
        if (request.RunNow == true && !jobs.RunRecurringTaskNow(request.Id))
            throw HttpError.Conflict($"Scheduled Task '{request.Id}' is disabled");

        var result = db.SingleById<ScheduledTask>(request.Id)
            ?? throw HttpError.NotFound($"Scheduled Task '{request.Id}' does not exist");
        return new AdminUpdateScheduledTaskResponse { Result = result };
    }



    /// <summary>
    /// How long Jobs waited between being queued and starting. This is what shows a backlog
    /// building up before Jobs start failing or timing out.
    /// </summary>
    private JobWaitTimes GetWaitTimes(IDbConnection db, Expression<Func<JobSummary,bool>> dateFilter)
    {
        var to = new JobWaitTimes();
        try
        {
            var waitMs = GetWaitMsSql(db);
            var q = db.From<JobSummary>()
                .Where(x => x.StartedDate != null)
                .And(dateFilter);
            to.Count = db.Scalar<int>(q.Clone().Select("COUNT(*)"));
            if (to.Count > 0)
            {
                to.AvgMs = (int)db.Scalar<double>(q.Clone().Select($"COALESCE(AVG({waitMs}),0)"));
                to.MaxMs = (int)db.Scalar<double>(q.Clone().Select($"COALESCE(MAX({waitMs}),0)"));
            }

            // The longest a Job is still waiting right now, which no completed Job can show
            var now = DateTime.UtcNow;
            var oldestWaiting = db.Scalar<DateTime?>(db.From<BackgroundJob>()
                .Where(x => x.State == BackgroundJobState.Queued && x.CompletedDate == null)
                .Select(x => Sql.Min(x.CreatedDate)));
            if (oldestWaiting != null && oldestWaiting < now)
                to.WaitingMs = (int)(now - oldestWaiting.Value).TotalMilliseconds;
        }
        catch (Exception e)
        {
            log.LogError(e, "Could not calculate Job wait times");
        }
        return to;
    }

    /// <summary>SQL for the milliseconds a Job waited between being created and started</summary>
    public static string GetWaitMsSql(IDbConnection db)
    {
        var dialect = db.GetDialectProvider();
        var started = dialect.GetQuotedColumnName(nameof(JobSummary.StartedDate));
        var created = dialect.GetQuotedColumnName(nameof(JobSummary.CreatedDate));
        return $"(julianday({started}) - julianday({created})) * 86400000";
    }

    public object Any(AdminGetJobBatch request)
    {
        AssertRequiredRole();
        using var db = jobs.OpenDb();
        var to = new AdminGetJobBatchResponse {
            Result = db.SingleById<JobBatch>(request.BatchId),
        };
        // Counted from the Jobs themselves so the Admin UI shows the real state of the batch even
        // if a counter update was lost
        var stateCounts = db.SqlList<JobStat>(db.From<JobSummary>()
            .Where(x => x.BatchId == request.BatchId)
            .GroupBy(x => x.State)
            .Select(x => new {
                Name = Sql.Custom("''"),
                x.State,
                Retries = Sql.Custom("0"),
                Count = Sql.Count("*"),
            }));
        foreach (var stat in stateCounts)
        {
            to.StateCounts[stat.State.ToString()] = stat.Count;
        }
        return to;
    }

    public object Any(AdminGetJobQueues request)
    {
        AssertRequiredRole();
        var now = DateTime.UtcNow;
        var queues = jobs.GetJobQueues();
        var workerQueueCounts = jobs.GetWorkerQueueCounts();
        var runningJobs = jobs.GetWorkerStats().Count(x => x.RunningJob != null);

        using var db = jobs.OpenDb();
        var to = new AdminGetJobQueuesResponse();
        foreach (var queue in queues)
        {
            var name = queue.Name;
            var pending = db.Select(db.From<BackgroundJob>()
                .Where(x => x.Queue == name && x.CompletedDate == null)
                .Select(x => new { x.State, x.CreatedDate, x.RunAfter }));
            var waiting = pending.Where(x => x.State == BackgroundJobState.Queued).ToList();
            var oldest = waiting.Count > 0
                ? waiting.Min(x => x.RunAfter ?? x.CreatedDate)
                : (DateTime?)null;

            to.Results.Add(new JobQueueStatus {
                Name = name,
                Paused = queue.Paused,
                Concurrency = jobs.GetQueueConcurrency(name),
                ConcurrencyOverridden = queue.Concurrency is > 0,
                RateLimit = queue.RateLimit,
                RateLimitSecs = queue.RateLimitSecs,
                Queued = waiting.Count,
                Running = pending.Count(x => x.State == BackgroundJobState.Started),
                OldestQueued = oldest != null && oldest < now ? now - oldest : null,
                ModifiedDate = queue.ModifiedDate,
                ModifiedBy = queue.ModifiedBy,
            });
        }
        return to;
    }

    public object Post(AdminUpdateJobQueue request)
    {
        AssertRequiredRole();
        var result = jobs.AssertQueues().SetJobQueue(request.Name, request.Paused, request.Concurrency,
            modifiedBy: Request.GetSession()?.UserName ?? Request.GetSession()?.UserAuthId,
            rateLimit: request.RateLimit, rateLimitSecs: request.RateLimitSecs);
        return new AdminUpdateJobQueueResponse { Result = result };
    }

    public object Any(AdminGetJobNodes request)
    {
        AssertRequiredRole();
        return new AdminGetJobNodesResponse { Results = jobs.AssertQueues().GetJobNodes() };
    }

    public object Post(AdminUpdateJobNode request)
    {
        AssertRequiredRole();
        var queues = jobs.AssertQueues();
        if (request.Draining != null && !queues.SetJobNodeDraining(request.ServerId, request.Draining.Value))
            throw HttpError.NotFound($"Job Node '{request.ServerId}' does not exist");
        return new AdminUpdateJobNodeResponse {
            Result = queues.GetJobNodes().FirstOrDefault(x => x.ServerId == request.ServerId)
                ?? throw HttpError.NotFound($"Job Node '{request.ServerId}' does not exist"),
        };
    }

    public object Any(AdminGetJobAttempts request)
    {
        AssertRequiredRole();
        return new AdminGetJobAttemptsResponse { Results = jobs.AssertQueues().GetJobAttempts(request.Id) };
    }

    public object Post(AdminReplayJob request)
    {
        AssertRequiredRole();
        var jobResult = jobs.GetJob(request.Id)
            ?? throw HttpError.NotFound($"Job {request.Id} does not exist");
        var job = jobResult.Job
            ?? throw HttpError.NotFound($"Job {request.Id} has no recorded request to replay");

        var options = new BackgroundJobOptions {
            Worker = job.Worker,
            Queue = job.Queue,
            Priority = job.Priority,
            ConcurrencyKey = job.ConcurrencyKey,
            TenantId = job.TenantId,
            Tag = job.Tag,
            BatchId = job.BatchId,
            Callback = job.Callback,
            ReplyTo = job.ReplyTo,
            UserId = job.UserId,
            RetryLimit = job.RetryLimit,
            RetryBackoff = job.RetryBackoff,
            RetryDelayMs = job.RetryDelayMs,
            MaxRetryDelayMs = job.MaxRetryDelayMs,
            TimeoutSecs = job.TimeoutSecs,
            Args = job.Args,
            CreatedBy = Request.GetSession()?.UserName ?? job.CreatedBy,
        };

        var replayRequest = jobs.CreateRequest(job);
        var jobRef = job.RequestType == CommandResult.Command
            ? jobs.EnqueueCommand(job.Command!, replayRequest, options)
            : jobs.EnqueueApi(replayRequest, options);

        return new AdminReplayJobResponse { JobId = jobRef.Id, RefId = jobRef.RefId };
    }

    public object Any(AdminQueryCompletedJobs request)
    {
        var feature = AssertRequiredRole();
        var month = request.Month ?? DateTime.UtcNow;
        using var monthDb = jobs.OpenMonthDb(month);
        var q = autoQuery.CreateQuery(request, base.Request, monthDb);
        return autoQuery.Execute(request, q, base.Request, monthDb);        
    }

    public object Any(AdminQueryFailedJobs request)
    {
        var feature = AssertRequiredRole();
        var month = request.Month ?? DateTime.UtcNow;
        using var monthDb = jobs.OpenMonthDb(month);
        var q = autoQuery.CreateQuery(request, base.Request, monthDb);
        return autoQuery.Execute(request, q, base.Request, monthDb);        
    }

    public object Any(AdminRequeueFailedJobs request)
    {
        var feature = AssertRequiredRole();
        var jobIds = new List<long>(request.Ids.Safe());
        if (request.Tag != null || request.BatchId != null)
        {
            // Matched against JobSummary rather than the monthly archives: every Job has a summary,
            // whereas the months reported by GetTableMonths() are derived from completed Jobs, so a
            // month containing only failures wouldn't be searched at all.
            using var db = jobs.OpenDb();
            var q = db.From<JobSummary>()
                .Where(x => x.State == BackgroundJobState.Failed);
            if (request.Tag != null)
                q.And(x => x.Tag == request.Tag);
            if (request.BatchId != null)
                q.And(x => x.BatchId == request.BatchId);
            if (request.From != null)
                q.And(x => x.CreatedDate >= request.From);
            jobIds.AddRange(db.Column<long>(q.Select(x => x.Id)));
        }
        if (jobIds.Count == 0)
            throw new ArgumentNullException(nameof(request.Ids),
                "Specify the Ids, Tag or BatchId of the failed Jobs to requeue");

        var to = new AdminRequeueFailedJobsJobsResponse();
        foreach (var jobId in jobIds.Distinct())
        {
            try
            {
                jobs.RequeueFailedJob(jobId);
            }
            catch (Exception e)
            {
                to.Errors[jobId] = e.Message;
            }
        }
        return to;
    }

    public object Any(AdminCancelJobs request)
    {
        var feature = AssertRequiredRole();
        var to = new AdminCancelJobsResponse();
        foreach (var jobId in request.Ids.Safe())
        {
            var jobResult = jobs.GetJob(jobId);
            if (jobResult?.Queued != null)
            {
                jobs.CancelJob(jobId);
                to.Results.Add(jobId);
            }
            else
            {
                to.Errors[jobId] = jobResult == null
                    ? "Job not found"
                    : "Can only cancel incomplete jobs";
            }
        }
        if (request.Worker != null || request.State != null)
        {
            to.Results.AddRange(jobs.CancelJobs(request.State, request.Worker));
        }
        if (request.BatchId != null && request.Queue == null && request.Tag == null)
        {
            // Cancelling a whole batch also stops more Jobs being added to it
            foreach (var jobId in jobs.CancelJobBatch(request.BatchId))
            {
                if (!to.Results.Contains(jobId))
                    to.Results.Add(jobId);
            }
        }
        else if (request.Queue != null || request.Tag != null || request.BatchId != null)
        {
            using var db = jobs.OpenDb();
            var q = db.From<BackgroundJob>().Where(x => x.CompletedDate == null);
            if (request.Queue != null)
                q.And(x => x.Queue == request.Queue);
            if (request.Tag != null)
                q.And(x => x.Tag == request.Tag);
            if (request.BatchId != null)
                q.And(x => x.BatchId == request.BatchId);
            foreach (var jobId in db.Column<long>(q.Select(x => x.Id)))
            {
                if (to.Results.Contains(jobId))
                    continue;
                if (jobs.CancelJob(jobId))
                    to.Results.Add(jobId);
            }
        }
        if (request.CancelWorker != null)
        {
            jobs.CancelWorker(request.CancelWorker);
        }
        return to;
    }
}

public static class AdminJobServiceExtensions
{
    public static List<JobStatSummary> ToSummaries(this List<JobStat> jobStats)
    {
        if (jobStats == null) return [];
        var map = new Dictionary<string, JobStatSummary>();
        foreach (var stat in jobStats)
        {
            var summary = map.GetOrAdd(stat.Name, name => new JobStatSummary { Name = stat.Name });
            summary.Total += stat.Count;
            if (stat.Retries)
                summary.Retries++;
            switch (stat.State)
            {
                case BackgroundJobState.Completed:
                    summary.Completed += stat.Count;
                    break;
                case BackgroundJobState.Failed:
                    summary.Failed += stat.Count;
                    break;
                case BackgroundJobState.Cancelled:
                    summary.Cancelled += stat.Count;
                    break;
            }
        }

        var to = new List<JobStatSummary>();
        foreach (var summary in map.Values.OrderByDescending(x => x.Total))
        {
            to.Add(summary);
        }
        return to;
    }

    public static List<HourSummary> ToSummaries(this List<HourStat> hourStats)
    {
        if (hourStats == null) return [];
        var map = new Dictionary<string, HourSummary>();
        foreach (var stat in hourStats)
        {
            var summary = map.GetOrAdd(stat.Hour, name => new HourSummary { Hour = stat.Hour });
            summary.Total += stat.Count;
            switch (stat.State)
            {
                case BackgroundJobState.Completed:
                    summary.Completed += stat.Count;
                    break;
                case BackgroundJobState.Failed:
                    summary.Failed += stat.Count;
                    break;
                case BackgroundJobState.Cancelled:
                    summary.Cancelled += stat.Count;
                    break;
            }
        }
        var to = new List<HourSummary>();
        foreach (var summary in map.Values.OrderByDescending(x => x.Total))
        {
            to.Add(summary);
        }
        return to;
    }
}
