#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using ServiceStack.OrmLite;
using ServiceStack.Jobs;
using DateTime = System.DateTime;

namespace ServiceStack;

public class DbJobsAdminServices(ILogger<DbJobsAdminServices> log, IBackgroundJobs jobs, IAutoQueryDb autoQuery) : Service
{
    private DatabaseJobFeature AssertRequiredRole()
    {
        var feature = AssertPlugin<DatabaseJobFeature>();
        RequiredRoleAttribute.AssertRequiredRoles(Request, feature.AccessRole);
        return feature;
    }

    public object Any(AdminJobDashboard request)
    {
        var feature = AssertRequiredRole();

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
        var dialect = db.GetDialectProvider();
        var tableDef = typeof(JobSummary).GetModelMetadata();
        var Attempts = dialect.GetQuotedColumnName(tableDef.GetFieldDefinition(nameof(JobSummary.Attempts)));
        
        var sqlRetries = $"CASE WHEN {Attempts} > 1 THEN 1 ELSE 0 END";
        // SQL Server doesn't support grouping by alias

        string groupByFields(string column)
        {
            return new[]
            {
                dialect.GetQuotedColumnName(tableDef.GetFieldDefinition(column)),
                dialect.GetQuotedColumnName(tableDef.GetFieldDefinition(nameof(JobSummary.Command))),
                dialect.GetQuotedColumnName(tableDef.GetFieldDefinition(nameof(JobSummary.State))),
                sqlRetries,
            }.Join(",");
        }
        
        to.Commands = db.SqlList<JobStat>(db.From<JobSummary>()
            .Where(x => x.Command != null && finishedStates.Contains(x.State))
            .And(dateFilter)
            .UnsafeGroupBy(groupByFields(nameof(JobSummary.Command)))
            .Select(x => new {
                Name = x.Command,
                x.State,
                Retries = Sql.Custom(sqlRetries),
                Count = Sql.Count("*")
            })
        ).ToSummaries();
        to.Apis = db.SqlList<JobStat>(db.From<JobSummary>()
            .Where(x => x.Command == null && finishedStates.Contains(x.State))
            .And(dateFilter)
            .UnsafeGroupBy(groupByFields(nameof(JobSummary.Request)))
            .Select(x => new {
                Name = x.Request,
                x.State,
                Retries = Sql.Custom(sqlRetries),
                Count = Sql.Count("*")
            })
        ).ToSummaries();
        to.Workers = db.SqlList<JobStat>(db.From<JobSummary>()
            .Where(x => x.Worker != null && finishedStates.Contains(x.State))
            .And(dateFilter)
            .UnsafeGroupBy(groupByFields(nameof(JobSummary.Worker)))
            .Select(x => new {
                Name = x.Worker,
                x.State,
                Retries = Sql.Custom(sqlRetries),
                Count = Sql.Count("*")
            })
        ).ToSummaries();

        var createdDate = dialect.GetQuotedColumnName(tableDef.GetFieldDefinition(nameof(JobSummary.CreatedDate)));
        var yesterday = DateTime.UtcNow.AddDays(-1); //Sql.Custom<DateTime>("datetime('now','-24 hours')")
        var hourCounts = db.SqlList<HourStat>(db.From<JobSummary>()
            .Where(x => x.CreatedDate >= yesterday)
            .GroupBy(x => new { x.State, x.CreatedDate })
            .OrderByDescending(x => x.CreatedDate)
            .Select(x => new {
                Hour = Sql.Custom(feature.DateFormat(createdDate, "%Y-%m-%d %H:00")),
                x.State,
                Count = Sql.Count("*"),
            })
        );

        var hourSummaries = hourCounts.ToSummaries();
        var first = hourSummaries.FirstOrDefault();

        DateTime ToDate(string hour)
        {
            var ymd = hour.LeftPart(' ').Split('-');
            var hm = hour.RightPart(' ').Split(':');
            return new DateTime(int.Parse(ymd[0]), int.Parse(ymd[1]), int.Parse(ymd[2]),
                int.Parse(hm[0]), int.Parse(hm[1]), 0);
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
            MonthDbs = feature.GetTableMonths(db)
        };

        var tables = new (string Label, System.Type Type)[] 
        {
            (nameof(BackgroundJob), typeof(BackgroundJob)),
            (nameof(JobSummary),    typeof(JobSummary)),
            (nameof(ScheduledTask), typeof(ScheduledTask)),
        };
        var totalSql = tables.Map(x => $"SELECT '{x.Label}', COUNT(*) FROM {dialect.GetQuotedTableName(x.Type.GetModelMetadata())}")
            .Join(" UNION ");
        to.TableCounts = db.Dictionary<string,int>(totalSql);

        var monthTables = new (string Label, System.Type Type)[] 
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
                Error = summary.ErrorCode != null
                    ? new() { ErrorCode = summary.ErrorCode, Message = summary.ErrorMessage }
                    : null
            };
        }

        var logs = request.LogStart != null
            ? job.Logs?[request.LogStart.Value..]
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
        var q = autoQuery.CreateQuery(request, base.Request!, db);
        return autoQuery.Execute(request, q, base.Request, db);        
    }

    public object Any(AdminQueryScheduledTasks request)
    {
        var feature = AssertRequiredRole();
        using var db = jobs.OpenDb();
        var q = autoQuery.CreateQuery(request, base.Request!, db);
        return autoQuery.Execute(request, q, base.Request, db);        
    }

    public object Any(AdminQueryCompletedJobs request)
    {
        var feature = AssertRequiredRole();
        var month = request.Month ?? DateTime.UtcNow;
        using var monthDb = jobs.OpenMonthDb(month);
        var q = autoQuery.CreateQuery(request, base.Request!, monthDb);
        return autoQuery.Execute(request, q, base.Request, monthDb);        
    }

    public object Any(AdminQueryFailedJobs request)
    {
        var feature = AssertRequiredRole();
        var month = request.Month ?? DateTime.UtcNow;
        using var monthDb = jobs.OpenMonthDb(month);
        var q = autoQuery.CreateQuery(request, base.Request!, monthDb);
        return autoQuery.Execute(request, q, base.Request, monthDb);        
    }

    public object Any(AdminRequeueFailedJobs request)
    {
        var feature = AssertRequiredRole();
        if (request.Ids == null || request.Ids.Count == 0)
            throw new ArgumentNullException(nameof(request.Ids));

        var to = new AdminRequeueFailedJobsJobsResponse();
        foreach (var jobId in request.Ids)
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
#endif
