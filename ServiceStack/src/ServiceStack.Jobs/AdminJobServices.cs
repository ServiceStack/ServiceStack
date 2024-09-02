using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.NativeTypes;
using ServiceStack.OrmLite;

namespace ServiceStack.Jobs;

[ExcludeMetadata, Tag(TagNames.Jobs)]
public class AdminJobInfo : IGet, IReturn<AdminJobInfoResponse>
{
    public DateTime? Month { get; set; }
}
public class AdminJobInfoResponse
{
    public List<DateTime> MonthDbs { get; set; } = [];
    public Dictionary<string, int> TableCounts { get; set; } = [];
    public ResponseStatus? ResponseStatus { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Jobs)]
public class AdminGetJob : IGet, IReturn<AdminGetJobResponse>
{
    public long? Id { get; set; }
    public string? RefId { get; set; }
}

public class AdminGetJobResponse
{
    public JobSummary Result { get; set; } = null!;
    public BackgroundJob? Queued { get; set; }
    public CompletedJob? Completed { get; set; }
    public FailedJob? Failed { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Jobs)]
public class AdminGetJobProgress : IGet, IReturn<AdminGetJobProgressResponse>
{
    [ValidateGreaterThan(0)]
    public long Id { get; set; }
    public int? LogStart { get; set; }
}
public class AdminGetJobProgressResponse
{
    public BackgroundJobState State { get; set; }
    public double? Progress { get; set; }
    public string? Status { get; set; }
    public string? Logs { get; set; }
    public int? DurationMs { get; set; }
    public ResponseStatus? Error { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Jobs), ExplicitAutoQuery]
[NamedConnection("jobs.db")]
public class AdminQueryBackgroundJobs : QueryDb<BackgroundJob>
{
    public int? Id { get; set; }
    public string? RefId { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Jobs), ExplicitAutoQuery]
[NamedConnection("jobs.db")]
public class AdminQueryScheduledTasks : QueryDb<ScheduledTask> {}

[ExcludeMetadata, Tag(TagNames.Jobs), ExplicitAutoQuery]
[NamedConnection("jobs.db")]
public class AdminQueryJobSummary : QueryDb<JobSummary>
{
    public int? Id { get; set; }
    public string? RefId { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Jobs), ExplicitAutoQuery]
public class AdminQueryCompletedJobs : QueryDb<CompletedJob>
{
    public DateTime? Month { get; set; }
}
[ExcludeMetadata, Tag(TagNames.Jobs), ExplicitAutoQuery]
public class AdminQueryFailedJobs : QueryDb<FailedJob>
{
    public DateTime? Month { get; set; }
}


[ExcludeMetadata, Tag(TagNames.Jobs)]
public class AdminRequeueFailedJobs : IReturn<AdminRequeueFailedJobsJobsResponse>
{
    [Input(Type = "tag"), FieldCss(Field = "col-span-12")]
    public List<long>? Ids { get; set; }
}
public class AdminRequeueFailedJobsJobsResponse
{
    public List<long> Results { get; set; } = new();
    public Dictionary<long,string> Errors { get; set; } = new();
    public ResponseStatus? ResponseStatus { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Jobs)]
public class AdminCancelJobs : IGet, IReturn<AdminCancelJobsResponse>
{
    [Input(Type = "tag"), FieldCss(Field = "col-span-12")]
    public List<long>? Ids { get; set; }
}
public class AdminCancelJobsResponse
{
    public List<long> Results { get; set; } = new();
    public Dictionary<long,string> Errors { get; set; } = new();
    public ResponseStatus? ResponseStatus { get; set; }
}

[ExcludeMetadata, Tag(TagNames.Jobs)]
public class AdminJobDashboard : IGet, IReturn<AdminJobDashboardResponse>
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public record class JobStat(string Name, BackgroundJobState State, bool Retries, int Count); 

public class JobStatSummary
{
    public required string Name { get; set; }
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Retries { get; set; }
    public int Failed { get; set; }
    public int Cancelled { get; set; }
}

public class AdminJobDashboardResponse
{
    public List<JobStatSummary> Commands { get; set; } = new();
    public List<JobStatSummary> Apis { get; set; } = new();
    public List<JobStatSummary> Workers { get; set; } = new();
    public List<HourSummary> Today { get; set; } = new();
    public ResponseStatus? ResponseStatus { get; set; }
}

public class HourStat
{
    public string Hour { get; set; }
    public BackgroundJobState State { get; set; }
    public int Count { get; set; }
}

public class HourSummary
{
    public string Hour { get; set; }
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Failed { get; set; }
    public int Cancelled { get; set; }
}

public class AdminJobServices(ILogger<AdminJobServices> log, IBackgroundJobs jobs, IAutoQueryDb autoQuery) : Service
{
    private BackgroundsJobFeature AssertRequiredRole()
    {
        var feature = AssertPlugin<BackgroundsJobFeature>();
        RequiredRoleAttribute.AssertRequiredRoles(Request, feature.AccessRole);
        return feature;
    }

    public object Any(AdminJobDashboard request)
    {
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
        // 
        var dir = new DirectoryInfo(((IAppHostNetCore)HostContext.AppHost).HostingEnvironment.ContentRootPath
            .CombineWith(feature.DbDir));
        var monthDbs = dir.GetFiles()
            .Where(x => x.Name.Contains('_'));
        var to = new AdminJobInfoResponse();

        to.MonthDbs = monthDbs.Select(x => 
            DateTime.TryParse(x.Name.RightPart('_').LeftPart('.') + "-01", out var date) ? date : (DateTime?)null)
            .Where(x => x != null)
            .Select(x => x!.Value)
            .OrderDescending()
            .ToList();

        using var db = jobs.OpenDb();
        var dialect = db.GetDialectProvider();

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
        var feature = AssertRequiredRole();
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