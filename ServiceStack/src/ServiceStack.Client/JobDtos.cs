#nullable enable
using System;
using System.Collections.Generic;
using ServiceStack.DataAnnotations;
using ServiceStack.Jobs;

namespace ServiceStack;

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
    public string? Worker { get; set; }
    public BackgroundJobState? State { get; set; }
    public string? CancelWorker { get; set; }
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

public class JobStat
{
    public string Name { get; set; } 
    public BackgroundJobState State { get; set; }
    public bool Retries { get; set; }
    public int Count { get; set; }
}

public class JobStatSummary
{
    public string Name { get; set; }
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

// Logging
[ExcludeMetadata, Tag(TagNames.Admin), ExplicitAutoQuery]
public class AdminQueryRequestLogs : QueryDb<RequestLog>
{
    public DateTime? Month { get; set; }
}
