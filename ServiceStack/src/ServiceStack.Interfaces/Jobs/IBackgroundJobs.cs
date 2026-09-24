 #nullable enable

using System;
using System.Data;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Jobs;

/// <summary>
/// Provides methods for managing background jobs, including enqueueing, running, canceling,
/// and monitoring jobs.
/// </summary>
public interface IBackgroundJobs
{
    /// <summary>
    /// Enqueues an API request as a background job.
    /// </summary>
    BackgroundJobRef EnqueueApi(object requestDto, BackgroundJobOptions? options = null);
    /// <summary>
    /// Enqueues a command as a background job.
    /// </summary>
    BackgroundJobRef EnqueueCommand(string commandName, object arg, BackgroundJobOptions? options = null);
    /// <summary>
    /// Executes a transient (i.e. non-durable) command and returns immediately with a Reference
    /// to the Executing Job
    /// </summary>
    BackgroundJob RunCommand(string commandName, object arg, BackgroundJobOptions? options = null);
    /// <summary>
    /// Executes a transient (i.e. non-durable) command that waits until the command is executed
    /// and returns the command result if any
    /// </summary>
    Task<object?> RunCommandAsync(string commandName, object arg, BackgroundJobOptions? options = null);
    /// <summary>
    /// Used by Background Workers to execute a Job 
    /// </summary>
    Task ExecuteJobAsync(BackgroundJob job);
    /// <summary>
    /// Cancels a running job by its Id, returns true if a running job was cancelled
    /// </summary>
    bool CancelJob(long jobId);
    /// <summary>
    /// Cancel all Jobs with the specified state or the specified worker
    /// </summary>
    /// <returns>How many jobs were cancelled</returns>
    List<long> CancelJobs(BackgroundJobState? state = null, string? worker = null);
    /// <summary>
    /// Cancels a named Background Worker and transfers any pending queues to a new worker
    /// </summary>
    void CancelWorker(string worker);
    /// <summary>
    /// Requeues a failed job.
    /// </summary>
    void RequeueFailedJob(long jobId);
    /// <summary>
    /// Marks a job as failed due to an exception.
    /// </summary>
    void FailJob(BackgroundJob job, Exception ex);
    /// <summary>
    /// Marks a job as failed that can optionally not be retried
    /// </summary>
    void FailJob(BackgroundJob job, ResponseStatus error, bool shouldRetry);
    /// <summary>
    /// Marks a job as completed and transfers it from Job Queue to CompletedJob table in Monthly DB
    /// </summary>
    void CompleteJob(BackgroundJob job, object? response = null);
    /// <summary>
    /// Update a running jobs status
    /// </summary>
    /// <param name="status"></param>
    void UpdateJobStatus(BackgroundJobStatusUpdate status);
    /// <summary>
    /// Run Startup tasks to populate Job Queue with incomplete tasks 
    /// </summary>
    Task StartAsync(CancellationToken stoppingToken);
    /// <summary>
    /// Stops dispatching new Jobs and waits for running Jobs to finish, releasing any Jobs that
    /// never started so another node can run them immediately
    /// </summary>
    Task StopAsync(CancellationToken token = default);
    /// <summary>
    /// Runs monitoring and periodic tasks 
    /// </summary>
    Task TickAsync();
    /// <summary>
    /// Get all named workers with their active queue counts 
    /// </summary>
    Dictionary<string, int> GetWorkerQueueCounts();
    /// <summary>
    /// Get execution stats of all named workers
    /// </summary>
    List<WorkerStats> GetWorkerStats();
    /// <summary>
    /// Returns an open ADO .NET Connection to the jobs.db
    /// </summary>
    IDbConnection OpenDb();
    /// <summary>
    /// Returns an open ADO .NET Connection to the monthly jobs.db indicated by CreatedDate
    /// </summary>
    IDbConnection OpenMonthDb(DateTime createdDate);
    /// <summary>
    /// Retrieves a job by its Job Id
    /// </summary>
    JobResult? GetJob(long jobId);
    /// <summary>
    /// Retrieves a job by its unique Ref Id
    /// </summary>
    JobResult? GetJobByRefId(string refId);
    /// <summary>
    /// Rehydrates the Request DTO from a persisted Background Job
    /// </summary>
    object CreateRequest(BackgroundJobBase job);
    /// <summary>
    /// Rehydrates the Response from a persisted Background Job
    /// </summary>
    object? CreateResponse(BackgroundJobBase job);
    /// <summary>
    /// Schedules a recurring API task.
    /// </summary>
    void RecurringApi(string taskName, Schedule schedule, object requestDto, BackgroundJobOptions? options = null);
    /// <summary>
    /// Schedules a recurring command task.
    /// </summary>
    void RecurringCommand(string taskName, Schedule schedule, string commandName, object arg, BackgroundJobOptions? options = null);
    /// <summary>
    /// Deletes a recurring task.
    /// </summary>
    void DeleteRecurringTask(string taskName);
    /// <summary>
    /// Returns the estimated duration of a Command Job in milliseconds
    /// </summary>
    int? GetCommandEstimatedDurationMs(string commandType, string? worker=null);
    /// <summary>
    /// Returns the estimated duration of an API Job in milliseconds
    /// </summary>
    int? GetApiEstimatedDurationMs(string requestType, string? worker=null);
    
