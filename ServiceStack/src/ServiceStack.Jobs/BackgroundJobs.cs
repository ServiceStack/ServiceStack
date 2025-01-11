using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Data;
using System.Security.Claims;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.Host;
using ServiceStack.Messaging;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Jobs;

public partial class BackgroundJobs : IBackgroundJobs
{
    private static readonly object dbWrites = Locks.JobsDb;
    readonly ILogger<BackgroundJobs> log;
    readonly BackgroundsJobFeature feature;
    private IServiceProvider services;
    readonly IServiceScopeFactory scopeFactory;
    private ConcurrentDictionary<string, int> lastCommandDurations = new();
    private ConcurrentDictionary<string, int> lastApiDurations = new();
    ConcurrentDictionary<string, BackgroundJobsWorker> workers = new();
    static ConcurrentQueue<BackgroundJobStatusUpdate> updates = new();
    string Table;
    Columns columns;
    private long ticks = 0;

    
    public BackgroundJobs(ILogger<BackgroundJobs> log, 
        BackgroundsJobFeature feature, IDbConnectionFactory dbFactory, IServiceProvider services, IServiceScopeFactory scopeFactory)
    {
        // Need to store local references to these dependencies otherwise won't exist on BG Thread callbacks
        this.log = log;
        this.feature = feature;
        this.services = services;
        this.scopeFactory = scopeFactory;

        var dialect = dbFactory.GetDialectProvider();
        this.Table = dialect.GetTableName(typeof(BackgroundJob));
        this.columns = new(
            Logs:dialect.GetQuotedColumnName(nameof(BackgroundJob.Logs)),
            Status:dialect.GetQuotedColumnName(nameof(BackgroundJob.Status)),
            Progress:dialect.GetQuotedColumnName(nameof(BackgroundJob.Progress)),
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
 
    readonly HashSet<Type> uniqueCommandTypes = new();

    public BackgroundJobRef EnqueueCommand(string commandName, object arg, BackgroundJobOptions? options = null)
    {
        var commandInfo = AssertCommand(commandName);
        uniqueCommandTypes.Add(commandInfo.Type);
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
        var requestId = Guid.NewGuid().ToString("N");
         using var db = feature.OpenDb();
        var now = DateTime.UtcNow;
        if (job.RunAfter == null || now > job.RunAfter)
        {
            if (job.DependsOn != null)
            {
                var dependsOnSummary = GetJob(job.DependsOn.Value);
                if (dependsOnSummary?.Completed != null)
                {
                    job.ParentJob = dependsOnSummary.Completed;
                    job.RequestId = requestId;
                }
            }
            else
            {
                job.RequestId = requestId;
            }
        }

        lock (dbWrites)
        {
            using var trans = db.OpenTransaction();
            job.Id = db.Insert(job, selectIdentity: true);
            var summary = job.ToJobSummary();
            db.Insert(summary);
            trans.Commit();
        }

        if (job.RequestId != null)
        {
            DispatchToWorker(job);
        }
        
        return new(job.Id, job.RefId!);
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

    public object CreateRequest(BackgroundJobBase job)
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
    
    public object? CreateResponse(BackgroundJobBase job)
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
                reqCtx.Items[Keywords.Session] = session;
            }
        }
        return reqCtx;
    }

    private readonly ConcurrentDictionary<long, CancellationTokenSource> cancellationSources = new();
    private readonly ConcurrentDictionary<long, DateTime> cancelJobIds = new();
    
    // Executed on BackgroundJobsWorker Thread
    public async Task ExecuteJobAsync(BackgroundJob job)
    {
        if (cancelJobIds.TryRemove(job.Id, out _))
        {
            FailJob(job, new TaskCanceledException("Job was cancelled"));
        }
        
        using var linkedCts = job.Token != null
            ? CancellationTokenSource.CreateLinkedTokenSource(ct, job.Token.Value)
            : CancellationTokenSource.CreateLinkedTokenSource(ct);
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
                lock (dbWrites)
                {
                    using var db = feature.OpenDb();
                    db.UpdateOnly(() => new BackgroundJob
                    {
                        StartedDate = job.StartedDate,
                        State = job.State,
                        LastActivityDate = job.LastActivityDate,
                    }, where: x => x.Id == job.Id);
                }
            }

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

