using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

public class QueueCheckUrl : IPost, IReturn<QueueCheckUrlResponse>
{
    [ValidateNotEmpty]
    public required string Url { get; set; }
    [ApiMember(Description = "Optional queue to run this Job on")]
    public string? Queue { get; set; }
    
    [ApiMember(Description = "Specify a user-defined UUID for the Job")]
    public string? RefId { get; set; }
    [ApiMember(Description = "Maintain a Reference to a parent Job")] 
    public long? ParentId { get; set; }
    [ApiMember(Description = "Named Worker Thread to execute Job on")]
    public string? Worker { get; set; }
    [ApiMember(Description = "Only run Job after date")]
    public DateTime? RunAfter { get; set; }
    [ApiMember(Description = "Command to Execute after successful completion of Job")]
    public string? Callback { get; set; }
    [ApiMember(Description = "Only execute job after successful completion of Parent Job")]
    public long? DependsOn { get; set; }
    [ApiMember(Description = "The ASP .NET Identity Auth User Id to populate the IRequest Context ClaimsPrincipal and User Session")]
    public string? UserId { get; set; }
    [ApiMember(Description = "How many times to attempt to retry Job on failure, default 2")]
    public virtual int? RetryLimit { get; set; }
    [ApiMember(Description = "Maintain a reference to a callback URL")]
    public string? ReplyTo { get; set; }
    [ApiMember(Description = "Associate Job with a tag group")]
    public string? Tag { get; set; }
    public virtual string? BatchId { get; set; }
    public string? CreatedBy { get; set; }
    public int? TimeoutSecs { get; set; }
}

