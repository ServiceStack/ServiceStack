using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.Host;
using ServiceStack.Messaging;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Jobs;

public partial class BackgroundJobs : BackgroundJobsProviderBase, IBackgroundJobs, IBackgroundJobsScheduler
{
    readonly ILogger<BackgroundJobs> log;
    readonly BackgroundsJobFeature feature;
    private IServiceProvider services;
    readonly IServiceScopeFactory scopeFactory;
    private ConcurrentDictionary<string, int> lastCommandDurations = new();
    private ConcurrentDictionary<string, int> lastApiDurations = new();
    ConcurrentDictionary<string, BackgroundJobsWorker> workers = new();
    private readonly ConcurrentQueue<BackgroundJobStatusUpdate> updates = new();
    string Table;
    Columns columns;
    private long ticks = 0;

    
    public BackgroundJobs(ILogger<BackgroundJobs> log, 
        BackgroundsJobFeature feature, IDbConnectionFactory dbFactory, IServiceProvider services, IServiceScopeFactory scopeFactory)
        : base(log)
    {
        // Need to store local references to these dependencies otherwise won't exist on BG Thread callbacks
        this.log = log;
        this.feature = feature;
        this.services = services;
        this.scopeFactory = scopeFactory;

        var dialect = feature.DialectProvider;
        this.Table = dialect.GetQuotedTableName(typeof(BackgroundJob));
        this.columns = new(
            Logs:dialect.GetQuotedColumnName(nameof(BackgroundJob.Logs)),
            LogsTruncated:dialect.GetQuotedColumnName(nameof(BackgroundJob.LogsTruncated)),
            Status:dialect.GetQuotedColumnName(nameof(BackgroundJob.Status)),
            Progress:dialect.GetQuotedColumnName(nameof(BackgroundJob.Progress)),
            LastActivityDate:dialect.GetQuotedColumnName(nameof(BackgroundJob.LastActivityDate)),
            Id:dialect.GetQuotedColumnName(nameof(BackgroundJob.Id)),
            Request:dialect.GetQuotedColumnName(nameof(BackgroundJob.Request)),
            Command:dialect.GetQuotedColumnName(nameof(BackgroundJob.Command)),
            Worker:dialect.GetQuotedColumnName(nameof(BackgroundJob.Worker)),
            DurationMs:dialect.GetQuotedColumnName(nameof(BackgroundJob.DurationMs))
        );
    }

    public BackgroundJobRef EnqueueApi(object requestDto, BackgroundJobOptions? options = null)
    {
        var requestType = requestDto.GetType();
        var serviceType = feature.AppHost.Metadata.GetServiceTypeByRequest(requestType);
        if (serviceType == null)
            throw new InvalidOperationException($"API for '{requestType.Name}' not found.");
        var workerAttr = requestType.FirstAttribute<WorkerAttribute>() ?? serviceType.FirstAttribute<WorkerAttribute>();;
        if (workerAttr != null)
        {
            options ??= new();
            options.Worker = workerAttr.Name;
        }
            
        var job = options.ToBackgroundJob(CommandResult.Api, requestDto);
        return RecordAndDispatchJob(job);
    }
 
    readonly ConcurrentDictionary<Type, bool> uniqueCommandTypes = new();

    public override BackgroundJobRef EnqueueCommand(string commandName, object arg, BackgroundJobOptions? options = null)
    {
        var commandInfo = AssertCommand(commandName);
        uniqueCommandTypes.TryAdd(commandInfo.Type, true);
        if (uniqueCommandTypes.Count > LicenseUtils.FreeQuotas.JobCommandTypes)
            LicenseUtils.AssertValidUsage(LicenseFeature.ServiceStack, QuotaType.Commands, uniqueCommandTypes.Count);
        
        var workerAttr = commandInfo.Type.FirstAttribute<WorkerAttribute>();
        if (workerAttr != null)
        {
            options ??= new();
            options.Worker = workerAttr.Name;
        }

        var job = options.ToBackgroundJob(CommandResult.Command, arg);
        job.Command = commandName;
        return RecordAndDispatchJob(job);
    }

    private BackgroundJobRef RecordAndDispatchJob(BackgroundJob job)
    {
        // Reject oversized payloads rather than truncating: a truncated Request can't be
        // deserialized, so the Job would be unrunnable and fail on every attempt.
        if (job.RequestBody != null && job.RequestBody.Length > feature.MaxRequestBodyChars)
            throw new ArgumentException(
                $"Job Request is {job.RequestBody.Length} chars, which exceeds the " +
                $"{nameof(feature.MaxRequestBodyChars)} limit of {feature.MaxRequestBodyChars}. " +
                $"Pass a reference to the payload instead of the payload itself.", nameof(job.RequestBody));
        AssertValidReplyTo(job);

        var existingRef = GetActiveSingletonRef(job) ?? this.GetExistingJobRef(job);
        if (existingRef != null)
            return existingRef;
        if (job.BatchId != null)
            EnsureJobBatch(job.BatchId, job.CreatedBy);
        var requestId = Guid.NewGuid().ToString("N");
        using var db = feature.OpenDb();
        var now = DateTime.UtcNow;
        // A Job that has to wait on a Batch is left for DispatchPendingJobs() to dispatch
        if ((job.RunAfter == null || now > job.RunAfter) && !IsQueuePaused(job.Queue)
            && job.DependsOnBatch == null && !IsDraining)
        {
            if (job.DependsOn != null)
            {
                var parentJob = GetFinishedParent(job, GetJob(job.DependsOn.Value));
                if (parentJob != null)
                {
                    job.ParentJob = parentJob;
                    job.RequestId = requestId;
                }
            }
            else
            {
                job.RequestId = requestId;
            }
        }
        // A rate limited queue's Jobs mustn't bypass its limit by being dispatched on enqueue
        if (job.RequestId != null && !TryTakeRateLimitSlot(job.Queue, now))
        {
            job.RequestId = null;
            job.ParentJob = null;
        }

        Exception? insertEx = null;
        try
        {
            lock (db.GetWriteLock())
            {
                using var trans = db.OpenTransaction();
                job.Id = db.Insert(job, selectIdentity: true);
                var summary = job.ToJobSummary();
                db.Insert(summary);
                if (job.BatchId != null)
                    AddJobToBatch(db, job.BatchId);
                trans.Commit();
            }
        }
        catch (Exception e)
        {
            insertEx = e;
        }

        if (insertEx != null)
        {
            // Recovery runs only after the failed transaction has been rolled back and disposed.
            // Querying from inside a `catch when` filter would run while it's still open, which
            // deadlocks SQLite and leaves PostgreSQL refusing commands on an aborted transaction.
            if (job.SingletonKey != null || job.DuplicateRefIdBehavior == DuplicateRefIdBehavior.ReturnExisting)
            {
                // Another submitter inserted a Job with the same SingletonKey or RefId after our
                // first read. The unique index is what enforces it, this just reports the winner.
                var concurrentRef = GetActiveSingletonRef(job) ?? this.GetExistingJobRef(job);
                if (concurrentRef != null)
                    return concurrentRef;
            }
            ExceptionDispatchInfo.Capture(insertEx).Throw();
        }

        JobsDiagnostics.RecordQueued(job);

        if (job.RequestId != null && TryClaimConcurrencySlot(job))
        {
            DispatchToWorker(job);
        }
        
        return new(job.Id, job.RefId!);
    }

    /// <summary>
    /// Returns the active Job already holding this Job's SingletonKey, if any. Only 1 Job per
    /// SingletonKey can exist in the active Jobs table, which a unique index enforces.
    /// </summary>
    private BackgroundJobRef? GetActiveSingletonRef(BackgroundJob job)
    {
        if (job.SingletonKey == null)
            return null;
        using var db = feature.OpenDb();
        var existing = db.Single<BackgroundJob>(x => x.SingletonKey == job.SingletonKey);
        return existing?.RefId == null
            ? null
            : new BackgroundJobRef(existing.Id, existing.RefId);
    }