    /// <summary>
    /// Get collection of loaded Scheduled Tasks
    /// </summary>
    public ICollection<ScheduledTask> ScheduledTasks { get; }
}

/// <summary>Optional Job Batch and Queue controls implemented by the built-in job providers.</summary>
public interface IBackgroundJobsQueues
{
    /// <summary>Returns the progress of a Job Batch, or null if the batch doesn't exist</summary>
    JobBatch? GetJobBatch(string batchId);
    /// <summary>
    /// Creates or updates a Job Batch, letting a caller record the expected Total and a Callback
    /// command to run once every Job in the batch has finished.
    /// </summary>
    JobBatch CreateJobBatch(JobBatch batch);
    /// <summary>Returns the runtime controls of every known Job Queue</summary>
    List<JobQueue> GetJobQueues();
    /// <summary>
    /// Pauses, resumes or re-throttles a Job Queue. Takes effect on every node.
    /// A rateLimit of 0 removes the queue's rate limit.
    /// </summary>
    JobQueue SetJobQueue(string name, bool? paused = null, int? concurrency = null, string? modifiedBy = null,
        int? rateLimit = null, int? rateLimitSecs = null);
    /// <summary>Whether Jobs on this queue are currently being dispatched</summary>
    bool IsQueuePaused(string queue);
    /// <summary>Max concurrent Jobs for this queue, including any runtime override</summary>
    int GetQueueConcurrency(string queue);
    /// <summary>Returns the App Servers processing Jobs and when they were last seen</summary>
    List<JobNode> GetJobNodes();
    /// <summary>
    /// Stops an App Server taking new Jobs while it finishes the ones it has, e.g. before a deploy.
    /// Returns false if the server isn't known.
    /// </summary>
    bool SetJobNodeDraining(string serverId, bool draining);
    /// <summary>
    /// Cancels every active Job in a batch and prevents any more being added to it.
    /// Returns the Ids of the Jobs that were cancelled.
    /// </summary>
    List<long> CancelJobBatch(string batchId);
    /// <summary>Returns the failed attempts of a Job, oldest first</summary>
    List<JobAttempt> GetJobAttempts(long jobId);
    /// <summary>Returns a point-in-time view of the Job backlog</summary>
    JobsStatus GetJobsStatus();
}

/// <summary>
/// Queues Jobs as part of the caller's own database transaction (the "transactional outbox"), so a
/// Job is only queued if the work that queued it commits. The connection must be to the Jobs
/// database, and the transaction opened with OrmLite's OpenTransaction(). Jobs queued this way are
/// dispatched by the next tick rather than immediately, since they can't run until it commits.
/// </summary>
public interface IBackgroundJobsTransactional
{
    BackgroundJobRef EnqueueApi(IDbConnection db, object requestDto, BackgroundJobOptions? options = null);
    BackgroundJobRef EnqueueCommand(IDbConnection db, string commandName, object arg, BackgroundJobOptions? options = null);
}