public class QueueCheckUrlResponse
{
    public long Id { get; set; }
    public string RefId { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

/*
 * Background Jobs examples. Each API queues Jobs that demonstrate a popular Background MQ use-case,
 * which you can then watch run in the Admin UI at /admin-ui/backgroundjobs.
 * The work itself is simulated, the Commands are in ServiceInterface/BackgroundJobCommands.cs.
 */

/// <summary>A Job queued by an example API</summary>
public class QueuedJob
{
    public long Id { get; set; }
    public string RefId { get; set; } = null!;
    /// <summary>What the Job does, e.g. which step of a workflow it is</summary>
    public string Name { get; set; } = null!;
}

public class QueueJobsResponse
{
    public List<QueuedJob> Jobs { get; set; } = [];
    public string? BatchId { get; set; }
    public string? Message { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

[Tag("Background Jobs")]
[Description("Fire-and-forget: send a welcome email, retrying with exponential backoff if the email provider fails")]
public class QueueWelcomeEmail : IPost, IReturn<QueueJobsResponse>
{
    [ValidateNotEmpty]
    public string Email { get; set; } = "new.user@example.org";
    [ApiMember(Description = "Simulate the email provider failing this many times before it succeeds")]
    public int FailAttempts { get; set; } = 2;
    [ApiMember(Description = "How many times to retry before the Job is recorded as Failed")]
    public int RetryLimit { get; set; } = 3;
}

[Tag("Background Jobs")]
[Description("Long-running Job that reports progress, status and logs. Cancel it from the Admin UI while it runs")]
public class QueueImportProducts : IPost, IReturn<QueueJobsResponse>
{
    public int Products { get; set; } = 50;
    [ApiMember(Description = "Simulated time to import each product")]
    public int MsPerProduct { get; set; } = 500;
    [ApiMember(Description = "Cancel the import if it runs longer than this")]
    public int TimeoutSecs { get; set; } = 120;
}

[Tag("Background Jobs")]
[Description("Delayed Job: generate a report later. It's cancelled with JobExpired if it can't start before ExpiresInSecs")]
public class QueueScheduledReport : IPost, IReturn<QueueJobsResponse>
{
    public string ReportName { get; set; } = "Monthly Sales";
    [ApiMember(Description = "Run the Job this many seconds from now")]
    public int DelaySecs { get; set; } = 30;
    [ApiMember(Description = "Don't run the Job if it hasn't started this many seconds after it was queued. Set lower than DelaySecs to see it expire")]
    public int? ExpiresInSecs { get; set; }
    [ApiMember(Description = "POST the report result to the JobResultWebhook API when it completes")]
    public bool UseWebhook { get; set; } = true;
}

[Tag("Background Jobs")]
[Description("Workflow: charge payment, then reserve inventory, then ship, each only after the previous step succeeded")]
public class QueueOrderFulfillment : IPost, IReturn<QueueJobsResponse>
{
    public int OrderId { get; set; } = 1001;
    public decimal Amount { get; set; } = 99.95m;
    [ApiMember(Description = "Simulate a step failing: charge, reserve or ship. The steps after it are cancelled, while the customer is still notified")]
    [Input(Type = "select", EvalAllowableValues = "['','charge','reserve','ship']")]
    public string? FailStep { get; set; }
}

[Tag("Background Jobs")]
[Description("Fan-out and fan-in: resize many images in parallel as a Batch, then zip them once every image is done")]
public class QueueResizeImages : IPost, IReturn<QueueJobsResponse>
{
    public int Images { get; set; } = 12;
    [ApiMember(Description = "Simulate this many of the images failing, which skips the Batch's OnSuccess callback")]
    public int FailImages { get; set; } = 0;
}

[Tag("Background Jobs")]
[Description("Per-tenant ordering: each tenant's sync Jobs run one at a time, while different tenants run in parallel")]
public class QueueTenantSync : IPost, IReturn<QueueJobsResponse>
{
    public List<string> Tenants { get; set; } = ["acme", "globex", "initech"];
    public int JobsPerTenant { get; set; } = 3;
}

[Tag("Background Jobs")]
[Description("De-duplicated Jobs: queue a cache refresh only if one isn't already queued or running, and charge an order at most once")]
public class QueueDeduplicatedJobs : IPost, IReturn<QueueJobsResponse>
{
    public string CacheName { get; set; } = "products";
    public int OrderId { get; set; } = 2001;
}

[Tag("Background Jobs")]
[Description("Rate limited third-party API: only RateLimit calls start per WindowSecs across every server, high priority calls first")]
public class QueueExternalApiCalls : IPost, IReturn<QueueJobsResponse>
{
    public int Calls { get; set; } = 10;
    public int RateLimit { get; set; } = 2;
    public int WindowSecs { get; set; } = 5;
}

[Tag("Background Jobs")]
[Description("Request/Response over a queue: queue a Job then wait for its result in the same request")]
public class RunReportAndWait : IPost, IReturn<RunReportAndWaitResponse>
{
    public string ReportName { get; set; } = "Daily Orders";
    [ApiMember(Description = "Run it as a durable Job, or as a transient in-memory Command that isn't persisted")]
    public bool Durable { get; set; } = true;
    public int TimeoutSecs { get; set; } = 30;
}
public class RunReportAndWaitResponse
{
    public long? JobId { get; set; }
    public string? ReportName { get; set; }
    public int Rows { get; set; }
    public string? Url { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

[Tag("Background Jobs")]
[Description("Recurring task: clean up temp files on a schedule until it has run MaxRuns times. See the Admin UI's Scheduled Tasks")]
public class ScheduleRecurringCleanup : IPost, IReturn<QueueJobsResponse>
{
    public int IntervalSecs { get; set; } = 30;
    public int? MaxRuns { get; set; } = 5;
    [ApiMember(Description = "Remove the recurring task instead of scheduling it")]
    public bool Delete { get; set; }
}

[Tag("Background Jobs")]
[Description("Transactional outbox: queue an order's Jobs in the same database transaction as the order, so they're only queued if it commits")]
public class QueuePlaceOrder : IPost, IReturn<QueueJobsResponse>
{
    public string Customer { get; set; } = "ALFKI";
    public decimal Amount { get; set; } = 250m;
    [ApiMember(Description = "Simulate the order failing to save, rolling back its Jobs with it")]
    public bool RollbackTransaction { get; set; }
}

/// <summary>
/// Receives Job results POSTed to a ReplyTo URL. The body is the Job's Response, with the Job's
/// details in X-Job-Id, X-Job-RefId and X-Job-State headers.
/// </summary>
[Tag("Background Jobs")]
public class JobResultWebhook : IPost, IReturnVoid
{
    public string? ReportName { get; set; }
    public int Rows { get; set; }
    public string? Url { get; set; }
}

[Tag("Background Jobs")]
[Description("Results received by the JobResultWebhook API")]
public class GetReceivedWebhooks : IGet, IReturn<GetReceivedWebhooksResponse> {}
public class GetReceivedWebhooksResponse
{
    public List<ReceivedWebhook> Results { get; set; } = [];
}
public class ReceivedWebhook
{
    public string? JobId { get; set; }
    public string? RefId { get; set; }
    public string? State { get; set; }
    public string? ReportName { get; set; }
    public int Rows { get; set; }
    public DateTime ReceivedDate { get; set; }
}