    public BackgroundJob RunCommand(string commandName, object arg, BackgroundJobOptions? options = null)
    {
        var commandInfo = AssertCommand(commandName);
        var workerAttr = commandInfo.Type.FirstAttribute<WorkerAttribute>();
        if (workerAttr != null)
        {
            options ??= new();
            options.Worker = workerAttr.Name;
        }
        var job = options.ToBackgroundJob(CommandResult.Command, arg);
        job.TransientRequest = arg;
        job.RequestId = Guid.NewGuid().ToString("N");
        job.Command = commandName;
        job.Transient = true;
        
        DispatchToWorker(job);
        return job;
    }

    public Task<object?> RunCommandAsync(string commandName, object arg, BackgroundJobOptions? options = null)
    {
        var tcs = new TaskCompletionSource<object?>();
        options ??= new();
        var origOnSuccess = options?.OnSuccess;
        var origOnFailed = options?.OnFailed;
        options.OnSuccess = r =>
        {
            origOnSuccess?.Invoke(r);
            tcs.SetResult(r);
        };
        options.OnFailed = e =>
        {
            origOnFailed?.Invoke(e);
            tcs.SetException(e);
        };
        
        var job = RunCommand(commandName, arg, options);
        return tcs.Task;
    }

    public override object CreateRequest(BackgroundJobBase job)
    {
        if (job is BackgroundJob { TransientRequest: not null } b)
            return b.TransientRequest;
        
        var requestType = job.RequestType switch {
            CommandResult.Command => AssertCommand(job.Command).Request?.Type,
            CommandResult.Api => feature.AppHost.Metadata.GetRequestType(job.Request),
            _ => throw new NotSupportedException(job.RequestType)
        };
        
        requestType ??= feature.AppHost.Metadata.FindDtoType(job.Request);
        if (requestType == null)
            throw new NotSupportedException($"Request Type for '{job.Request}' not found.");
        
        var request = string.IsNullOrEmpty(job.RequestBody)
            ? requestType.CreateInstance()
            : DeserializeFromJson(job.RequestBody, requestType);
        return request;
    }

    public object CreateRequestForCommand(string command, string argType, string? argJson)
    {
        var requestType = AssertCommand(command).Request?.Type ?? feature.AppHost.Metadata.FindDtoType(argType);
        if (requestType == null)
            throw new NotSupportedException($"Request Type for '{argType}' not found.");
        var request = string.IsNullOrEmpty(argJson)
            ? requestType.CreateInstance()
            : DeserializeFromJson(argJson, requestType);
        return request;
    }

    public object CreateRequestForApi(string requestType, string? requestJson)
    {
        var type = feature.AppHost.Metadata.GetRequestType(requestType) 
                   ?? feature.AppHost.Metadata.FindDtoType(requestType);
        if (type == null)
            throw new NotSupportedException($"Request Type for '{requestType}' not found.");
        var request = string.IsNullOrEmpty(requestJson)
            ? type.CreateInstance()
            : DeserializeFromJson(requestJson, type);
        return request;
    }
    
    public override object? CreateResponse(BackgroundJobBase job)
    {
        if (job.Response == null)
            return null;
        
        var responseType = job.RequestType switch {
            CommandResult.Command => AssertCommand(job.Command).Response?.Type,
            CommandResult.Api => feature.AppHost.Metadata.FindDtoType(job.Response),
            _ => throw new NotSupportedException(job.RequestType)
        };
        
        responseType ??= JsConfig.TypeFinder(job.Response);
        if (responseType == null)
            throw new NotSupportedException($"Response Type for '{job.Response}' not found.");
        
        var response = string.IsNullOrEmpty(job.ResponseBody)
            ? responseType.CreateInstance()
            : DeserializeFromJson(job.ResponseBody, responseType);
        return response;
    }

    public int? GetCommandEstimatedDurationMs(string commandType, string? worker=null)
    {
        if (worker != null && lastCommandDurations.TryGetValue($"{commandType}.{worker}", out var lastDuration))
            return lastDuration;
        if (lastCommandDurations.TryGetValue(commandType, out lastDuration))
            return lastDuration;

        // Return best matching duration
        var prefix = commandType + ".";
        var keys = lastCommandDurations.Keys.Where(x => x == commandType || x.StartsWith(prefix)).ToList();
        foreach (var key in keys)
        {
            if (lastCommandDurations.TryGetValue(key, out lastDuration))
                return lastDuration;
        }
        return null;
    }

    public int? GetApiEstimatedDurationMs(string requestType, string? worker=null)
    {
        if (worker != null && lastApiDurations.TryGetValue($"{requestType}.{worker}", out var lastDuration))
            return lastDuration;
        if (lastApiDurations.TryGetValue(requestType, out lastDuration))
            return lastDuration;

        // Return best matching duration
        var prefix = requestType + ".";
        var keys = lastApiDurations.Keys.Where(x => x == requestType || x.StartsWith(prefix)).ToList();
        foreach (var key in keys)
        {
            if (lastApiDurations.TryGetValue(key, out lastDuration))
                return lastDuration;
        }
        return null;
    }

    object DeserializeFromJson(string json, Type type)
    {
        try
        {
            return ClientConfig.FromJson(type, json);
        }
        catch (Exception e)
        {
            log.LogWarning(e, "JOBS Failed to deserialize {Type}: {Message}\n{Json}\ntrying ServiceStack.Text...", type, e.Message,json);
            return JsonSerializer.DeserializeFromString(json, type);
        }
    }
    
    async Task<BasicRequest> CreateRequestContextAsync(IServiceScope scope, object request, BackgroundJob job, CancellationToken token)
    {
        var msg = MessageFactory.Create(request);
        var reqCtx = new BasicRequest(request)
        {
            ServiceScope = scope,
            Items = {
                [nameof(BackgroundJob)] = job,
                [nameof(CancellationToken)] = token,
            }
        };
        if (job.UserId != null)
        {
            var userResolver = scope.ServiceProvider.GetService<IUserResolver>()
                ?? services.GetRequiredService<IUserResolver>();
            var user = await userResolver.CreateClaimsPrincipalAsync(reqCtx, job.UserId, token);
            if (user == null)
                throw HttpError.NotFound("User not found");
            reqCtx.Items[Keywords.ClaimsPrincipal] = user;
            var session = await userResolver.CreateAuthSessionAsync(reqCtx, user, token);
            if (session != null)
            {
                reqCtx.SetItem(Keywords.Session, session);
            }
        }
        return reqCtx;
    }

    private readonly ConcurrentDictionary<long, CancellationTokenSource> cancellationSources = new();
    private readonly ConcurrentDictionary<long, DateTime> cancelJobIds = new();
    
    // Executed on BackgroundJobsWorker Thread
    public async Task ExecuteJobAsync(BackgroundJob job)
    {
        if (cancelJobIds.ContainsKey(job.Id))
        {
            FailJob(job, new TaskCanceledException("Job was cancelled"));
            cancelJobIds.TryRemove(job.Id, out _);
            return;
        }
        
        var executionToken = executionCts.Token;
        using var linkedCts = job.Token != null
            ? CancellationTokenSource.CreateLinkedTokenSource(executionToken, job.Token.Value)
            : CancellationTokenSource.CreateLinkedTokenSource(executionToken);
        linkedCts.CancelAfter(TimeSpan.FromSeconds(job.TimeoutSecs ?? feature.DefaultTimeoutSecs));
        cancellationSources[job.Id] = linkedCts;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var services = scope.ServiceProvider;

            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug("JOBS {Ticks}: [{RequestType} {Request} on {Worker}] Executing Job: {Id}",
                    ticks, job.Worker ?? "ANY", job.RequestType, job.Request, job.Id);
            }