/// <summary>Optional recurring-task controls implemented by the built-in job providers.</summary>
public interface IBackgroundJobsScheduler
{
    /// <summary>Pauses or resumes a recurring task. Resumed tasks become eligible immediately.</summary>
    bool SetRecurringTaskEnabled(long taskId, bool enabled);
    /// <summary>Makes a recurring task due immediately without affecting its ongoing schedule.</summary>
    bool RunRecurringTaskNow(long taskId);
    /// <summary>Reloads Scheduled Tasks from the database, picking up changes made by other nodes.</summary>
    void ReloadScheduledTasks();
}

/// <summary>
/// Reference of a Queued Job 
/// </summary>
public class BackgroundJobRef(long id, string refId)
{
    /// <summary>Id of the queued Job</summary>
    public long Id { get; } = id;
    /// <summary>Unique user-specified or system generated GUID of the queued Job</summary>
    public string RefId { get; } = refId;
    public void Deconstruct(out long id, out string refId)
    {
        id = this.Id;
        refId = this.RefId;
    }
}

/// <summary>
/// Status Update of a Job
/// </summary>
public struct BackgroundJobStatusUpdate(BackgroundJob job, double? progress=null, string? status=null, string? log=null)
{
    /// <summary>The Job being updated</summary>
    public BackgroundJob Job { get; } = job;
    /// <summary>Progress of the Job from 0-1</summary>
    public double? Progress { get; } = progress;
    /// <summary>Status message to report, e.g. Downloaded 2/10</summary>
    public string? Status { get; } = status;
    /// <summary>Log message to append to the Job's Logs</summary>
    public string? Log { get; } = log;

    public void Deconstruct(out BackgroundJob job, out double? progress, out string? status, out string? log)
    {
        job = this.Job;
        progress = this.Progress;
        status = this.Status;
        log = this.Log;
    }
}

/// <summary>
/// Customize Queued Job Options
/// </summary>
public class BackgroundJobOptions
{
    /// <summary>
    /// Specify a user-defined UUID for the Job
    /// </summary>
    public string? RefId { get; set; }
    /// <summary>Return the existing job reference when this RefId has already been submitted.</summary>
    public DuplicateRefIdBehavior DuplicateRefIdBehavior { get; set; }
    /// <summary>
    /// Only allow a single Job with this key to be queued or running at a time. When a Job with this
    /// key is already active the existing Job is returned instead of queueing a duplicate.
    /// </summary>
    public string? SingletonKey { get; set; }
    /// <summary>
    /// Maintain a Reference to a parent Job
    /// </summary>
    public long? ParentId { get; set; }
    /// <summary>
    /// Named Worker Thread to execute Job on  
    /// </summary>
    public string? Worker { get; set; }
    /// <summary>Logical queue used to separate classes of work.</summary>
    public string? Queue { get; set; }
    /// <summary>Higher values are dispatched first within a queue.</summary>
    public int? Priority { get; set; }
    /// <summary>
    /// Only run Job after date
    /// </summary>
    public DateTime? RunAfter { get; set; }
    /// <summary>
    /// Don't run the Job after this date, it's cancelled with the JobExpired error code instead
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    /// <summary>
    /// Don't run the Job if it hasn't started within this period of being queued
    /// </summary>
    public TimeSpan? ExpiresIn { get; set; }
    /// <summary>
    /// Command to Execute after successful completion of Job
    /// </summary>
    public string? Callback { get; set; }
    /// <summary>
    /// Only execute job after successful completion of Parent Job
    /// </summary>
    public long? DependsOn { get; set; }
    /// <summary>
    /// Only execute this Job after every Job in this Batch has finished
    /// </summary>
    public string? DependsOnBatch { get; set; }
    /// <summary>
    /// Whether the Job only runs when the Job it DependsOn completes (default), or once it has
    /// finished in any state
    /// </summary>
    public JobDependencyPolicy? DependsOnPolicy { get; set; }
    /// <summary>
    /// Jobs sharing a ConcurrencyKey run one at a time, while different keys still run in parallel
    /// </summary>
    public string? ConcurrencyKey { get; set; }
    /// <summary>The tenant the Job belongs to, for multi-tenant filtering and reporting</summary>
    public string? TenantId { get; set; }
    /// <summary>
    /// The ASP .NET Identity Auth User Id to populate the IRequest Context ClaimsPrincipal and User Session
    /// </summary>
    public string? UserId { get; set; }
    /// <summary>
    /// How many times to attempt to retry Job on failure, default 2 (BackgroundsJobFeature.DefaultRetryLimit)
    /// </summary>
    public virtual int? RetryLimit { get; set; }
    /// <summary>How the delay between retries grows, defaults to the feature's DefaultRetryBackoff</summary>
    public RetryBackoff? RetryBackoff { get; set; }
    /// <summary>Base delay before the first retry in milliseconds, defaults to the feature's DefaultRetryDelayMs</summary>
    public int? RetryDelayMs { get; set; }
    /// <summary>The longest delay between retries in milliseconds, defaults to the feature's DefaultMaxRetryDelayMs</summary>
    public int? MaxRetryDelayMs { get; set; }
    /// <summary>Base delay before the first retry, an alternative to RetryDelayMs</summary>
    public TimeSpan? RetryDelay
    {
        get => RetryDelayMs.HasValue ? TimeSpan.FromMilliseconds(RetryDelayMs.Value) : null;
        set => RetryDelayMs = value.HasValue ? checked((int)value.Value.TotalMilliseconds) : null;
    }
    /// <summary>The longest delay between retries, an alternative to MaxRetryDelayMs</summary>
    public TimeSpan? MaxRetryDelay
    {
        get => MaxRetryDelayMs.HasValue ? TimeSpan.FromMilliseconds(MaxRetryDelayMs.Value) : null;
        set => MaxRetryDelayMs = value.HasValue ? checked((int)value.Value.TotalMilliseconds) : null;
    }
    /// <summary>
    /// Maintain a reference to a callback URL
    /// </summary>
    public string? ReplyTo { get; set; }
    /// <summary>
    /// Associate Job with a tag group
    /// </summary>
    public string? Tag { get; set; }
    /// <summary>Add the Job to this JobBatch, which is created if it doesn't exist</summary>
    public virtual string? BatchId { get; set; }
    /// <summary>The user or process queueing the Job</summary>
    public string? CreatedBy { get; set; }
    /// <summary>Cancel the Job if it runs longer than this, defaults to the feature's DefaultTimeoutSecs</summary>
    public int? TimeoutSecs { get; set; }
    /// <summary>Cancel the Job if it runs longer than this, an alternative to TimeoutSecs</summary>
    public TimeSpan? Timeout
    {
        get => TimeoutSecs.HasValue ? TimeSpan.FromSeconds(TimeoutSecs.Value) : null;
        set => TimeoutSecs = value.HasValue ? (int)value.Value.TotalSeconds : null;
    }
    