    public bool CancelJob(long jobId)
    {
        var wasCancelled = false;
        using var db = OpenDb();
        var error = new TaskCanceledException("Job was cancelled").ToResponseStatus();
        lock (dbWrites)
        {
            var now = DateTime.UtcNow;
            var updatedQueuedJob = db.UpdateOnly(() => new BackgroundJob {
                State = BackgroundJobState.Cancelled,
                Error = error,
                ErrorCode = error.ErrorCode,
                LastActivityDate = now,
            }, where: x => x.Id == jobId);
            if (updatedQueuedJob > 0)
            {
                cancelJobIds[jobId] = DateTime.UtcNow;
                db.UpdateOnly(() => new JobSummary {
                    State = BackgroundJobState.Cancelled,
                    ErrorCode = error.ErrorCode,
                    ErrorMessage = error.Message,
                }, where: x => x.Id == jobId);
                wasCancelled = true;
            }
            using var dbMonth = OpenMonthDb(now);
            CancelDependentJobs(db, dbMonth, jobId, now);
        }
        if (cancellationSources.TryGetValue(jobId, out var cts))
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

        var requeueJob = failedJob.PopulateJob(new BackgroundJob());
        requeueJob.State = BackgroundJobState.Queued;
        requeueJob.RequestId = null;
        requeueJob.Response = null;
        requeueJob.ResponseBody = null;
        requeueJob.Logs = null;
        requeueJob.Error = null;
        requeueJob.ErrorCode = null;
        requeueJob.Attempts = 0;
        requeueJob.DurationMs = 0;
        requeueJob.StartedDate = requeueJob.LastActivityDate = DateTime.UtcNow;

        lock (dbWrites)
        {
            var jobMetadata = typeof(BackgroundJob).GetModelMetadata();
            try
            {
                jobMetadata.PrimaryKey.AutoIncrement = false;
                db.Insert(requeueJob);
                monthDb.DeleteById<FailedJob>(failedJob.Id);
            }
            finally
            {
                jobMetadata.PrimaryKey.AutoIncrement = true;
            }
        }
    }

    public void FailJob(BackgroundJob job, Exception ex)
    {
        FailJob(job, ex, ShouldRetry(job, ex));
        // Callbacks are only available from the BackgroundJobOptions executed immediately
        var onFailed = job.OnFailed;
        onFailed?.Invoke(ex);
    }

    private bool ShouldRetry(BackgroundJob job, Exception ex)
    {
        var retryLimit = job.RetryLimit ?? feature.DefaultRetryLimit;
        return job.Attempts <= retryLimit && feature.ShouldRetry(job, ex);
    }

    public void FailJob(BackgroundJob job, Exception ex, bool shouldRetry) => 
        FailJob(job, ex.ToResponseStatus(), shouldRetry);