            job.StartedDate = job.LastActivityDate = DateTime.UtcNow;
            job.State = BackgroundJobState.Started;

            PerformDbUpdates();

            if (!job.Transient)
            {
                using var db = feature.OpenDb();
                var claimed = db.UpdateOnly(() => new BackgroundJob
                {
                    StartedDate = job.StartedDate,
                    State = job.State,
                    LastActivityDate = job.LastActivityDate,
                }, where: x => x.Id == job.Id && x.State == BackgroundJobState.Queued && x.CancelRequestedDate == null);
                if (claimed == 0)
                {
                    log.LogWarning("JOBS Skipping Job {Id}: it was cancelled before it started", job.Id);
                    return;
                }
                var serverId = feature.ServerId;
                db.UpdateOnly(() => new JobSummary {
                    StartedDate = job.StartedDate,
                    State = job.State,
                    LeaseOwner = serverId,
                }, where: x => x.Id == job.Id);
            }

            JobsDiagnostics.RecordStarted(job);
            using var activity = JobsDiagnostics.StartActivity(job);
            var diagnosticId = JobsDiagnostics.WriteJobBefore(job);

            // Execute Command
            if (job.RequestType == null || job.Request == null)
                throw new ArgumentNullException(nameof(job.Request), "Job Request is not set");