    /// <summary>Additional user-defined arguments for the Job</summary>
    public Dictionary<string, string>? Args { get; set; }

    /// <summary>
    /// Whether command should be run and not persisted
    /// </summary>
    public bool? RunCommand { get; set; }
    /// <summary>Callback invoked with the Response when the Job completes, only available in the process that queued it</summary>
    [IgnoreDataMember]
    public Action<object?>? OnSuccess { get; set; }
    /// <summary>Callback invoked with the Exception when the Job fails, only available in the process that queued it</summary>
    [IgnoreDataMember]
    public Action<Exception>? OnFailed { get; set; }
    /// <summary>Cancellation Token to cancel the Job with, only available in the process that queued it</summary>
    [IgnoreDataMember]
    public CancellationToken? Token { get; set; } 
}

/// <summary>
/// Captures Stats of a Background Job Worker
/// </summary>
public class WorkerStats
{
    /// <summary>Name of the Worker</summary>
    public string Name { get; set; } = null!;
    /// <summary>Jobs waiting to run on the Worker</summary>
    public long Queued { get; set; }
    /// <summary>Jobs the Worker has received</summary>
    public long Received { get; set; }
    /// <summary>Jobs the Worker has executed</summary>
    public long Completed { get; set; }
    /// <summary>Jobs the Worker has executed that were retries of a previous attempt</summary>
    public long Retries { get; set; }
    /// <summary>Jobs that threw an unhandled exception on the Worker</summary>
    public long Failed { get; set; }
    /// <summary>Id of the Job the Worker is currently executing</summary>
    public long? RunningJob { get; set; }
    /// <summary>How long the Worker has been executing its current Job</summary>
    public TimeSpan? RunningTime { get; set; }
}