    // Call within lock
    public void InsertFailedJob(IDbConnection dbMonth, BackgroundJob job)
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
            }
        }
    }
    
    public void FailJob(BackgroundJob job, ResponseStatus error, bool shouldRetry)
    {
        job.Error = error;
        job.ErrorCode = error.ErrorCode;
        job.LastActivityDate = DateTime.UtcNow;
        
        if (!job.Transient)
        {
            lock (dbWrites)
            {
                if (!shouldRetry)
                {
                    job.State = error.ErrorCode == nameof(TaskCanceledException)
                        ? BackgroundJobState.Cancelled
                        : BackgroundJobState.Failed;
                    if (job.StartedDate != null)
                        job.DurationMs = (int)(job.LastActivityDate.Value - job.StartedDate.Value).TotalMilliseconds;

                    using var dbMonth = feature.OpenMonthDb(job.CreatedDate);
                    InsertFailedJob(dbMonth, job);

                    using var db = feature.OpenDb();
                    using var trans = db.OpenTransaction();
                    db.UpdateOnly(() => new BackgroundJob {
                        State = job.State,
                        Error = job.Error,
                        ErrorCode = job.ErrorCode,
                        StartedDate = job.StartedDate,
                        LastActivityDate = job.LastActivityDate,
                        Attempts = job.Attempts,
                        DurationMs = job.DurationMs,
                    }, where: x => x.Id == job.Id);

                    db.UpdateOnly(() => new JobSummary {
                        State = job.State,
                        ErrorMessage = job.Error.Message,
                        ErrorCode = job.ErrorCode,
                        Attempts = job.Attempts,
                        DurationMs = job.DurationMs,
                    }, where: x => x.Id == job.Id);

                    db.DeleteById<BackgroundJob>(job.Id);

                    CancelDependentJobs(db, dbMonth, job.Id, job.LastActivityDate.Value);

                    trans.Commit();
                }
                else
                {
                    job.RequestId = null;
                    job.Attempts += 1;
                    job.State = BackgroundJobState.Queued;
                    job.StartedDate = DateTime.UtcNow;
                    using var db = feature.OpenDb();
                    db.UpdateOnly(() => new BackgroundJob {
                        RequestId = job.RequestId,
                        State = job.State,
                        Error = job.Error,
                        ErrorCode = job.ErrorCode,
                        Attempts = job.Attempts,
                        StartedDate = job.StartedDate,
                        LastActivityDate = job.LastActivityDate,
                    }, where: x => x.Id == job.Id);
                }
            }
        }
    }

    // Call within lock
    private static void CancelDependentJobs(IDbConnection db, IDbConnection dbMonth, long jobId, DateTime lastActivityDate)
    {
        // Cancel any Dependent Jobs as well
        var dependentJobs = db.Select<BackgroundJob>(x => x.DependsOn == jobId);
        if (dependentJobs.Count > 0)
        {
            foreach (var dependentJob in dependentJobs)
            {
                var depFailedJob = dependentJob.PopulateJob(new FailedJob());
                depFailedJob.State = BackgroundJobState.Cancelled;
                depFailedJob.ErrorCode = nameof(TaskCanceledException);
                depFailedJob.LastActivityDate = lastActivityDate;
                depFailedJob.Error = new() {
                    ErrorCode = depFailedJob.ErrorCode,
                    Message = "Parent Job failed"
                };
                dbMonth.Insert(depFailedJob);
                db.UpdateOnly(() => new JobSummary {
                    State = depFailedJob.State,
                    ErrorMessage = depFailedJob.Error.Message,
                    ErrorCode = depFailedJob.ErrorCode,
                }, where: x => x.Id == depFailedJob.Id);
                db.DeleteById<BackgroundJob>(depFailedJob.Id);
            }
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
                lock (dbWrites)
                {
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
                }
            }
            catch (Exception ex)
            {
                PerformDbUpdates();
                FailJob(job, ex, shouldRetry:false);
                return;
            }
        }
        PerformDbUpdates();
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
            job.ResponseBody = ClientConfig.ToJson(response);
        }

        if (!job.Transient)
        {
            lock (dbWrites)
            {
                using var trans = db.OpenTransaction();
                db.UpdateOnly(() => new BackgroundJob {
                    Progress = job.Progress,
                    CompletedDate = job.CompletedDate,
                    DurationMs = job.DurationMs,
                    State = job.State,
                    Response = job.Response,
                    ResponseBody = job.ResponseBody,
                    LastActivityDate = job.LastActivityDate,
                }, where: x => x.Id == job.Id);

                db.UpdateOnly(() => new JobSummary {
                    CompletedDate = job.CompletedDate,
                    DurationMs = job.DurationMs,
                    State = job.State,                
                    Response = job.Response,
                    Attempts = job.Attempts,
                }, where: x => x.Id == job.Id);
                trans.Commit();
            }
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
        lock (dbWrites)
        {
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
                }
            }
            db.DeleteById<BackgroundJob>(job.Id);
            
            // Execute any jobs depending on this job
            db.UpdateOnly(() => new BackgroundJob {
                RequestId = requestId,
                StartedDate = now,
                LastActivityDate = now,
                State = BackgroundJobState.Queued,
                ParentId = job.Id,
            }, where:x => x.CompletedDate == null && x.RequestId == null && x.DependsOn == job.Id);
        }
        
        var dispatchJobs = db.Select<BackgroundJob>(x => x.RequestId == requestId);
        if (dispatchJobs.Count > 0)
        {
            log.LogInformation("JOBS Queued {Count} Jobs dependent on {JobId}", dispatchJobs.Count, job.Id);
            var orderedJobs = dispatchJobs.OrderBy(x => x.RunAfter ?? x.CreatedDate).ThenBy(x => x.Id);
            foreach (var dependentJob in orderedJobs)
            {
                dependentJob.ParentJob = completedJob;
                DispatchToWorker(dependentJob);
            }
        }
    }

    // Worker Manager
    private CancellationToken ct = new();
    public BackgroundJob? LastJob { get; set; }

    public Dictionary<string, int> GetWorkerQueueCounts()
    {
        var to = new Dictionary<string, int>();
        foreach (var (name, worker) in workers)
        {
            to[name] = worker.Queue.Count;
        }
        return to;
    }

    public List<WorkerStats> GetWorkerStats() => workers.Select(x => x.Value.GetStats()).ToList();
    public IDbConnection OpenDb() => feature.OpenDb();
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
        // If job.Thread is specified, use a dedicated worker for that thread
        if (job.Worker != null)
        {
            var worker = workers.GetOrAdd(job.Worker, 
                _ => new BackgroundJobsWorker(this, ct, transient:false, feature.DefaultTimeoutSecs) { Name = job.Worker });
            if (worker.HasJobQueued(job.Id))
            {
                var runningTime = worker.RunningTime ?? TimeSpan.Zero;
                var runningJob = worker.RunningJob;
                log.LogWarning("JOBS Worker Job {job.Id} has already been queued (currently running job {RunningJobId} for {TotalSeconds})...",
                    job.Id, runningJob?.Id, Math.Floor(runningTime.TotalSeconds));

                if (runningTime.TotalSeconds > (runningJob?.TimeoutSecs ?? feature.DefaultTimeoutSecs))
                {
                    CancelWorker(job.Worker);
                    worker = workers.GetOrAdd(job.Worker, 
                        _ => new BackgroundJobsWorker(this, ct, transient:false, feature.DefaultTimeoutSecs) { Name = job.Worker });
                }
                else
                {
                    log.LogWarning("JOBS Ignoring already queued job {Id}", job.Id);
                    return;
                }
            }
            worker.Enqueue(job);
        }
        else
        {
            // Otherwise invoke a new worker immediately
            new BackgroundJobsWorker(this, ct, transient:true, feature.DefaultTimeoutSecs).Enqueue(job);
        }
    }

    public void CancelWorker(string worker)
    {
        if (workers.TryRemove(worker, out var bgWorker))
        {
            log.LogInformation("JOBS Cancelling worker {worker}...", worker);
            bgWorker.Cancel();
            
            // Transfer jobs to new Worker before disposing
            var newWorker = workers.GetOrAdd(worker, 
                _ => new BackgroundJobsWorker(this, ct, transient:false, feature.DefaultTimeoutSecs) { Name = worker });
            while (bgWorker.Queue.TryDequeue(out var job))
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

    public Task StartAsync(CancellationToken stoppingToken)
    {
        ct = stoppingToken;
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
                        .GroupBy(x => new { x.Command, x.Worker })
                        .Select(x => x.Id)))
                .Select(x => new {
                    Command = Sql.Custom($"IIF({columns.Worker} is null, {columns.Command}, {columns.Command} || '.' || {columns.Worker}) AS Command, {columns.DurationMs}"), 
                    x.DurationMs
                }));
        lastCommandDurations = new(commandDurations);
        
        var apiDurations = db.Dictionary<string, int>(
            db.From<JobSummary>()
                .Where(j => Sql.In(j.Id,
                    db.From<JobSummary>()
                        .Where(x => x.State == BackgroundJobState.Completed
                                    && x.DurationMs > 0
                                    && x.RequestType == CommandResult.Api)
                        .GroupBy(x => new { x.Command, x.Worker })
                        .Select(x => x.Id)))
                .Select(x => new {
                    Request = Sql.Custom($"IIF({columns.Worker} is null, {columns.Request}, {columns.Request} || '.' || {columns.Worker}) AS Request, {columns.DurationMs}"), 
                    x.DurationMs
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
        
        lock (dbWrites)
        {
            db.UpdateOnly(() => new BackgroundJob {
                RequestId = requestId,
                StartedDate = now,
                LastActivityDate = now,
                State = BackgroundJobState.Queued,
            }, where:x => x.DependsOn == null && (x.RunAfter == null || now > x.RunAfter));
        }

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
                if (job.CompletedDate == null && lastActivityDate < timeoutDate && job.State is BackgroundJobState.Queued or BackgroundJobState.Started)
                {
                    expiredJobIds.Add(job.Id);
                    continue;
                }
                if (job.RequestId != null)
                    continue;
                if (job.RunAfter == null || now > job.RunAfter)
                {
                    if (job.DependsOn != null)
                    {
                        var dependsOnSummary = GetJob(job.DependsOn.Value);
                        if (dependsOnSummary?.Completed != null)
                        {
                            completedJobsMap[job.DependsOn.Value] = job.ParentJob = dependsOnSummary.Completed;
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
        
        lock (dbWrites)
        {
            // Requeue any expired jobs
            if (expiredJobIds.Count > 0)
            {
                requeudJobsCount += db.UpdateOnly(() => new BackgroundJob {
                    RequestId = requestId,
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
        }
        
        if (requeudJobsCount > 0)
        {
            var requeudJobs = db.Select<BackgroundJob>(x => x.RequestId == requestId);
            if (requeudJobs.Count > 0)
            {
                log.LogInformation("JOBS Queueing {Count} Jobs ({ScheduledCount} Scheduled, {DependentCount} Dependent, {TimedOutCount} Expired)",
                    requeudJobs.Count, scheduledJobIds.Count, dependentJobIds.Count, expiredJobIds.Count);
                var orderedJobs = requeudJobs.OrderBy(x => x.RunAfter ?? x.CreatedDate).ThenBy(x => x.Id);
                foreach (var job in orderedJobs)
                {
                    if (job.DependsOn != null && completedJobsMap.TryGetValue(job.DependsOn.Value, out var completedJob))
                    {
                        job.ParentJob = completedJob;
                    }
                    
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
            using var dbMonth = feature.OpenMonthDb(cancelledJob.CreatedDate);
            lock (dbWrites)
            {
                InsertFailedJob(dbMonth, cancelledJob);
                db.DeleteById<BackgroundJob>(cancelledJob.Id);
            }
        }
    }

    public void UpdateJobStatus(BackgroundJobStatusUpdate status)
    {
        updates.Enqueue(status);
    }

    record class Columns(string Logs, string Status, string Progress, string Id, string Request, string Command, string Worker, string DurationMs);

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

                if (update.Log != null)
                {
                    job.Logs = job.Logs != null
                        ? job.Logs + "\n" + update.Log
                        : update.Log;
                    
                    fieldUpdates.Add($"Logs = CASE WHEN {columns.Logs} IS NOT NULL THEN {columns.Logs} || char(10) || @log ELSE @log END");
                    dbParams["log"] = update.Log;
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
                    dbParams["id"] = job.Id;
                    var sql = $"UPDATE {Table} SET {string.Join(", ", fieldUpdates)} WHERE {columns.Id} = @id";
                    lock (dbWrites)
                    {
                        db.ExecuteSql(sql, dbParams);
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError(e, "JOBS Error updating Job Status");
            }
        }
    }

    public Task TickAsync()
    {
        try
        {
            Interlocked.Increment(ref ticks);
            if (log.IsEnabled(LogLevel.Debug))
                log.LogDebug("JOBS Tick {Ticks}", ticks);

            DispatchPendingJobs();
            PerformDbUpdates();
            ExecuteDueScheduledTasks();
            ClearCancelledJobs();
        }
        catch (Exception e)
        {
            log.LogError(e, "JOBS {Ticks} Tick Failed: {Message}", ticks, e.Message);
        }
        return Task.CompletedTask;
    }

    private CommandInfo AssertCommand(string? command)
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