            var request = CreateRequest(job);
            object? response = null;
            if (job.RequestType == CommandResult.Command)
            {
                var commandInfo = AssertCommand(job.Command);
                var command = (IAsyncCommand)services.GetRequiredService(commandInfo.Type);

                var reqCtx = await CreateRequestContextAsync(scope, request, job, linkedCts.Token);
                if (command is IRequiresRequest requiresRequest)
                    requiresRequest.Request = reqCtx;
                var commandResult =
                    await feature.CommandsFeature.ExecuteCommandAsync(command, reqCtx.Dto, linkedCts.Token);
                if (commandResult.Exception != null)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, commandResult.Exception.Message);
                    JobsDiagnostics.WriteJobError(diagnosticId, job, commandResult.Exception);
                    FailJob(job, commandResult.Exception);
                    return;
                }

                response = feature.CommandsFeature.GetCommandResult(command);
            }
            else if (job.RequestType == CommandResult.Api)
            {
                var reqCtx = await CreateRequestContextAsync(scope, request, job, linkedCts.Token);
                response = await feature.AppHost.ServiceController.ExecuteMessageAsync(reqCtx.Message, reqCtx,
                    linkedCts.Token);
                if (response is Exception e)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, e.Message);
                    JobsDiagnostics.WriteJobError(diagnosticId, job, e);
                    FailJob(job, e);
                    return;
                }

                if (response is IHttpError httpError)
                {
                    var errorStatus = httpError.Response.GetResponseStatus()
                       ?? ResponseStatusUtils.CreateResponseStatus(httpError.ErrorCode,httpError.Message,null);
                    activity?.SetStatus(ActivityStatusCode.Error, errorStatus.Message);
                    JobsDiagnostics.WriteJobError(diagnosticId, job, null);
                    FailJob(job, errorStatus, shouldRetry: false);
                    return;
                }
            }
            else throw new NotSupportedException($"Unsupported Job Request Type: '{job.RequestType}'");

            var onSuccess = job.OnSuccess;
            onSuccess?.Invoke(response);

            PerformDbUpdates();
            CompleteJob(job, response);
            JobsDiagnostics.WriteJobAfter(diagnosticId, job);
        }
        catch (TaskCanceledException tex)
        {
            FailJob(job, tex);
        }
        catch (Exception ex)
        {
            FailJob(job, ex);
        }
        finally
        {
            cancellationSources.Remove(job.Id, out _);
            cancelJobIds.Remove(job.Id, out _);
        }
    }

    public override bool CancelJob(long jobId)
    {
        var wasCancelled = false;
        var cancelledDependents = new List<BackgroundJobBase>();
        using var db = OpenDb();
        var error = new TaskCanceledException("Job was cancelled").ToResponseStatus();
        lock (db.GetWriteLock())
        {
            var now = DateTime.UtcNow;
            var updatedQueuedJob = db.UpdateOnly(() => new BackgroundJob {
                State = BackgroundJobState.Cancelled,
                Error = error,
                ErrorCode = error.ErrorCode,
                LastActivityDate = now,
                CancelRequestedDate = now,
            }, where: x => x.Id == jobId && x.State == BackgroundJobState.Queued);
            var updatedRunningJob = updatedQueuedJob == 0
                ? db.UpdateOnly(() => new BackgroundJob { CancelRequestedDate = now },
                    where: x => x.Id == jobId && x.State == BackgroundJobState.Started && x.CancelRequestedDate == null)
                : 0;
            if (updatedQueuedJob > 0 || updatedRunningJob > 0)
            {
                cancelJobIds[jobId] = now;
                db.UpdateOnly(() => new JobSummary {
                    State = updatedQueuedJob > 0 ? BackgroundJobState.Cancelled : BackgroundJobState.Started,
                    CancelRequestedDate = now,
                    ErrorCode = updatedQueuedJob > 0 ? error.ErrorCode : null,
                    ErrorMessage = updatedQueuedJob > 0 ? error.Message : null,
                }, where: x => x.Id == jobId);
                wasCancelled = true;
                CancelDependentJobs(db, jobId, now, cancelledDependents);
            }
        }
        OnDependentJobsCancelled(cancelledDependents);
        if ((wasCancelled || jobId == 0) && cancellationSources.TryGetValue(jobId, out var cts))
        {
            cts.Cancel();
            return true;
        }
        return wasCancelled;
    }
    
    public List<long> CancelJobs(BackgroundJobState? state = null, string? worker = null)
    {
        
        List<long> jobIds = new();
        using (var db = OpenDb())
        {
            if (state != null)
            {
                jobIds.AddDistinctRange(db.Column<long>(db.From<BackgroundJob>()
                    .Where(x => x.State == state)));
            }
            if (worker != null)
            {
                var useWorker = worker == "None" ? null : worker;
                jobIds.AddDistinctRange(db.Column<long>(db.From<BackgroundJob>()
                    .Where(x => x.Worker == useWorker)));
            }
            // If no filter was supplied, cancel All Jobs
            if (state == null && worker == null)
            {
                jobIds = db.Column<long>(db.From<BackgroundJob>());
            }
        }
        foreach (var jobId in jobIds)
        {
            CancelJob(jobId);
        }
        return jobIds;
    }

    public void RequeueFailedJob(long jobId)
    {
        using var db = OpenDb();
        var jobSummary = db.SingleById<JobSummary>(jobId);
        if (jobSummary == null)
            throw HttpError.NotFound("Job not found");

        using var monthDb = OpenMonthDb(jobSummary.CreatedDate);
        var failedJob = monthDb.SingleById<FailedJob>(jobId);
        if (failedJob == null)
            throw HttpError.NotFound("Job not found");

        var previousState = failedJob.State;
        var requeueJob = failedJob.PopulateJob(new BackgroundJob());
        requeueJob.State = BackgroundJobState.Queued;
        requeueJob.RequestId = null;
        requeueJob.RunAfter = null;
        requeueJob.Response = null;
        requeueJob.ResponseBody = null;
        requeueJob.Logs = null;
        requeueJob.LogsTruncated = null;
        requeueJob.Error = null;
        requeueJob.ErrorCode = null;
        requeueJob.Attempts = 0;
        requeueJob.DurationMs = 0;
        requeueJob.StartedDate = null;
        requeueJob.CompletedDate = null;
        requeueJob.CancelRequestedDate = null;
        requeueJob.LastActivityDate = DateTime.UtcNow;

        lock (db.GetWriteLock())
        {
            db.Insert(requeueJob, enableIdentityInsert:true);
            monthDb.DeleteById<FailedJob>(failedJob.Id);
            db.UpdateOnly(() => new JobSummary {
                State = BackgroundJobState.Queued,
                StartedDate = null,
                CompletedDate = null,
                CancelRequestedDate = null,
                Attempts = 0,
                DurationMs = 0,
                ErrorCode = null,
                ErrorMessage = null,
                RunAfter = requeueJob.RunAfter,
                LogsTruncated = null,
            }, where: x => x.Id == jobId);
        }
        if (requeueJob.BatchId != null)
            RequeueJobInBatch(requeueJob.BatchId, previousState);
    }

    public void FailJob(BackgroundJob job, Exception ex)
    {
        if (ex is OperationCanceledException && IsShutdownCancellation(job))
        {
            // Interrupted by the App shutting down rather than cancelled or timed out. It's left
            // incomplete so it's requeued when the App restarts instead of recorded as Cancelled.
            log.LogWarning("JOBS Job {Id} was interrupted by shutdown, it will run again on restart", job.Id);
            return;
        }
        FailJob(job, ex, ShouldRetry(job, ex));
        // Callbacks are only available from the BackgroundJobOptions executed immediately
        var onFailed = job.OnFailed;
        onFailed?.Invoke(ex);
    }

    private bool ShouldRetry(BackgroundJob job, Exception ex)
    {
        var retryLimit = job.RetryLimit ?? feature.DefaultRetryLimit;
        // A Job that was asked to be cancelled is never retried, whatever exception it surfaced
        return job.Attempts <= retryLimit && !cancelJobIds.ContainsKey(job.Id) && feature.ShouldRetry(job, ex);
    }

    private bool IsShutdownCancellation(BackgroundJob job) =>
        !job.Transient && executionCts.IsCancellationRequested && !cancelJobIds.ContainsKey(job.Id)
        && job.Token?.IsCancellationRequested != true;

    private static bool IsCancelledErrorCode(string? errorCode) =>
        errorCode is nameof(TaskCanceledException) or nameof(OperationCanceledException);

    public void FailJob(BackgroundJob job, Exception ex, bool shouldRetry) => 
        FailJob(job, ex.ToResponseStatus(), shouldRetry);

    // Call within lock
    public void InsertFailedJob(IDbConnection dbMonth, BackgroundJobBase job)
    {
        var failedJob = job.PopulateJob(new FailedJob());
        try
        {
            dbMonth.Insert(failedJob);
        }
        catch (Exception e)
        {
            var existingJob = dbMonth.SingleById<FailedJob>(failedJob.Id);
            if (existingJob != null)
            {
                log.LogWarning("Existing FailedJob {Id} already exists, updating instead", failedJob.Id);
                dbMonth.Update(failedJob);
            }
            else
            {
                log.LogError(e, "Failed to Insert FailedJob {Id}: {Message}", failedJob.Id, e.Message);
                throw;
            }
        }
    }
    
    public void FailJob(BackgroundJob job, ResponseStatus error, bool shouldRetry)
    {
        job.Error = error;
        job.ErrorCode = error.ErrorCode;
        job.LastActivityDate = DateTime.UtcNow;
        JobsDiagnostics.RecordFailed(job, willRetry:shouldRetry);
        
        if (!job.Transient)
        {
            var recordScheduledRun = false;
            DateTime? retriedStartedDate = null;
            var cancelledDependents = new List<BackgroundJobBase>();
            lock (Locks.JobsDb)
            {
                if (!shouldRetry)
                {
                    job.State = IsCancelledErrorCode(error.ErrorCode)
                        ? BackgroundJobState.Cancelled
                        : BackgroundJobState.Failed;
                    job.CompletedDate = job.LastActivityDate;
                    if (job.StartedDate != null)
                        job.DurationMs = (int)(job.LastActivityDate.Value - job.StartedDate.Value).TotalMilliseconds;

                    using var db = feature.OpenDb();
                    using var trans = db.OpenTransaction();
                    var updated = db.UpdateOnly(() => new BackgroundJob {
                        State = job.State,
                        Error = job.Error,
                        ErrorCode = job.ErrorCode,
                        StartedDate = job.StartedDate,
                        CompletedDate = job.CompletedDate,
                        LastActivityDate = job.LastActivityDate,
                        Attempts = job.Attempts,
                        DurationMs = job.DurationMs,
                    }, where: x => x.Id == job.Id);
                    // Already archived, e.g. by ClearCancelledJobs(). Recording it again would
                    // count it against its batch twice.
                    if (updated == 0)
                    {
                        log.LogWarning("JOBS Discarded failure of Job {Id}: it's no longer active", job.Id);
                        return;
                    }

                    using (var dbMonth = feature.OpenMonthDb(job.CreatedDate))
                    {
                        InsertFailedJob(dbMonth, job);
                    }

                    db.UpdateOnly(() => new JobSummary {
                        State = job.State,
                        CompletedDate = job.CompletedDate,
                        ErrorMessage = job.Error.Message,
                        ErrorCode = job.ErrorCode,
                        Attempts = job.Attempts,
                        DurationMs = job.DurationMs,
                    }, where: x => x.Id == job.Id);

                    db.DeleteById<BackgroundJob>(job.Id);

                    CancelDependentJobs(db, job.Id, job.LastActivityDate.Value, cancelledDependents);

                    trans.Commit();
                    recordScheduledRun = true;
                }
                else
                {
                    var retryDelay = JobUtils.GetRetryDelay(job, feature.DefaultRetryBackoff,
                        feature.DefaultRetryDelayMs, feature.DefaultMaxRetryDelayMs, Random.Shared.NextDouble());
                    retriedStartedDate = job.StartedDate;
                    job.RequestId = null;
                    job.Attempts += 1;
                    job.State = BackgroundJobState.Queued;
                    job.StartedDate = null;
                    job.RunAfter = DateTime.UtcNow.Add(retryDelay);
                    using var db = feature.OpenDb();
                    db.UpdateOnly(() => new BackgroundJob {
                        RequestId = job.RequestId,
                        State = job.State,
                        Error = job.Error,
                        ErrorCode = job.ErrorCode,
                        Attempts = job.Attempts,
                        StartedDate = job.StartedDate,
                        RunAfter = job.RunAfter,
                        LastActivityDate = job.LastActivityDate,
                    }, where: x => x.Id == job.Id);
                    db.UpdateOnly(() => new JobSummary {
                        State = job.State,
                        Attempts = job.Attempts,
                        RunAfter = job.RunAfter,
                    }, where: x => x.Id == job.Id);
                }
            }
            // Outside the Jobs DB lock to avoid taking 2 DB locks at once
            if (recordScheduledRun)
            {
                RecordJobAttempt(job, job.Attempts, job.StartedDate, job.State, job.Error);
                UpdateScheduledTaskRun(job);
                UpdateJobBatch(job);
                ReleaseConcurrencySlot(job);
            }
            OnDependentJobsCancelled(cancelledDependents);
            if (shouldRetry)
                RecordJobAttempt(job, job.Attempts - 1, retriedStartedDate, job.State, job.Error);
        }
    }

    // Call within the main DB write lock.
    private void CancelDependentJobs(IDbConnection db, long jobId, DateTime lastActivityDate,
        List<BackgroundJobBase> cancelled)
    {
        // Cancel any Dependent Jobs as well
        // A dependent that runs once its parent finishes in any state is left to be dispatched
        var dependentJobs = db.Select<BackgroundJob>(x => x.DependsOn == jobId).Where(CancelsWithParent).ToList();
        if (dependentJobs.Count > 0)
        {
            foreach (var dependentJob in dependentJobs)
            {
                var depFailedJob = dependentJob.PopulateJob(new FailedJob());
                depFailedJob.State = BackgroundJobState.Cancelled;
                depFailedJob.ErrorCode = nameof(TaskCanceledException);
                depFailedJob.LastActivityDate = lastActivityDate;
                depFailedJob.CompletedDate = lastActivityDate;
                depFailedJob.Error = new() {
                    ErrorCode = depFailedJob.ErrorCode,
                    Message = "Parent Job failed"
                };
                using var dependentMonthDb = OpenMonthDb(dependentJob.CreatedDate);
                InsertFailedJob(dependentMonthDb, depFailedJob);
                db.UpdateOnly(() => new JobSummary {
                    State = depFailedJob.State,
                    CompletedDate = lastActivityDate,
                    ErrorMessage = depFailedJob.Error.Message,
                    ErrorCode = depFailedJob.ErrorCode,
                }, where: x => x.Id == depFailedJob.Id);
                db.DeleteById<BackgroundJob>(depFailedJob.Id);
                cancelled.Add(depFailedJob);
                CancelDependentJobs(db, depFailedJob.Id, lastActivityDate, cancelled);
            }
        }
    }

    /// <summary>
    /// A cancelled dependent still counts towards its batch, otherwise a batch with a Total would
    /// never complete. Called outside the Jobs DB lock, which these updates take themselves.
    /// </summary>
    private void OnDependentJobsCancelled(List<BackgroundJobBase> cancelled)
    {
        foreach (var job in cancelled)
        {
            UpdateScheduledTaskRun(job);
            UpdateJobBatch(job);
            ReleaseConcurrencySlot(job);
        }
    }

    // Runs on BG Thread
    private async Task NotifyCompletionAsync(BackgroundJob job, object? response = null)
    {
        if (job.NotifiedDate == null && job.Callback != null)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var services = scope.ServiceProvider;

                var commandInfo = AssertCommand(job.Callback);
                var command = services.GetRequiredService(commandInfo.Type);
                var requestType = commandInfo.Request.Type;

                response ??= requestType.CreateInstance();
                var msg = MessageFactory.Create(response);
                var reqCtx = new BasicRequest(response);
                reqCtx.SetBackgroundJob(job);
                reqCtx.SetCancellationToken(ct);
                if (command is IRequiresRequest requiresRequest)
                    requiresRequest.Request = reqCtx;

                var retryLimit = job.RetryLimit ?? feature.DefaultRetryLimit;
                CommandResult? commandResult;
                var i = 0;
                do
                {
                    commandResult = await feature.CommandsFeature.ExecuteCommandAsync(command, reqCtx.Dto, ct);
                    if (commandResult.Exception != null)
                    {
                        await ExecUtils.DelayBackOffMultiplierAsync(i);
                        continue;
                    }
                    break;
                } while (i++ < retryLimit && feature.ShouldRetry(job, commandResult.Exception));

                if (commandResult.Exception != null)
                {
                    FailJob(job, commandResult.Exception, shouldRetry:false);
                    return;
                }
                
                job.LastActivityDate = job.NotifiedDate = DateTime.UtcNow;
                job.Progress = 1;
                job.State = BackgroundJobState.Completed;
                if (job.StartedDate != null)
                    job.DurationMs = (int)(job.NotifiedDate.Value - job.StartedDate!.Value).TotalMilliseconds;

                using var db = OpenDb();
                db.UpdateOnly(() => new BackgroundJob {
                    NotifiedDate = job.NotifiedDate,
                    Progress = job.Progress,
                    State = job.State,
                    LastActivityDate = job.LastActivityDate,
                    DurationMs = job.DurationMs,
                }, where:x => x.Id == job.Id);
                db.UpdateOnly(() => new JobSummary {
                    State = job.State,
                    DurationMs = job.DurationMs,
                }, where:x => x.Id == job.Id);
                UpdateJobBatch(job);
            }
            catch (Exception ex)
            {
                PerformDbUpdates();
                FailJob(job, ex, shouldRetry:false);
                return;
            }
        }
        PerformDbUpdates();
        await NotifyReplyToAsync(job, response).ConfigAwait();
        ArchiveJob(job);
    }

    public void CompleteJob(BackgroundJob job, object? response=null)
    {
        using var db = feature.OpenDb();

        job.CompletedDate = job.LastActivityDate = DateTime.UtcNow;
        job.State = job.Callback != null ? BackgroundJobState.Executed : BackgroundJobState.Completed;
        if (job.StartedDate != null)
            job.DurationMs = (int)(job.CompletedDate.Value - job.StartedDate!.Value).TotalMilliseconds;
        if (job.Callback == null)
        {
            job.Progress = 1;
        }
        if (response != null)
        {
            job.Response = response.GetType().Name;
            var responseBody = ClientConfig.ToJson(response);
            if (responseBody != null && responseBody.Length > feature.MaxResponseBodyChars)
            {
                // Dropped rather than truncated, since a partial body can't be deserialized.
                // Recorded in Meta so it's visible why the result isn't there.
                log.LogWarning("JOBS Job {Id} Response of {Length} chars exceeds MaxResponseBodyChars, not persisted",
                    job.Id, responseBody.Length);
                job.Meta ??= new();
                job.Meta[JobMetaKeys.ResponseBodyOmitted] = responseBody.Length.ToString();
                job.ResponseBody = null;
            }
            else
            {
                job.ResponseBody = responseBody;
            }
        }

        if (!job.Transient)
        {
            var cancelledBeforeCompletion = false;
            var completionRejected = false;
            lock (db.GetWriteLock())
            {
                using var trans = db.OpenTransaction();
                var updated = db.UpdateOnly(() => new BackgroundJob {
                    Progress = job.Progress,
                    CompletedDate = job.CompletedDate,
                    DurationMs = job.DurationMs,
                    State = job.State,
                    Response = job.Response,
                    ResponseBody = job.ResponseBody,
                    Meta = job.Meta,
                    LastActivityDate = job.LastActivityDate,
                }, where: x => x.Id == job.Id && x.CancelRequestedDate == null && x.State != BackgroundJobState.Cancelled);

                if (updated > 0)
                {
                    db.UpdateOnly(() => new JobSummary {
                        CompletedDate = job.CompletedDate,
                        DurationMs = job.DurationMs,
                        State = job.State,
                        Response = job.Response,
                        Attempts = job.Attempts,
                    }, where: x => x.Id == job.Id);
                    trans.Commit();
                }
                else
                {
                    completionRejected = true;
                    var current = db.SingleById<BackgroundJob>(job.Id);
                    cancelledBeforeCompletion = current?.CancelRequestedDate != null || current?.State == BackgroundJobState.Cancelled;
                }
            }
            if (cancelledBeforeCompletion)
            {
                log.LogWarning("JOBS Job {Id} was cancelled before it completed, discarding its result", job.Id);
                FailJob(job, new TaskCanceledException("Job was cancelled"), shouldRetry:false);
                return;
            }
            if (completionRejected)
            {
                log.LogWarning("JOBS Discarded completion of Job {Id}: it is no longer queued", job.Id);
                return;
            }
            UpdateScheduledTaskRun(job);
            // A Job with a Callback isn't finished until its Callback runs, which records its
            // outcome in the batch, otherwise a failed Callback would be counted twice
            if (job.Callback == null)
                UpdateJobBatch(job);
            ReleaseConcurrencySlot(job);
            JobsDiagnostics.RecordCompleted(job);
        }

        if (job is { RequestType: CommandResult.Command, Command: not null, DurationMs: > 0 })
            lastCommandDurations[job.Worker != null ? $"{job.Command}.{job.Worker}" : job.Command] = job.DurationMs;
        else if (job is { RequestType: CommandResult.Api, DurationMs: > 0 })
            lastApiDurations[job.Worker != null ? $"{job.Request}.{job.Worker}" : job.Request] = job.DurationMs;

        if (job.Callback != null)
        {
            _ = Task.Factory.StartNew(() => NotifyCompletionAsync(job, response), ct);
        }
        else
        {
            if (job.ReplyTo != null)
            {
                var replyToJob = job;
                var replyToResponse = response;
                _ = Task.Factory.StartNew(() => NotifyReplyToAsync(replyToJob, replyToResponse), ct);
            }
            ArchiveJob(job);
        }
    }

    public void ArchiveJob(BackgroundJob job)
    {
        if (job.Transient) return;

        var now = DateTime.UtcNow;
        var requestId = Guid.NewGuid().ToString("N");
        var completedJob = job.PopulateJob(new CompletedJob());
        using var db = feature.OpenDb();
        using var dbMonth = feature.OpenMonthDb(job.CreatedDate);
        try
        {
            dbMonth.Insert(completedJob);
        }
        catch (Exception e)
        {
            var existingJob = dbMonth.SingleById<CompletedJob>(completedJob.Id);
            if (existingJob != null)
            {
                log.LogWarning("Existing CompletedJob {Id} already exists, updating instead", completedJob.Id);
                dbMonth.Update(completedJob);
            }
            else
            {
                log.LogError(e, "Failed to Insert CompletedJob {Id}: {Message}", completedJob.Id, e.Message);
                throw;
            }
        }
        db.DeleteById<BackgroundJob>(job.Id);
        
        var dispatchJobs = new List<BackgroundJob>();
        var dependentJobIds = db.Column<long>(db.From<BackgroundJob>()
            .Where(x => x.CompletedDate == null && x.RequestId == null && x.DependsOn == job.Id &&
                (x.RunAfter == null || x.RunAfter <= now))
            .Select(x => x.Id));

        if (dependentJobIds.Count > 0)
        {
            // Execute any jobs depending on this job
            db.UpdateOnly(() => new BackgroundJob {
                RequestId = requestId,
                StartedDate = now,
                LastActivityDate = now,
                State = BackgroundJobState.Queued,
                ParentId = job.Id,
            }, where:x => Sql.In(x.Id, dependentJobIds));
            
            dispatchJobs = db.Select<BackgroundJob>(x => x.RequestId == requestId);
        }
        if (dispatchJobs.Count > 0)
        {
            log.LogInformation("JOBS Queued {Count} Jobs dependent on {JobId}", dispatchJobs.Count, job.Id);
            var orderedJobs = dispatchJobs.OrderByDescending(x => x.Priority)
                .ThenBy(x => x.RunAfter ?? x.CreatedDate).ThenBy(x => x.Id);
            foreach (var dependentJob in orderedJobs)
            {
                dependentJob.ParentJob = completedJob;
                DispatchToWorker(dependentJob);
            }
        }
    }

    // Worker Manager
    private CancellationToken ct = new();
    /// <summary>
    /// Cancels running Jobs. Kept separate from the host's stopping token so running Jobs get
    /// ShutdownTimeoutSecs to finish whichever order the hosted services are stopped in.
    /// </summary>
    private CancellationTokenSource executionCts = new();
    public BackgroundJob? LastJob { get; set; }

    public Dictionary<string, int> GetWorkerQueueCounts()
    {
        var to = new Dictionary<string, int>();
        foreach (var (name, worker) in workers)
        {
            to[name] = worker.QueuedCount;
        }
        return to;
    }

    public override List<WorkerStats> GetWorkerStats() => workers.Select(x => x.Value.GetStats()).ToList();
    public override IDbConnection OpenDb() => feature.OpenDb();
    public IDbConnection OpenMonthDb(DateTime createdDate) => feature.OpenMonthDb(createdDate);

    public JobResult? GetJob(long jobId)
    {
        using var db = OpenDb(); 
        var summary = db.SingleById<JobSummary>(jobId);
        if (summary == null)
            return null;

        var to = new JobResult
        {
            Summary = summary,
            Queued = db.SingleById<BackgroundJob>(jobId),
        };
        if (to.Queued == null)
        {
            using var dbMonth = OpenMonthDb(summary.CreatedDate);
            to.Completed = dbMonth.SingleById<CompletedJob>(jobId);
            if (to.Completed == null)
                to.Failed = dbMonth.SingleById<FailedJob>(jobId);
        }
        return to;
    }

    public JobResult? GetJobByRefId(string refId)
    {
        using var db = OpenDb(); 
        var summary = db.Single<JobSummary>(x => x.RefId == refId);
        if (summary == null)
            return null;

        var to = new JobResult
        {
            Summary = summary,
            Queued = db.Single<BackgroundJob>(x => x.RefId == refId),
        };
        if (to.Queued == null)
        {
            using var dbMonth = OpenMonthDb(summary.CreatedDate);
            to.Completed = dbMonth.Single<CompletedJob>(x => x.RefId == refId);
            if (to.Completed == null)
                to.Failed = dbMonth.Single<FailedJob>(x => x.RefId == refId);
        }
        return to;
    }

    public void DispatchToWorker(BackgroundJob job)
    {
        // A Job should only ever be queued on a single worker. Transient Jobs are never persisted
        // so they all share Id 0 and can't be identified this way.
        foreach (var existingWorker in AssignedWorkers(job))
        {
            var runningTime = existingWorker.RunningTime ?? TimeSpan.Zero;
            var runningJob = existingWorker.RunningJob;
            log.LogWarning("JOBS Job {JobId} has already been queued on {Worker} (currently running job {RunningJobId} for {TotalSeconds}s)",
                job.Id, existingWorker.Name, runningJob?.Id, Math.Floor(runningTime.TotalSeconds));

            if (runningTime.TotalSeconds <= (runningJob?.TimeoutSecs ?? feature.DefaultTimeoutSecs))
            {
                log.LogWarning("JOBS Ignoring already queued job {Id}", job.Id);
                return;
            }

            // Worker is stuck on a Job that's exceeded its timeout, replace it with a new Worker
            CancelWorker(existingWorker.Name!);
            break;
        }

        var worker = job.Worker != null
            ? GetWorker(job.Worker)
            : GetLeastBusyQueueWorker(job.Queue);
        worker.Enqueue(job);
    }

    private IEnumerable<BackgroundJobsWorker> AssignedWorkers(BackgroundJob job)
    {
        if (job.Id == 0)
            yield break;
        foreach (var worker in workers.Values)
        {
            if (worker.HasJobQueued(job.Id))
                yield return worker;
        }
    }

    private BackgroundJobsWorker GetWorker(string name) => workers.GetOrAdd(name,
        _ => new BackgroundJobsWorker(this, ct, transient:false, feature.DefaultTimeoutSecs) { Name = name });

    /// <summary>
    /// Jobs in a queue are distributed over a bounded number of Workers. Choosing the least busy
    /// Worker for each dispatch stops a slow Job from blocking every other Job routed to it, which
    /// a fixed (e.g. hash-based) assignment would do.
    /// </summary>
    private BackgroundJobsWorker GetLeastBusyQueueWorker(string queue)
    {
        var concurrency = GetQueueConcurrency(queue);

        BackgroundJobsWorker? leastBusyWorker = null;
        var leastBusyCount = int.MaxValue;
        for (var i = 0; i < concurrency; i++)
        {
            var worker = GetWorker($"queue:{queue}:{i}");
            var pendingCount = worker.QueuedCount + (worker.RunningJob != null ? 1 : 0);
            if (pendingCount == 0)
                return worker;
            if (pendingCount < leastBusyCount)
            {
                leastBusyCount = pendingCount;
                leastBusyWorker = worker;
            }
        }
        return leastBusyWorker!;
    }

    public void CancelWorker(string worker)
    {
        if (workers.TryRemove(worker, out var bgWorker))
        {
            log.LogInformation("JOBS Cancelling worker {worker}...", worker);
            bgWorker.Cancel();
            
            // Transfer jobs to new Worker before disposing
            var newWorker = GetWorker(worker);
            foreach (var job in bgWorker.DrainPending())
            {
                newWorker.Enqueue(job);
            }
            
            bgWorker.Dispose();
        }
        else
        {
            log.LogWarning("JOBS worker {worker} not found", worker);
        }
    }

    private bool stopping;

    public Task StartAsync(CancellationToken stoppingToken)
    {
        ct = stoppingToken;
        stopping = false;
        var jobsCts = executionCts = new();
        var graceSecs = feature.ShutdownTimeoutSecs;
        stoppingToken.Register(() => {
            try { jobsCts.CancelAfter(TimeSpan.FromSeconds(graceSecs)); }
            catch (ObjectDisposedException) {}
        });
        log.LogInformation("JOBS Starting...");
        LoadJobQueue();
        LoadScheduledTasks();
        return Task.CompletedTask;
    }

    /// <summary>
    /// On App Startup, requeue any incomplete jobs and notify any completed jobs
    /// </summary>
    private void LoadJobQueue()
    {
        using var db = feature.OpenDb();
        var requestId = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        
        var commandDurations = db.Dictionary<string, int>(
            db.From<JobSummary>()
                .Where(j => Sql.In(j.Id,
                    db.From<JobSummary>()
                        .Where(x => x.State == BackgroundJobState.Completed
                            && x.DurationMs > 0
                            && x.RequestType == CommandResult.Command)
                        .GroupBy(x => new { x.Id, x.Command, x.Worker })
                        .SelectDistinct(x => x.Id)))
                .GroupBy(x => new { x.Command, x.Worker })
                .Select(x => new {
                    Command = Sql.Custom($"IIF({columns.Worker} is null, {columns.Command}, {columns.Command} || '.' || {columns.Worker})"), 
                    DurationMs = Sql.Custom($"CAST(AVG({columns.DurationMs}) AS INT)"),
                }));
        lastCommandDurations = new(commandDurations);
        
        var apiDurations = db.Dictionary<string, int>(
            db.From<JobSummary>()
                .Where(j => Sql.In(j.Id,
                    db.From<JobSummary>()
                        .Where(x => x.State == BackgroundJobState.Completed
                            && x.DurationMs > 0
                            && x.RequestType == CommandResult.Api)
                        .GroupBy(x => new { x.Id, x.Request, x.Worker })
                        .SelectDistinct(x => x.Id)))
                .GroupBy(x => new { x.Request, x.Worker })
                .Select(x => new {
                    Request = Sql.Custom($"IIF({columns.Worker} is null, {columns.Request}, {columns.Request} || '.' || {columns.Worker})"), 
                    DurationMs = Sql.Custom($"CAST(AVG({columns.DurationMs}) AS INT)"),
                }));
        lastApiDurations = new(apiDurations);

        var completedJobs = db.Select<BackgroundJob>(x => x.CompletedDate != null);
        if (completedJobs.Count > 0)
        {
            foreach (var completedJob in completedJobs)
            {
                try
                {
                    var response = CreateResponse(completedJob);
                    CompleteJob(completedJob, response);
                }
                catch (Exception e)
                {
                    log.LogError("Failed to complete job on Startup: {Id}", completedJob.Id);
                    FailJob(completedJob, e, shouldRetry: false);
                }
            }
        }
        
        db.UpdateOnly(() => new BackgroundJob {
            RequestId = requestId,
            StartedDate = null,
            LastActivityDate = now,
            State = BackgroundJobState.Queued,
        }, where:x => x.DependsOn == null && (x.RunAfter == null || now > x.RunAfter));

        var dispatchJobs = db.Select<BackgroundJob>(x => x.RequestId == requestId);
        if (dispatchJobs.Count > 0)
        {
            log.LogInformation("JOBS Queued {Count} Incomplete Jobs", dispatchJobs.Count);
            foreach (var job in dispatchJobs)
            {
                DispatchToWorker(job);
            }
        }

        // Execute any queued dependent jobs
        DispatchPendingJobs();
    }

    public void DispatchPendingJobs()
    {
        // A draining node finishes what it has without taking anything new
        if (IsDraining)
            return;
        using var db = feature.OpenDb();
        var expiredJobIds = new List<long>();
        var dependentJobIds = new List<long>();
        var scheduledJobIds = new List<long>();
        var pendingJobs = db.Select(db.From<BackgroundJob>()
            .Where(x => x.CompletedDate == null));

        var now = DateTime.UtcNow;
        var completedJobsMap = new Dictionary<long, CompletedJob>();
        if (pendingJobs.Count >= 0)
        {
            foreach (var job in pendingJobs)
            {
                // Requeue jobs that have timed out
                var timeoutDate = DateTime.UtcNow.AddSeconds(-(job.TimeoutSecs ?? feature.DefaultTimeoutSecs));
                var lastActivityDate = job.RunAfter != null && job.RunAfter > job.LastActivityDate
                    ? job.RunAfter.Value
                    : job.LastActivityDate;
                // Only a Job that was dispatched can time out. One that was never dispatched is
                // still waiting on its queue, dependency or batch, which requeueing would bypass.
                var dispatched = job.RequestId != null || job.State == BackgroundJobState.Started;
                if (dispatched && lastActivityDate < timeoutDate && job.State is BackgroundJobState.Queued or BackgroundJobState.Started)
                {
                    expiredJobIds.Add(job.Id);
                    continue;
                }
                if (job.RequestId != null)
                    continue;
                // Expired Jobs are cancelled by ExpireJobs(), paused queues wait to be resumed
                if (job.ExpiresAt != null && job.ExpiresAt < now)
                    continue;
                if (IsQueuePaused(job.Queue))
                    continue;
                // Fan-in: wait for every Job in the Batch this one depends on
                if (job.DependsOnBatch != null && !IsBatchFinished(job.DependsOnBatch))
                    continue;
                if (!TryTakeRateLimitSlot(job.Queue, now))
                    continue;
                if (job.RunAfter == null || now > job.RunAfter)
                {
                    if (job.DependsOn != null)
                    {
                        var parent = GetJob(job.DependsOn.Value);
                        if (parent?.Failed != null && CancelsWithParent(job))
                        {
                            // Queued after its parent had already failed, so CancelDependentJobs() missed it
                            CancelJob(job.Id);
                            continue;
                        }
                        var parentJob = GetFinishedParent(job, parent);
                        if (parentJob != null)
                        {
                            completedJobsMap[job.Id] = job.ParentJob = parentJob;
                            dependentJobIds.Add(job.Id);
                        }
                    }
                    else
                    {
                        scheduledJobIds.Add(job.Id);
                    }
                }
            }
        }

        var requestId = Guid.NewGuid().ToString("N");
        var requeudJobsCount = 0;
        var allIds = new HashSet<long>();
        allIds.AddDistinctRanges(dependentJobIds, scheduledJobIds);

        if (allIds.Count == 0 && expiredJobIds.Count == 0) 
            return;
        
        // Requeue any expired jobs
        if (expiredJobIds.Count > 0)
        {
            requeudJobsCount += db.UpdateOnly(() => new BackgroundJob {
                RequestId = requestId,
                State = BackgroundJobState.Queued,
                StartedDate = null,
                LastActivityDate = now,
            }, where:x => expiredJobIds.Contains(x.Id));
        }

        // Queue any eligible scheduled or dependent jobs
        if (allIds.Count > 0)
        {
            requeudJobsCount += db.UpdateOnly(() => new BackgroundJob {
                RequestId = requestId,
                LastActivityDate = now,
            }, where:x => allIds.Contains(x.Id) && x.RequestId == null);
        }
        
        if (requeudJobsCount > 0)
        {
            var requeudJobs = db.Select<BackgroundJob>(x => x.RequestId == requestId);
            if (requeudJobs.Count > 0)
            {
                log.LogInformation("JOBS Queueing {Count} Jobs ({ScheduledCount} Scheduled, {DependentCount} Dependent, {TimedOutCount} Expired)",
                    requeudJobs.Count, scheduledJobIds.Count, dependentJobIds.Count, expiredJobIds.Count);
                var orderedJobs = requeudJobs.OrderByDescending(x => x.Priority)
                    .ThenBy(x => x.RunAfter ?? x.CreatedDate).ThenBy(x => x.Id);
                foreach (var job in orderedJobs)
                {
                    if (job.DependsOn != null && completedJobsMap.TryGetValue(job.Id, out var completedJob))
                    {
                        job.ParentJob = completedJob;
                    }

                    if (!TryClaimConcurrencySlot(job))
                        continue;

                    DispatchToWorker(job);
                }
            }
        }
    }

    public void ClearCancelledJobs()
    {
        using var db = feature.OpenDb();
        var cancelledJobs = db.Select(db.From<BackgroundJob>()
            .Where(x => x.State == BackgroundJobState.Cancelled));
        foreach (var cancelledJob in cancelledJobs)
        {
            // Claim the row first: the Worker may have archived it already, and archiving it
            // again would count it against its batch twice
            int deleted;
            lock (db.GetWriteLock())
            {
                deleted = db.DeleteById<BackgroundJob>(cancelledJob.Id);
            }
            if (deleted == 0)
                continue;
            using var dbMonth = feature.OpenMonthDb(cancelledJob.CreatedDate);
            InsertFailedJob(dbMonth, cancelledJob);
            UpdateScheduledTaskRun(cancelledJob);
            UpdateJobBatch(cancelledJob);
            ReleaseConcurrencySlot(cancelledJob);
            JobsDiagnostics.RecordCancelled(cancelledJob);
        }
    }


    private DateTime lastSummaryPurge = DateTime.MinValue;

    /// <summary>
    /// Deletes JobSummary rows older than JobSummaryRetention. Only completed Jobs are deleted so
    /// this can never remove the summary of a Job that's still queued or running.
    /// </summary>
    private void PurgeExpiredJobSummaries()
    {
        if (feature.JobSummaryRetention == null)
            return;
        var now = DateTime.UtcNow;
        if (now - lastSummaryPurge < TimeSpan.FromHours(1))
            return;
        lastSummaryPurge = now;

        try
        {
            var expiredDate = now - feature.JobSummaryRetention.Value;
            using var db = feature.OpenDb();
            int deleted;
            lock (db.GetWriteLock())
            {
                deleted = db.Delete<JobSummary>(x => x.CreatedDate < expiredDate && x.CompletedDate != null &&
                    x.State != BackgroundJobState.Queued && x.State != BackgroundJobState.Started);
            }
            if (deleted > 0)
                log.LogInformation("JOBS Deleted {Count} JobSummary rows created before {ExpiredDate}",
                    deleted, expiredDate);
        }
        catch (Exception e)
        {
            log.LogError(e, "JOBS Error purging expired JobSummary rows");
        }
    }

    public void UpdateJobStatus(BackgroundJobStatusUpdate status)
    {
        updates.Enqueue(status);
    }

    record class Columns(string Logs, string LogsTruncated, string Status, string Progress, string LastActivityDate, string Id, string Request, string Command, string Worker, string DurationMs);

    private void PerformDbUpdates()
    {
        if (updates.Count == 0) return;

        using var db = feature.OpenDb();
        while (updates.TryDequeue(out var update))
        {
            try
            {
                var job = update.Job;
                var dbParams = new Dictionary<string,object?>();
                var fieldUpdates = new List<string>();
                var logsTruncated = false;

                if (update.Log != null)
                {
                    var previousLength = job.Logs?.Length ?? 0;
                    var separatorLength = previousLength > 0 ? 1 : 0;
                    var available = Math.Max(0, feature.MaxJobLogChars - previousLength - separatorLength);
                    var logPart = update.Log.Length > available ? update.Log[..available] : update.Log;
                    if (logPart.Length < update.Log.Length && job.LogsTruncated != true)
                    {
                        job.LogsTruncated = true;
                        logsTruncated = true;
                        // Parameterised as PostgreSQL 'boolean' and SQL Server 'bit' reject an integer literal
                        dbParams["logsTruncated"] = true;
                        fieldUpdates.Add($"{columns.LogsTruncated} = @logsTruncated");
                    }
                    if (logPart.Length > 0)
                    {
                        job.Logs = previousLength > 0 ? job.Logs + "\n" + logPart : logPart;
                        fieldUpdates.Add($"{columns.Logs} = CASE WHEN {columns.Logs} IS NOT NULL THEN {columns.Logs} || char(10) || @log ELSE @log END");
                        dbParams["log"] = logPart;
                    }
                }
                if (update.Status != null)
                {
                    job.Status = update.Status;
                    dbParams["status"] = job.Status;
                    fieldUpdates.Add($"{columns.Status} = @status");
                }
                if (update.Progress != null)
                {
                    dbParams["progress"] = job.Progress = update.Progress;
                    fieldUpdates.Add($"{columns.Progress} = @progress");
                }

                if (!job.Transient && fieldUpdates.Count > 0)
                {
                    job.LastActivityDate = DateTime.UtcNow;
                    dbParams["lastActivityDate"] = job.LastActivityDate;
                    fieldUpdates.Add($"{columns.LastActivityDate} = @lastActivityDate");
                    dbParams["id"] = job.Id;
                    var sql = $"UPDATE {Table} SET {string.Join(", ", fieldUpdates)} WHERE {columns.Id} = @id";
                    db.ExecuteSql(sql, dbParams);
                    if (logsTruncated)
                        db.UpdateOnly(() => new JobSummary { LogsTruncated = true }, where: x => x.Id == job.Id);
                }
            }
            catch (Exception e)
            {
                log.LogError(e, "JOBS Error updating Job Status");
            }
        }
    }


    /// <summary>
    /// Stops accepting new work and gives running Jobs a chance to finish so their progress and
    /// results are persisted rather than lost when the App shuts down.
    /// </summary>
    public async Task StopAsync(CancellationToken token = default)
    {
        log.LogInformation("JOBS Stopping...");
        stopping = true;

        // Jobs that were queued to a worker but never started stay queued in the database
        foreach (var worker in workers.Values)
        {
            worker.DrainPending();
        }

        var deadline = DateTime.UtcNow.AddSeconds(feature.ShutdownTimeoutSecs);
        while (DateTime.UtcNow < deadline && !token.IsCancellationRequested)
        {
            var runningJobs = workers.Values.Select(x => x.RunningJob).Where(x => x != null).ToList();
            if (runningJobs.Count == 0)
                break;
            PerformDbUpdates();
            await Task.Delay(200, CancellationToken.None).ConfigAwait();
        }
        // Jobs still running are interrupted and left incomplete, to be requeued on restart
        try { executionCts.Cancel(); }
        catch (Exception e) { log.LogError(e, "JOBS Error cancelling running Jobs on shutdown"); }

        PerformDbUpdates();
        RecordNodeStopped();
        log.LogInformation("JOBS Stopped");
    }

    public Task TickAsync()
    {
        if (stopping)
            return Task.CompletedTask;
        try
        {
            Interlocked.Increment(ref ticks);
            if (log.IsEnabled(LogLevel.Debug))
                log.LogDebug("JOBS Tick {Ticks}", ticks);

            DispatchPendingJobs();
            PerformDbUpdates();
            RecordNodeHeartbeat();
            ReloadScheduledTasksIfDue();
            ExecuteDueScheduledTasks();
            ExpireJobs();
            ClearCancelledJobs();
            ReleaseExpiredConcurrencySlots();
            PurgeExpiredJobSummaries();
            PurgeExpiredJobAttempts();
            PurgeExpiredArchives();
        }
        catch (Exception e)
        {
            log.LogError(e, "JOBS {Ticks} Tick Failed: {Message}", ticks, e.Message);
        }
        return Task.CompletedTask;
    }

    protected override CommandInfo AssertCommand(string? command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return feature.CommandsFeature.AssertCommandInfo(command);
    }

    public void Clear()
    {
        workers.Clear();
        updates.Clear();
        ClearScheduledTasks();
    }
}
