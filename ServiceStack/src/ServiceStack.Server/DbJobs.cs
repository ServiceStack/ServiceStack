#nullable enable
#if NET8_0_OR_GREATER
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Host;
using ServiceStack.Jobs;
using ServiceStack.Messaging;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

public partial class DbJobs : BackgroundJobsProviderBase, IBackgroundJobs, IBackgroundJobsScheduler,
    IBackgroundJobsTransactional
{
    private readonly ILogger<DbJobs> log;
    private readonly DatabaseJobFeature feature;
    private readonly IServiceProvider services;
    private readonly IServiceScopeFactory scopeFactory;
    private ConcurrentDictionary<string, int> lastCommandDurations = new();
    private ConcurrentDictionary<string, int> lastApiDurations = new();
    private ConcurrentDictionary<string, DbJobsWorker> workers = new();
    private readonly ConcurrentQueue<BackgroundJobStatusUpdate> updates = new();
    string Table;
    Columns columns;
    private long ticks = 0;
    
    public DbJobs(ILogger<DbJobs> log, 
        DatabaseJobFeature feature, IServiceProvider services, IServiceScopeFactory scopeFactory) : base(log)
    {
        // Need to store local references to these dependencies otherwise won't exist on BG Thread callbacks
        this.log = log;
        this.feature = feature;
        this.services = services;
        this.scopeFactory = scopeFactory;

        var dialect = feature.Dialect;
        this.Table = dialect.GetQuotedTableName(typeof(BackgroundJob));
        this.columns = new(
            Logs:dialect.GetQuotedColumnName(nameof(BackgroundJob.Logs)),
            LogsTruncated:dialect.GetQuotedColumnName(nameof(BackgroundJob.LogsTruncated)),
            Status:dialect.GetQuotedColumnName(nameof(BackgroundJob.Status)),
            Progress:dialect.GetQuotedColumnName(nameof(BackgroundJob.Progress)),
            LastActivityDate:dialect.GetQuotedColumnName(nameof(BackgroundJob.LastActivityDate)),
            LeaseToken:dialect.GetQuotedColumnName(nameof(BackgroundJob.LeaseToken)),
            LeaseExpiresAt:dialect.GetQuotedColumnName(nameof(BackgroundJob.LeaseExpiresAt)),
            Id:dialect.GetQuotedColumnName(nameof(BackgroundJob.Id)),
            Request:dialect.GetQuotedColumnName(nameof(BackgroundJob.Request)),
            Command:dialect.GetQuotedColumnName(nameof(BackgroundJob.Command)),
            Worker:dialect.GetQuotedColumnName(nameof(BackgroundJob.Worker)),
            DurationMs:dialect.GetQuotedColumnName(nameof(BackgroundJob.DurationMs))
        );
    }

    public BackgroundJobRef EnqueueApi(object requestDto, BackgroundJobOptions? options = null) =>
        RecordAndDispatchJob(CreateApiJob(requestDto, options));

    public override BackgroundJobRef EnqueueCommand(string commandName, object arg, BackgroundJobOptions? options = null) =>
        RecordAndDispatchJob(CreateCommandJob(commandName, arg, options));

    public BackgroundJobRef EnqueueApi(IDbConnection db, object requestDto, BackgroundJobOptions? options = null) =>
        RecordJob(db, CreateApiJob(requestDto, options));

    public BackgroundJobRef EnqueueCommand(IDbConnection db, string commandName, object arg, BackgroundJobOptions? options = null) =>
        RecordJob(db, CreateCommandJob(commandName, arg, options));

    private BackgroundJob CreateApiJob(object requestDto, BackgroundJobOptions? options)
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
            
        return options.ToBackgroundJob(CommandResult.Api, requestDto);
    }
 
    readonly ConcurrentDictionary<Type, bool> uniqueCommandTypes = new();

    private BackgroundJob CreateCommandJob(string commandName, object arg, BackgroundJobOptions? options)
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
        return job;
    }

    private void AssertCanQueue(BackgroundJob job)
    {
        // Reject oversized payloads rather than truncating: a truncated Request can't be
        // deserialized, so the Job would be unrunnable and fail on every attempt.
        if (job.RequestBody != null && job.RequestBody.Length > feature.MaxRequestBodyChars)
            throw new ArgumentException(
                $"Job Request is {job.RequestBody.Length} chars, which exceeds the " +
                $"{nameof(feature.MaxRequestBodyChars)} limit of {feature.MaxRequestBodyChars}. " +
                $"Pass a reference to the payload instead of the payload itself.", nameof(job.RequestBody));
        AssertValidReplyTo(job);
    }

    /// <summary>Inserts a Job, its summary and its batch count, within the caller's transaction</summary>
    private static void InsertJob(IDbConnection db, BackgroundJob job)
    {
        job.Id = db.Insert(job, selectIdentity: true);
        db.Insert(job.ToJobSummary());
        if (job.BatchId != null)
            AddJobToBatch(db, job.BatchId);
    }

    /// <summary>
    /// Records a Job on the caller's connection, inside whatever transaction it has open. It's left
    /// unclaimed for a tick to dispatch, since it can't run before the caller commits.
    /// </summary>
    private BackgroundJobRef RecordJob(IDbConnection db, BackgroundJob job)
    {
        ArgumentNullException.ThrowIfNull(db);
        AssertCanQueue(job);
        // Checked on the caller's connection so a Job it queued earlier in the same transaction is seen
        if (job.SingletonKey != null)
        {
            var singletonKey = job.SingletonKey;
            var existing = db.Single<BackgroundJob>(x => x.SingletonKey == singletonKey);
            if (existing?.RefId != null)
                return new(existing.Id, existing.RefId);
        }
        var existingRef = this.GetExistingJobRef(job);
        if (existingRef != null)
            return existingRef;
        if (job.BatchId != null)
            EnsureJobBatch(job.BatchId, job.CreatedBy);

        InsertJob(db, job);
        JobsDiagnostics.RecordQueued(job);
        return new(job.Id, job.RefId!);
    }

    private BackgroundJobRef RecordAndDispatchJob(BackgroundJob job)
    {
        AssertCanQueue(job);
        var existingRef = GetActiveSingletonRef(job) ?? this.GetExistingJobRef(job);
        if (existingRef != null)
            return existingRef;
        if (job.BatchId != null)
            EnsureJobBatch(job.BatchId, job.CreatedBy);
        var requestId = Guid.NewGuid().ToString("N");
        using var db = feature.OpenDb();
        var now = DateTime.UtcNow;
        // A Job that has to wait on a Batch, or take a ConcurrencyKey slot, is left for the next
        // tick to claim, so the gating in DispatchPendingJobs() applies to it as well. So is one
        // this node shouldn't take, which is left for another node to claim.
        var canDispatchNow = (job.RunAfter == null || now > job.RunAfter)
            && !IsQueuePaused(job.Queue)
            && job.DependsOnBatch == null
            && job.ConcurrencyKey == null
            && !IsDraining
            && ProcessesQueue(job.Queue)
            && HasPrefetchCapacity(job.Queue);
        if (canDispatchNow)
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

        if (job.RequestId != null)
        {
            job.LeaseOwner = feature.ServerId;
            job.LeaseToken = Guid.NewGuid().ToString("N");
            job.LeaseExpiresAt = now.AddSeconds(feature.LeaseDurationSecs);
        }

        Exception? insertEx = null;
        try
        {
            using var trans = db.OpenTransaction();
            InsertJob(db, job);
            trans.Commit();
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

        if (job.RequestId != null)
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
        options!.OnSuccess = r =>
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
                    LeaseExpiresAt = DateTime.UtcNow.AddSeconds(feature.LeaseDurationSecs),
                }, where: x => x.Id == job.Id && x.State == BackgroundJobState.Queued &&
                    x.CancelRequestedDate == null && x.LeaseToken == job.LeaseToken);
                if (claimed == 0)
                {
                    log.LogWarning("JOBS Skipping Job {Id}: it was cancelled or claimed by another node", job.Id);
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
                    FailJob(job, e);
                    return;
                }

                if (response is IHttpError httpError)
                {
                    var errorStatus = httpError.Response.GetResponseStatus()
                                      ?? ResponseStatusUtils.CreateResponseStatus(httpError.ErrorCode,
                                          httpError.Message, null);
                    FailJob(job, errorStatus, shouldRetry: false);
                    return;
                }
            }
            else throw new NotSupportedException($"Unsupported Job Request Type: '{job.RequestType}'");

            var onSuccess = job.OnSuccess;
            onSuccess?.Invoke(response);

            PerformDbUpdates();
            CompleteJob(job, response);
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
        using var db = OpenDb();
        var error = new TaskCanceledException("Job was cancelled").ToResponseStatus();

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
            CancelDependentJobs(db, jobId, now);
        }
        
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
        requeueJob.LeaseOwner = null;
        requeueJob.LeaseToken = null;
        requeueJob.LeaseExpiresAt = null;
        requeueJob.LastActivityDate = DateTime.UtcNow;

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
        if (requeueJob.BatchId != null)
            RequeueJobInBatch(requeueJob.BatchId, previousState);
    }

    public void FailJob(BackgroundJob job, Exception ex)
    {
        if (ex is OperationCanceledException && IsShutdownCancellation(job))
        {
            // Interrupted by the App shutting down rather than cancelled or timed out, so it's
            // returned to the queue to run again instead of being recorded as Cancelled
            log.LogWarning("JOBS Job {Id} was interrupted by shutdown, releasing it to be run again", job.Id);
            ReleaseLeases([job]);
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
            var leaseToken = job.LeaseToken;
            if (!shouldRetry)
            {
                job.State = IsCancelledErrorCode(error.ErrorCode)
                    ? BackgroundJobState.Cancelled
                    : BackgroundJobState.Failed;
                job.CompletedDate = job.LastActivityDate;
                if (job.StartedDate != null)
                    job.DurationMs = (int)(job.LastActivityDate.Value - job.StartedDate.Value).TotalMilliseconds;

                using var db = feature.OpenDb();

                // Claim the Job first. Everything after this is fenced off from any other node,
                // so the rest doesn't need to be wrapped in a transaction — and mustn't be: the
                // archive below writes on a second connection, which for the default provider is
                // the same database. Holding a write transaction open across that deadlocks.
                var updated = db.UpdateOnly(() => new BackgroundJob {
                    State = job.State,
                    Error = job.Error,
                    ErrorCode = job.ErrorCode,
                    StartedDate = job.StartedDate,
                    CompletedDate = job.CompletedDate,
                    LastActivityDate = job.LastActivityDate,
                    Attempts = job.Attempts,
                    DurationMs = job.DurationMs,
                }, where: x => x.Id == job.Id && x.LeaseToken == leaseToken);
                if (updated == 0)
                {
                    log.LogWarning("JOBS Discarded failure of Job {Id}: lease {LeaseToken} is no longer held",
                        job.Id, leaseToken);
                    return;
                }

                using (var dbMonth = feature.OpenMonthDb(job.CreatedDate))
                {
                    InsertFailedJob(dbMonth, job);
                }

                // Removed before the summary reports the Job as finished, so anyone who sees it finished
                // finds it in the archive rather than a stale copy in the active Jobs table
                db.Delete<BackgroundJob>(x => x.Id == job.Id && x.LeaseToken == leaseToken);

                db.UpdateOnly(() => new JobSummary {
                    State = job.State,
                    CompletedDate = job.CompletedDate,
                    ErrorMessage = job.Error.Message,
                    ErrorCode = job.ErrorCode,
                    Attempts = job.Attempts,
                    DurationMs = job.DurationMs,
                }, where: x => x.Id == job.Id);

                CancelDependentJobs(db, job.Id, job.LastActivityDate.Value);

                RecordJobAttempt(job, job.Attempts, job.StartedDate, job.State, job.Error);
                UpdateScheduledTaskRun(job);
                UpdateJobBatch(job);
                ReleaseConcurrencySlot(job);
            }
            else
            {
                var retryDelay = JobUtils.GetRetryDelay(job, feature.DefaultRetryBackoff,
                    feature.DefaultRetryDelayMs, feature.DefaultMaxRetryDelayMs, Random.Shared.NextDouble());
                var (startedDate, runAfter) = (job.StartedDate, job.RunAfter);
                job.RequestId = null;
                job.Attempts += 1;
                job.State = BackgroundJobState.Queued;
                job.StartedDate = null;
                job.RunAfter = DateTime.UtcNow.Add(retryDelay);
                using var db = feature.OpenDb();
                var updated = db.UpdateOnly(() => new BackgroundJob {
                    RequestId = job.RequestId,
                    State = job.State,
                    Error = job.Error,
                    ErrorCode = job.ErrorCode,
                    Attempts = job.Attempts,
                    StartedDate = job.StartedDate,
                    RunAfter = job.RunAfter,
                    LastActivityDate = job.LastActivityDate,
                    LeaseOwner = null,
                    LeaseToken = null,
                    LeaseExpiresAt = null,
                }, where: x => x.Id == job.Id && x.LeaseToken == leaseToken && x.CancelRequestedDate == null);
                if (updated == 0)
                {
                    // Cancellation requested on another node surfaces here as whatever exception the
                    // Job threw when its token fired. Retrying it would leave it queued forever,
                    // since a Job with a CancelRequestedDate is never claimed again.
                    if (db.Exists<BackgroundJob>(x => x.Id == job.Id && x.LeaseToken == leaseToken
                            && x.CancelRequestedDate != null))
                    {
                        job.Attempts -= 1;
                        (job.StartedDate, job.RunAfter) = (startedDate, runAfter);
                        FailJob(job, new TaskCanceledException("Job was cancelled").ToResponseStatus(), shouldRetry:false);
                        return;
                    }
                    log.LogWarning("JOBS Discarded retry of Job {Id}: lease {LeaseToken} is no longer held",
                        job.Id, leaseToken);
                    return;
                }
                job.LeaseOwner = null;
                job.LeaseToken = null;
                job.LeaseExpiresAt = null;
                RecordJobAttempt(job, job.Attempts - 1, startedDate, job.State, job.Error);
                // Hand the key back between attempts so a retry doesn't block its own queue
                ReleaseConcurrencySlot(job);
                db.UpdateOnly(() => new JobSummary {
                    State = job.State,
                    Attempts = job.Attempts,
                    RunAfter = job.RunAfter,
                }, where: x => x.Id == job.Id);
            }
        }
    }

    private void CancelDependentJobs(IDbConnection db, long jobId, DateTime lastActivityDate)
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
                // A cancelled dependent still counts towards its batch, otherwise a batch with a
                // Total would never complete
                UpdateScheduledTaskRun(depFailedJob);
                UpdateJobBatch(depFailedJob);
                ReleaseConcurrencySlot(depFailedJob);
                CancelDependentJobs(db, depFailedJob.Id, lastActivityDate);
            }
        }
    }

    /// <summary>Executed Jobs running their Callback, whose leases still need renewing</summary>
    private readonly ConcurrentDictionary<long, BackgroundJob> notifyingJobs = new();

    // Runs on BG Thread
    private async Task NotifyCompletionAsync(BackgroundJob job, object? response = null)
    {
        try
        {
            await NotifyCallbackAsync(job, response).ConfigAwait();
        }
        finally
        {
            notifyingJobs.TryRemove(job.Id, out _);
        }
    }

    private async Task NotifyCallbackAsync(BackgroundJob job, object? response)
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
                var updated = db.UpdateOnly(() => new BackgroundJob {
                    NotifiedDate = job.NotifiedDate,
                    Progress = job.Progress,
                    State = job.State,
                    LastActivityDate = job.LastActivityDate,
                    DurationMs = job.DurationMs,
                }, where:x => x.Id == job.Id && x.LeaseToken == job.LeaseToken && x.CancelRequestedDate == null);
                if (updated == 0)
                {
                    FailJob(job, new TaskCanceledException("Job was cancelled"), shouldRetry:false);
                    return;
                }
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
            using (var trans = db.OpenTransaction())
            {
                var updated = db.UpdateOnly(() => new BackgroundJob {
                    Progress = job.Progress,
                    CompletedDate = job.CompletedDate,
                    DurationMs = job.DurationMs,
                    State = job.State,
                    Response = job.Response,
                    ResponseBody = job.ResponseBody,
                    Meta = job.Meta,
                    LastActivityDate = job.LastActivityDate,
                }, where: x => x.Id == job.Id && x.LeaseToken == job.LeaseToken &&
                    x.CancelRequestedDate == null && x.State != BackgroundJobState.Cancelled);

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
                log.LogWarning("JOBS Discarded completion of Job {Id}: it is no longer owned by this node", job.Id);
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
            if (!job.Transient)
                notifyingJobs[job.Id] = job;
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

        var completedJob = job.PopulateJob(new CompletedJob());

        // Archive to monthly database (separate connection, no transaction needed)
        using (var dbMonth = feature.OpenMonthDb(job.CreatedDate))
        {
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
        }

        // Update main database with transaction
        using var db = feature.OpenDb();
        using var trans = db.OpenTransaction();

        var deleted = db.Delete<BackgroundJob>(x => x.Id == job.Id && x.LeaseToken == job.LeaseToken);
        if (deleted == 0)
        {
            log.LogWarning("JOBS Did not archive Job {Id}: lease {LeaseToken} is no longer held",
                job.Id, job.LeaseToken);
            return;
        }

        trans.Commit();
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

    private IEnumerable<DbJobsWorker> AssignedWorkers(BackgroundJob job)
    {
        if (job.Id == 0)
            yield break;
        foreach (var worker in workers.Values)
        {
            if (worker.HasJobQueued(job.Id))
                yield return worker;
        }
    }

    private DbJobsWorker GetWorker(string name) => workers.GetOrAdd(name,
        _ => new DbJobsWorker(this, ct, transient:false, feature.DefaultTimeoutSecs) { Name = name });

    /// <summary>
    /// Jobs in a queue are distributed over a bounded number of Workers. Choosing the least busy
    /// Worker for each dispatch stops a slow Job from blocking every other Job routed to it, which
    /// a fixed (e.g. hash-based) assignment would do.
    /// </summary>
    private DbJobsWorker GetLeastBusyQueueWorker(string queue)
    {
        var concurrency = GetQueueConcurrency(queue);

        DbJobsWorker? leastBusyWorker = null;
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
        var sqlCommandWorker = feature.Dialect.SqlConcat([columns.Command, "'.'", columns.Worker]);
        var isSqlServer = db.GetDialectProvider().Kind == DbKind.SqlServer;
        var durationColumn = isSqlServer ? $"CAST({columns.DurationMs} AS BIGINT)" : columns.DurationMs;

        var sqlAverageDuration = $"CAST(AVG({durationColumn}) AS INT)";
        
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
                    Command = Sql.Custom($"CASE WHEN {columns.Worker} is null THEN {columns.Command} ELSE {sqlCommandWorker} END"),
                    DurationMs = Sql.Custom(sqlAverageDuration),
                }));
        lastCommandDurations = new(commandDurations);

        var sqlRequestWorker = feature.Dialect.SqlConcat([columns.Request, "'.'", columns.Worker]);
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
                    Request = Sql.Custom($"CASE WHEN {columns.Worker} is null THEN {columns.Request} ELSE {sqlRequestWorker} END"),
                    DurationMs = Sql.Custom(sqlAverageDuration),
                }));
        lastApiDurations = new(apiDurations);

        // Only finish Jobs whose owner is gone. A live node may still be running a Job's Callback
        // or archiving it, and finishing it here as well would run its Callback twice.
        var now = DateTime.UtcNow;
        var completedJobs = db.Select<BackgroundJob>(x => x.CompletedDate != null
            && (x.LeaseExpiresAt == null || x.LeaseExpiresAt < now));
        if (completedJobs.Count > 0)
        {
            foreach (var completedJob in completedJobs)
            {
                var leaseToken = Guid.NewGuid().ToString("N");
                var leaseExpiresAt = now.AddSeconds(feature.LeaseDurationSecs);
                var claimed = db.UpdateOnly(() => new BackgroundJob {
                    LeaseOwner = feature.ServerId,
                    LeaseToken = leaseToken,
                    LeaseExpiresAt = leaseExpiresAt,
                }, where: x => x.Id == completedJob.Id && x.LeaseToken == completedJob.LeaseToken);
                if (claimed == 0)
                    continue; // Another node that's starting up claimed it first
                completedJob.LeaseOwner = feature.ServerId;
                completedJob.LeaseToken = leaseToken;
                completedJob.LeaseExpiresAt = leaseExpiresAt;
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
        
        DispatchPendingJobs();
    }

    public void DispatchPendingJobs()
    {
        // A draining node finishes what it has without taking anything new
        if (IsDraining || feature.Queues is { Count: 0 })
            return;

        using var db = feature.OpenDb();
        var now = DateTime.UtcNow;
        var staleDate = now.AddSeconds(-feature.DefaultTimeoutSecs);

        // Only select Jobs that are eligible to be claimed, ordered by the Job that should run next.
        // Filtering in SQL matters: the alternative reads every incomplete row (including its
        // Request/Response bodies and Logs) on every tick.
        var q = (db.From<BackgroundJob>()
            .Where(x => x.CompletedDate == null && x.CancelRequestedDate == null
                && (x.RunAfter == null || x.RunAfter <= now)
                // Expired Jobs are cancelled by ExpireJobs() instead of being run late
                && (x.ExpiresAt == null || x.ExpiresAt > now)
                && ((x.RequestId == null && x.LeaseToken == null && x.State == BackgroundJobState.Queued)
                    // Recover a Job whose owner stopped renewing its lease
                    || (x.LeaseToken != null && x.LeaseExpiresAt < now
                        && (x.State == BackgroundJobState.Queued || x.State == BackgroundJobState.Started))
                    // Recover a Job left Started without a lease, e.g. a node that stopped between
                    // claiming and writing its lease
                    || (x.LeaseToken == null && x.State == BackgroundJobState.Started
                        && x.LastActivityDate < staleDate)))
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.RunAfter)
            .ThenBy(x => x.Id)
            .Take(feature.ClaimBatchSize));

        if (feature.Queues != null)
        {
            var nodeQueues = feature.Queues;
            q.And(x => Sql.In(x.Queue, nodeQueues));
        }

        // Jobs that can't run yet are excluded in SQL rather than skipped after they're read,
        // otherwise a backlog of them fills every claim window and starves the Jobs behind them.
        // A queue this node already holds its prefetch limit of is excluded for the same reason:
        // claiming more would hoard Jobs that other nodes could be running.
        var blockedQueues = GetBlockedQueues(now);
        var inFlight = GetInFlightCounts();
        foreach (var (queue, count) in inFlight)
        {
            if (count >= GetInFlightLimit(queue))
                blockedQueues.Add(queue);
        }
        if (blockedQueues.Count > 0)
            q.And(x => !Sql.In(x.Queue, blockedQueues));
        // Keys whose slot another Job holds. A recovered Job (which has a lease) may hold its own.
        q.And(x => x.ConcurrencyKey == null || x.LeaseToken != null || !Sql.In(x.ConcurrencyKey,
            db.From<JobConcurrencyLock>().Where(l => l.ExpiresAt >= now).Select(l => l.Key)));
        // A dependent Job waits until its parent has left the active Jobs table
        q.And(x => x.DependsOn == null || !Sql.In(x.DependsOn,
            db.From<BackgroundJob>().Select(p => p.Id)));

        var pendingJobs = SelectClaimCandidates(db, q);
        if (pendingJobs.Count == 0)
            return;

        var candidates = new List<(BackgroundJob Job, bool Expired)>();
        var dependentJobs = pendingJobs.Where(x => x.DependsOn != null).ToList();
        var finishedDependencies = GetFinishedDependencies(dependentJobs);
        foreach (var job in pendingJobs)
        {
            // A paused queue leaves its Jobs unclaimed until it's resumed
            if (IsQueuePaused(job.Queue))
                continue;
            if (inFlight.GetValueOrDefault(job.Queue) >= GetInFlightLimit(job.Queue))
                continue;
            // Fan-in: wait for every Job in the Batch this one depends on
            if (job.DependsOnBatch != null && !IsBatchFinished(job.DependsOnBatch))
                continue;
            if (job.DependsOn != null)
            {
                finishedDependencies.TryGetValue(job.DependsOn.Value, out var parent);
                if (parent?.Failed != null && CancelsWithParent(job))
                {
                    // Queued after its parent had already failed, so CancelDependentJobs() missed it
                    CancelJob(job.Id);
                    continue;
                }
                var parentJob = GetFinishedParent(job, parent);
                if (parentJob == null)
                    continue;
                job.ParentJob = parentJob;
            }
            if (!TryTakeRateLimitSlot(job.Queue, now))
                continue;
            inFlight[job.Queue] = inFlight.GetValueOrDefault(job.Queue) + 1;
            var expired = job.LeaseToken != null || job.State == BackgroundJobState.Started;
            candidates.Add((job, expired));
        }

        if (candidates.Count == 0)
            return;

        var claimedJobs = new List<BackgroundJob>();
        foreach (var (job, expired) in candidates)
        {
            var requestId = Guid.NewGuid().ToString("N");
            var leaseToken = Guid.NewGuid().ToString("N");
            var leaseExpiresAt = now.AddSeconds(feature.LeaseDurationSecs);
            // A Job abandoned mid-execution counts as an attempt, otherwise a Job that crashes or
            // hangs its node would be recovered and run again forever
            var abandoned = expired && job.State == BackgroundJobState.Started;
            var attempts = abandoned ? job.Attempts + 1 : job.Attempts;
            var claimed = expired
                ? db.UpdateOnly(() => new BackgroundJob {
                    RequestId = requestId,
                    State = BackgroundJobState.Queued,
                    StartedDate = null,
                    LastActivityDate = now,
                    LeaseOwner = feature.ServerId,
                    LeaseToken = leaseToken,
                    LeaseExpiresAt = leaseExpiresAt,
                    Attempts = attempts,
                }, where: x => x.Id == job.Id && x.CancelRequestedDate == null &&
                    x.LeaseToken == job.LeaseToken && x.LastActivityDate == job.LastActivityDate)
                : db.UpdateOnly(() => new BackgroundJob {
                    RequestId = requestId,
                    State = BackgroundJobState.Queued,
                    StartedDate = null,
                    LastActivityDate = now,
                    LeaseOwner = feature.ServerId,
                    LeaseToken = leaseToken,
                    LeaseExpiresAt = leaseExpiresAt,
                }, where: x => x.Id == job.Id && x.CancelRequestedDate == null &&
                    x.RequestId == null && x.LeaseToken == null && x.State == BackgroundJobState.Queued);
            if (claimed == 0)
                continue;

            if (abandoned && job.Attempts > (job.RetryLimit ?? feature.DefaultRetryLimit))
            {
                job.RequestId = requestId;
                job.LeaseOwner = feature.ServerId;
                job.LeaseToken = leaseToken;
                job.LeaseExpiresAt = leaseExpiresAt;
                log.LogWarning("JOBS Job {Id} was abandoned by its node {Attempts} times, failing it",
                    job.Id, job.Attempts);
                FailJob(job, LeaseExpiredError, shouldRetry:false);
                continue;
            }
            if (abandoned)
                RecordJobAttempt(job, job.Attempts, job.StartedDate, BackgroundJobState.Queued, LeaseExpiredError);
            job.Attempts = attempts;

            // Serialise Jobs sharing a ConcurrencyKey. Taken after the claim so a Job can't hold a
            // key it didn't win, and released back if the slot is already taken.
            if (!TryAcquireConcurrencySlot(db, job, now))
            {
                db.UpdateOnly(() => new BackgroundJob {
                    RequestId = null,
                    LeaseOwner = null,
                    LeaseToken = null,
                    LeaseExpiresAt = null,
                }, where: x => x.Id == job.Id && x.LeaseToken == leaseToken);
                continue;
            }

            job.RequestId = requestId;
            job.State = BackgroundJobState.Queued;
            job.StartedDate = null;
            job.LastActivityDate = now;
            job.LeaseOwner = feature.ServerId;
            job.LeaseToken = leaseToken;
            job.LeaseExpiresAt = leaseExpiresAt;
            claimedJobs.Add(job);
            db.UpdateOnly(() => new JobSummary {
                State = BackgroundJobState.Queued,
                StartedDate = null,
            }, where: x => x.Id == job.Id);
        }

        if (claimedJobs.Count > 0)
        {
            log.LogInformation("JOBS Claimed {Count} Jobs on {ServerId}", claimedJobs.Count, feature.ServerId);
            foreach (var job in claimedJobs)
                DispatchToWorker(job);
        }
    }

    /// <summary>
    /// Selects the Jobs this node will try to claim. On an RDBMS that supports it, SKIP LOCKED lets
    /// competing nodes read disjoint sets in one query rather than racing for the same rows, which
    /// is what makes claiming scale with the number of App Servers.
    /// </summary>
    private List<BackgroundJob> SelectClaimCandidates(IDbConnection db, SqlExpression<BackgroundJob> q)
    {
        if (!feature.DbProvider.SupportsSkipLocked)
            return db.Select(q);

        try
        {
            return db.SqlList<BackgroundJob>(q.ToSelectStatement() + feature.DbProvider.SqlSkipLocked(),
                q.Params);
        }
        catch (Exception e)
        {
            // e.g. an older server without SKIP LOCKED, or a dialect that rejects it with this query
            log.LogWarning(e, "JOBS SKIP LOCKED claim failed, falling back to an ordinary select");
            return db.Select(q);
        }
    }

    private static readonly ResponseStatus LeaseExpiredError = new() {
        ErrorCode = JobErrorCodes.LeaseExpired,
        Message = "Job did not complete before its lease expired, e.g. its App Server stopped or it exceeded its timeout",
    };

    /// <summary>
    /// Resolves the finished parent Jobs of the specified dependent Jobs in a single query per
    /// month partition, instead of a lookup per Job.
    /// </summary>
    private Dictionary<long, JobResult> GetFinishedDependencies(List<BackgroundJob> dependentJobs)
    {
        var to = new Dictionary<long, JobResult>();
        if (dependentJobs.Count == 0)
            return to;

        var parentIds = dependentJobs.Select(x => x.DependsOn!.Value).Distinct().ToList();
        using var db = feature.OpenDb();
        var parentSummaries = db.Select(db.From<JobSummary>().Where(x => Sql.In(x.Id, parentIds)));

        foreach (var monthJobs in parentSummaries.GroupBy(x => new DateTime(x.CreatedDate.Year, x.CreatedDate.Month, 1)))
        {
            var monthIds = monthJobs.Select(x => x.Id).ToList();
            using var dbMonth = OpenMonthDb(monthJobs.Key);
            foreach (var completedJob in dbMonth.Select(dbMonth.From<CompletedJob>().Where(x => Sql.In(x.Id, monthIds))))
            {
                to[completedJob.Id] = new JobResult { Summary = monthJobs.First(x => x.Id == completedJob.Id), Completed = completedJob };
            }
            foreach (var failedJob in dbMonth.Select(dbMonth.From<FailedJob>().Where(x => Sql.In(x.Id, monthIds))))
            {
                if (!to.ContainsKey(failedJob.Id))
                    to[failedJob.Id] = new JobResult { Summary = monthJobs.First(x => x.Id == failedJob.Id), Failed = failedJob };
            }
        }
        return to;
    }

    public void ClearCancelledJobs()
    {
        using var db = feature.OpenDb();
        var now = DateTime.UtcNow;
        var cancelledJobs = db.Select(db.From<BackgroundJob>()
            .Where(x => x.State == BackgroundJobState.Cancelled ||
                (x.CancelRequestedDate != null && (x.LeaseExpiresAt == null || x.LeaseExpiresAt < now))));
        foreach (var cancelledJob in cancelledJobs)
        {
            cancelledJob.State = BackgroundJobState.Cancelled;
            cancelledJob.CompletedDate = cancelledJob.LastActivityDate = now;
            cancelledJob.ErrorCode ??= nameof(TaskCanceledException);
            cancelledJob.Error ??= new ResponseStatus {
                ErrorCode = cancelledJob.ErrorCode,
                Message = "Job was cancelled",
            };
            // Claim the row first so a lost race can't leave an archived copy of a Job that's
            // still active in the Jobs table
            var deleted = db.Delete<BackgroundJob>(x => x.Id == cancelledJob.Id &&
                x.LeaseToken == cancelledJob.LeaseToken);
            if (deleted == 0)
                continue;

            using var dbMonth = feature.OpenMonthDb(cancelledJob.CreatedDate);
            InsertFailedJob(dbMonth, cancelledJob);
            db.UpdateOnly(() => new JobSummary {
                State = BackgroundJobState.Cancelled,
                CompletedDate = now,
                ErrorCode = cancelledJob.ErrorCode,
                ErrorMessage = cancelledJob.Error.Message,
            }, where: x => x.Id == cancelledJob.Id);
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
            deleted = db.Delete<JobSummary>(x => x.CreatedDate < expiredDate && x.CompletedDate != null &&
                x.State != BackgroundJobState.Queued && x.State != BackgroundJobState.Started);
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

    record class Columns(string Logs, string LogsTruncated, string Status, string Progress, string LastActivityDate,
        string LeaseToken, string LeaseExpiresAt, string Id, string Request, string Command, string Worker, string DurationMs);

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
                        var newLine = feature.DbProvider.SqlChar(10);
                        fieldUpdates.Add($"{columns.Logs} = CASE WHEN {columns.Logs} IS NOT NULL THEN {feature.Dialect.SqlConcat([columns.Logs, newLine, "@log"])} ELSE @log END");
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
                    job.LeaseExpiresAt = job.LastActivityDate.Value.AddSeconds(feature.LeaseDurationSecs);
                    dbParams["lastActivityDate"] = job.LastActivityDate;
                    dbParams["leaseExpiresAt"] = job.LeaseExpiresAt;
                    dbParams["leaseToken"] = job.LeaseToken;
                    fieldUpdates.Add($"{columns.LastActivityDate} = @lastActivityDate");
                    fieldUpdates.Add($"{columns.LeaseExpiresAt} = @leaseExpiresAt");
                    dbParams["id"] = job.Id;
                    // A Job claimed before leases existed (or an in-process transient Job) has no
                    // token, so the fence needs to match NULL instead of comparing against it
                    var leaseFence = job.LeaseToken != null
                        ? $"{columns.LeaseToken} = @leaseToken"
                        : $"{columns.LeaseToken} IS NULL";
                    var sql = $"UPDATE {Table} SET {string.Join(", ", fieldUpdates)} " +
                        $"WHERE {columns.Id} = @id AND {leaseFence}";
                    var updated = db.ExecuteSql(sql, dbParams);
                    if (updated == 0)
                    {
                        log.LogWarning("JOBS Discarded status update for Job {Id}: lease {LeaseToken} is no longer held",
                            job.Id, job.LeaseToken);
                        continue;
                    }
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
    /// Stops accepting new work and gives running Jobs a chance to finish. Leases for Jobs that
    /// never started are released immediately so another node can pick them up straight away,
    /// instead of waiting out the lease duration on every deploy.
    /// </summary>
    public async Task StopAsync(CancellationToken token = default)
    {
        log.LogInformation("JOBS Stopping...");
        stopping = true;

        // Hand back Jobs that were claimed but never started
        var pendingJobs = new List<BackgroundJob>();
        foreach (var worker in workers.Values)
        {
            pendingJobs.AddRange(worker.DrainPending());
        }
        ReleaseLeases(pendingJobs);

        // Let Jobs that are already running finish within the shutdown budget
        var deadline = DateTime.UtcNow.AddSeconds(feature.ShutdownTimeoutSecs);
        var lastRenewal = DateTime.UtcNow;
        while (DateTime.UtcNow < deadline && !token.IsCancellationRequested)
        {
            var runningJobs = workers.Values.Select(x => x.RunningJob).Where(x => x != null).ToList();
            if (runningJobs.Count == 0)
                break;
            PerformDbUpdates();
            // Ticks have stopped, so keep renewing leases or a long ShutdownTimeoutSecs would let
            // another node recover these Jobs while they're still running here
            if (DateTime.UtcNow - lastRenewal > TimeSpan.FromSeconds(Math.Max(1, feature.LeaseDurationSecs / 3)))
            {
                lastRenewal = DateTime.UtcNow;
                try { RenewOwnedLeases(); }
                catch (Exception e) { log.LogError(e, "JOBS Error renewing leases on shutdown"); }
            }
            await Task.Delay(200, CancellationToken.None).ConfigAwait();
        }

        // Anything still running is abandoned, release its lease so it fails over immediately
        var stillRunning = workers.Values.Select(x => x.RunningJob)
            .Where(x => x is { Transient: false })
            .Select(x => x!)
            .ToList();
        if (stillRunning.Count > 0)
        {
            log.LogWarning("JOBS {Count} Jobs did not finish within {Secs}s, releasing their leases",
                stillRunning.Count, feature.ShutdownTimeoutSecs);
            ReleaseLeases(stillRunning);
        }
        // Their leases are released, so cancelling them now can't record them as Cancelled
        try { executionCts.Cancel(); }
        catch (Exception e) { log.LogError(e, "JOBS Error cancelling running Jobs on shutdown"); }

        PerformDbUpdates();
        RecordNodeStopped();
        log.LogInformation("JOBS Stopped");
    }

    /// <summary>
    /// Returns Jobs to the queue by clearing the claim this node holds on them, fenced by the
    /// lease token so it can't disturb a Job another node has since taken over.
    /// </summary>
    private void ReleaseLeases(List<BackgroundJob> jobs)
    {
        if (jobs.Count == 0)
            return;
        try
        {
            using var db = feature.OpenDb();
            var now = DateTime.UtcNow;
            var released = 0;
            foreach (var job in jobs)
            {
                if (job.Transient)
                    continue;
                released += db.UpdateOnly(() => new BackgroundJob {
                    RequestId = null,
                    State = BackgroundJobState.Queued,
                    StartedDate = null,
                    LastActivityDate = now,
                    LeaseOwner = null,
                    LeaseToken = null,
                    LeaseExpiresAt = null,
                }, where: x => x.Id == job.Id && x.LeaseToken == job.LeaseToken &&
                    x.CancelRequestedDate == null && x.CompletedDate == null);
            }
            if (released > 0)
                log.LogInformation("JOBS Released {Count} Job leases on shutdown", released);
        }
        catch (Exception e)
        {
            log.LogError(e, "JOBS Error releasing Job leases on shutdown");
        }
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

            RecordNodeHeartbeat();
            RenewOwnedLeases();
            DispatchPendingJobs();
            PerformDbUpdates();
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

    private void RenewOwnedLeases()
    {
        var leasedJobs = workers.Values.SelectMany(x => x.GetLeasedJobs())
            .Concat(notifyingJobs.Values)
            .Where(x => x is { Transient: false, LeaseToken: not null })
            .DistinctBy(x => x.Id)
            .ToList();
        if (leasedJobs.Count == 0)
            return;

        using var db = feature.OpenDb();
        var now = DateTime.UtcNow;
        var leaseExpiresAt = now.AddSeconds(feature.LeaseDurationSecs);
        foreach (var job in leasedJobs)
        {
            // Stop renewing a Job that's exceeded its timeout so that its lease expires and another
            // node can recover it. Cancellation is co-operative, so a Job that ignores its
            // CancellationToken would otherwise renew its lease (and hold its Worker) forever.
            var timeoutSecs = job.TimeoutSecs ?? feature.DefaultTimeoutSecs;
            if (job.State == BackgroundJobState.Started && job.StartedDate != null
                && now > job.StartedDate.Value.AddSeconds(timeoutSecs))
            {
                log.LogWarning("JOBS Job {Id} exceeded its {TimeoutSecs}s timeout, abandoning lease {LeaseToken}",
                    job.Id, timeoutSecs, job.LeaseToken);
                CancelLocalExecution(job.Id);
                // The Job may still be blocking its Worker, so hand the Jobs queued behind it to a
                // new Worker instead of leaving them stuck until it returns
                var stuckWorker = workers.Values.FirstOrDefault(x => x.RunningJob?.Id == job.Id);
                if (stuckWorker?.Name != null)
                    CancelWorker(stuckWorker.Name);
                continue;
            }

            var renewed = db.UpdateOnly(() => new BackgroundJob {
                LastActivityDate = now,
                LeaseExpiresAt = leaseExpiresAt,
            }, where: x => x.Id == job.Id && x.LeaseToken == job.LeaseToken &&
                (x.State == BackgroundJobState.Queued || x.State == BackgroundJobState.Started
                    || x.State == BackgroundJobState.Executed) &&
                x.CancelRequestedDate == null);
            if (renewed > 0)
            {
                job.LastActivityDate = now;
                job.LeaseExpiresAt = leaseExpiresAt;
                RenewConcurrencySlot(db, job, now);
            }
            else
            {
                // The lease is gone: either cancellation was requested (possibly on another node)
                // or another node reclaimed it after it expired. Either way this node should stop
                // executing it, its writes are fenced off from here on.
                log.LogWarning("JOBS Lost lease {LeaseToken} for Job {Id}, cancelling local execution",
                    job.LeaseToken, job.Id);
                CancelLocalExecution(job.Id);
            }
        }
    }

    /// <summary>
    /// Cancels a Job executing on this node. Unlike CancelJob() this doesn't change the Job's
    /// recorded state, which is owned by whichever node now holds the lease.
    /// </summary>
    private void CancelLocalExecution(long jobId)
    {
        if (!cancellationSources.TryGetValue(jobId, out var cts))
            return;
        try
        {
            cts.Cancel();
        }
        catch (Exception e)
        {
            log.LogError(e, "JOBS Error cancelling local execution of Job {Id}", jobId);
        }
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
#endif
