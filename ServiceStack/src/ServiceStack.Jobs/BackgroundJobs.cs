using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using ServiceStack.Data;
using ServiceStack.Host;
using ServiceStack.Messaging;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Jobs;

public class BackgroundJobs : IBackgroundJobs
{
    private static readonly object dbWrites = new();
    readonly ILogger<BackgroundJobs> log;
    readonly BackgroundsJobFeature feature;
    
    public BackgroundJobs(ILogger<BackgroundJobs> log, BackgroundsJobFeature feature, IDbConnectionFactory dbFactory)
    {
        // Need to store local references to these dependencies otherwise wont exist on BG Thread callbacks
        this.log = log;
        this.feature = feature;

        var dialect = dbFactory.GetDialectProvider();
        this.Table = dialect.GetTableName(typeof(BackgroundJob));
        this.columns = new(
            Logs:dialect.GetQuotedColumnName(nameof(BackgroundJob.Logs)),
            Status:dialect.GetQuotedColumnName(nameof(BackgroundJob.Status)),
            Progress:dialect.GetQuotedColumnName(nameof(BackgroundJob.Progress)),
            Id:dialect.GetQuotedColumnName(nameof(BackgroundJob.Id))
        );
    }

    public BackgroundJobRef EnqueueApi(string requestDto, object request, BackgroundJobOptions? options = null)
    {
        var job = options.ToBackgroundJob(CommandResult.Api, request);
        return RecordAndDispatchJob(job);
    }

    public BackgroundJobRef EnqueueCommand(string commandName, object arg, BackgroundJobOptions? options = null)
    {
        var job = options.ToBackgroundJob(CommandResult.Command, arg);
        job.Command = commandName;
        return RecordAndDispatchJob(job);
    }

    private BackgroundJobRef RecordAndDispatchJob(BackgroundJob job)
    {
        lock (dbWrites)
        {
            using var db = feature.OpenJobsDb();
            using var trans = db.OpenTransaction();
            job.Id = db.Insert(job, selectIdentity: true);
            var summary = job.ToJobSummary();
            db.Insert(summary);
            trans.Commit();
        }

        Dispatch(job);

        return new(job.Id, job.RefId!, job.RequestId!);
    }

    public BackgroundJob ExecuteTransientCommand(string commandName, object arg, BackgroundJobOptions? options = null)
    {
        var job = options.ToBackgroundJob(CommandResult.Command, arg);
        job.Command = commandName;
        job.Transient = true;
        Dispatch(job);
        return job;
    }

    BasicRequest CreateRequestContext(Type requestType, BackgroundJob job)
    {
        var request = string.IsNullOrEmpty(job.RequestBody)
            ? requestType.CreateInstance()
            : JsonSerializer.DeserializeFromString(job.RequestBody, requestType);
        var msg = MessageFactory.Create(request);
        var reqCtx = new BasicRequest(request);
        reqCtx.Items[nameof(BackgroundJob)] = job;
        return reqCtx;
    }

    // Executed on BackgroundJobsWorker Thread
    public async Task ExecuteJobAsync(BackgroundJob job)
    {
        try
        {
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
                    using var db = feature.OpenJobsDb();
                    db.UpdateOnly(() => new BackgroundJob {
                        StartedDate = job.StartedDate,
                        State = job.State,
                    }, where: x => x.Id == job.Id);
                }
            }
            
            // Execute Command
            if (job.RequestType == null || job.Request == null)
                throw new ArgumentNullException(nameof(job.Request), "Job Request is not set");

            object? response = null;
            if (job.RequestType == CommandResult.Command)
            {
                var commandInfo = AssertCommand(job.Command);
                var command = feature.Services.GetRequiredService(commandInfo.Type);
                var requestType = commandInfo.Request.Type;

                var reqCtx = CreateRequestContext(requestType, job);
                if (command is IRequiresRequest requiresRequest)
                {
                    requiresRequest.Request = reqCtx;
                }
                var commandResult = await feature.CommandsFeature.ExecuteCommandAsync(command, reqCtx.Dto);

                var resultProp = commandInfo.Type.GetProperty("Result", BindingFlags.Instance | BindingFlags.Public);
                var resultAccessor = TypeProperties.Get(commandInfo.Type).GetAccessor("Result");
                response = resultProp != null
                    ? resultAccessor.PublicGetter(command)
                    : null;
            }
            else if (job.RequestType == CommandResult.Api)
            {
                var metadata = feature.AppHost.Metadata;
                var requestType = metadata.GetRequestType(job.Request)
                    ?? throw new NotSupportedException($"API Request Type for '{job.Request}' not found.");
                var reqCtx = CreateRequestContext(requestType, job);
                await feature.AppHost.ServiceController.ExecuteMessageAsync(reqCtx.Message, reqCtx, cts.Token);

                response = reqCtx.Response.Dto;
                var onSuccess = job.OnSuccess;
                onSuccess?.Invoke(response);
            }
            else throw new NotSupportedException($"Unsupported Job Request Type: '{job.RequestType}'");

            PerformDbUpdates();

            await CompleteJobAsync(job, response);
        }
        catch (TaskCanceledException tex)
        {
            var onFailed = job.OnFailed;
            onFailed?.Invoke(tex);
            await CancelJobAsync(job);
        }
        catch (Exception ex)
        {
            var onFailed = job.OnFailed;
            onFailed?.Invoke(ex);
            await FailJobAsync(job, ex);
        }
    }

    public Task CancelJobAsync(BackgroundJob job)
    {
        return FailJobAsync(job, new TaskCanceledException("Job was cancelled"));
    }

    public Task FailJobAsync(BackgroundJob job, Exception ex, bool shouldRetry)
    {
        job.Error = ex.ToResponseStatus();
        job.ErrorCode = job.Error.ErrorCode;
        job.LastActivityDate = DateTime.UtcNow;
        
        if (!job.Transient)
        {
            lock (dbWrites)
            {
                using var db = feature.OpenJobsDb();
                if (shouldRetry)
                {
                    job.State = BackgroundJobState.Failed;

                    db.UpdateOnly(() => new BackgroundJob {
                        State = job.State,
                        Error = job.Error,
                        ErrorCode = job.ErrorCode,
                        LastActivityDate = job.LastActivityDate,
                    }, where: x => x.Id == job.Id);

                    db.UpdateOnly(() => new JobSummary {
                        Status = job.State,
                        ErrorMessage = job.Error.Message,
                        ErrorCode = job.ErrorCode,
                    }, where: x => x.Id == job.Id);

                    using var dbMonth = feature.OpenJobsMonthDb(job.CreatedDate);
                    var failedJob = job.PopulateJob(new FailedJob());
                    dbMonth.Insert(failedJob);
                    db.DeleteById<BackgroundJob>(job.Id);
                }
                else
                {
                    job.RequestId = null;
                    job.StartedDate = null;
                    job.Attempts += 1;
                    job.State = BackgroundJobState.Queued;
                    db.UpdateOnly(() => new BackgroundJob {
                        RequestId = job.RequestId,
                        StartedDate = job.StartedDate,
                        State = job.State,
                        Error = job.Error,
                        ErrorCode = job.ErrorCode,
                        Attempts = job.Attempts,
                        LastActivityDate = job.LastActivityDate,
                    }, where: x => x.Id == job.Id);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task FailJobAsync(BackgroundJob job, Exception ex)
    {
        var retryLimit = job.RetryLimit ?? feature.DefaultRetryLimit;
        var shouldRetry = job.Attempts > retryLimit && feature.ShouldRetry(job,ex);
        return FailJobAsync(job, ex, shouldRetry);
    }

    // Runs on BG Thread
    private async Task NotifyCompletionAsync(BackgroundJob job, object? response = null)
    {
        if (job.NotifiedDate == null && job.Callback != null)
        {
            try
            {
                var commandInfo = AssertCommand(job.Callback);
                var command = feature.Services.GetRequiredService(commandInfo.Type);
                var requestType = commandInfo.Request.Type;

                response ??= requestType.CreateInstance();
                var msg = MessageFactory.Create(response);
                var reqCtx = new BasicRequest(response);
                reqCtx.Items[nameof(BackgroundJob)] = job;
                if (command is IRequiresRequest requiresRequest)
                {
                    requiresRequest.Request = reqCtx;
                }
                var commandResult = await feature.CommandsFeature.ExecuteCommandAsync(command, reqCtx.Dto);
            }
            catch (Exception ex)
            {
                _ = FailJobAsync(job, ex, shouldRetry:false);
                return;
            }
            job.NotifiedDate = DateTime.UtcNow;
            job.Progress = 1;
        }
        ArchiveJob(job);
    }

    public Task CompleteJobAsync(BackgroundJob job, object? response=null)
    {
        using var db = feature.OpenJobsDb();

        job.CompletedDate = job.LastActivityDate = DateTime.UtcNow;
        job.State = job.Callback != null ? BackgroundJobState.Executed : BackgroundJobState.Completed;
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

        lock (dbWrites)
        {
            db.UpdateOnly(() => new BackgroundJob {
                Progress = job.Progress,
                CompletedDate = job.CompletedDate,
                State = job.State,
                Response = job.Response,
                ResponseBody = job.ResponseBody,
                LastActivityDate = job.LastActivityDate,
            }, where: x => x.Id == job.Id);

            db.UpdateOnly(() => new JobSummary {
                CompletedDate = job.CompletedDate,
                DurationMs = job.DurationMs,
                Status = job.State,                
                Response = job.Response,
            }, where: x => x.Id == job.Id);
        }

        if (job.Callback != null)
        {
            _ = Task.Factory.StartNew(() => NotifyCompletionAsync(job, response), cts.Token);
        }
        else
        {
            ArchiveJob(job);
        }
        return Task.CompletedTask;
    }

    public void ArchiveJob(BackgroundJob job)
    {
        if (job.Transient) return;
        lock (dbWrites)
        {
            using var db = feature.OpenJobsDb();
            using var dbMonth = feature.OpenJobsMonthDb(job.CreatedDate);
            var completedJob = job.PopulateJob(new CompletedJob());
            dbMonth.Insert(completedJob);
            db.DeleteById<BackgroundJob>(job.Id);
        }
    }

    // Worker Manager
    private Task? bgTask;
    private CancellationTokenSource cts = new();
    private readonly BlockingCollection<BackgroundJob> queue = new();
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

    ConcurrentDictionary<string, BackgroundJobsWorker> workers = new();
    public void Dispatch(BackgroundJob job)
    {
        // If job.Thread is specified, use a dedicated worker for that thread
        if (job.Worker != null)
        {
            var worker = workers.GetOrAdd(job.Worker, 
                _ => new BackgroundJobsWorker(this, cts.Token) { Name = job.Worker });
            worker.Enqueue(job);
        }
        else
        {
            // Otherwise invoke a new worker immediately
            new BackgroundJobsWorker(this, cts.Token).Enqueue(job);
        }
    }

    public void Start()
    {
        LoadJobQueue();
        cts = new CancellationTokenSource();
        bgTask = Task.Factory.StartNew(Run, null, TaskCreationOptions.LongRunning);
        log.LogInformation("JOBS Starting...");
    }

    /// <summary>
    /// On App Startup, requeue any incomplete jobs and notify any completed jobs
    /// </summary>
    private void LoadJobQueue()
    {
        using var db = feature.OpenJobsDb();
        var requestId = Guid.NewGuid().ToString("N");

        lock (dbWrites)
        {
            db.UpdateOnly(() => new BackgroundJob {
                RequestId = requestId,
                StartedDate = null,
                State = BackgroundJobState.Queued,
            }, where:x => x.CompletedDate == null);
        }

        var dispatchJobs = db.Select<BackgroundJob>(x => x.RequestId == requestId);
        var notifyJobs = db.Select<BackgroundJob>(x => x.CompletedDate != null);

        if (dispatchJobs.Count > 0)
        {
            log.LogInformation("JOBS Queued {Count} Incomplete Jobs", dispatchJobs.Count);
            foreach (var job in dispatchJobs)
            {
                Dispatch(job);
            }
        }
        if (notifyJobs.Count > 0)
        {
            log.LogInformation("JOBS Notifying {Count} Jobs", notifyJobs.Count);
            _ = Task.Factory.StartNew(async () => {
                foreach (var job in notifyJobs)
                {
                    await NotifyCompletionAsync(job);
                }
            }, cts.Token);
        }
    }

    public void DispatchPendingJobs()
    {
        using var db = feature.OpenJobsDb();
        var incompleteJobIds = new List<long>();
        var pendingJobs = db.Select(db.From<BackgroundJob>()
            .Where(x => x.CompletedDate == null));

        if (pendingJobs.Count >= 0)
        {
            foreach (var x in pendingJobs)
            {
                // Requeue jobs that have timed out
                var timeoutDate = DateTime.UtcNow.AddSeconds(-(x.TimeoutSecs ?? feature.DefaultTimeoutSecs));
                if (x.CompletedDate == null && x.LastActivityDate < timeoutDate)
                {
                    incompleteJobIds.Add(x.Id);
                }
            }
        }

        var requestId = Guid.NewGuid().ToString("N");
        var incompleteJobsCount = 0;
        lock (dbWrites)
        {
            // Requeue any incomplete jobs
            if (incompleteJobIds.Count > 0)
            {
                incompleteJobsCount = db.UpdateOnly(() => new BackgroundJob {
                    RequestId = requestId,
                }, where:x => incompleteJobIds.Contains(x.Id) ||
                    (x.RequestId == null && (x.State == BackgroundJobState.Queued || x.State == BackgroundJobState.Started)));
            }
            else
            {
                incompleteJobsCount = db.UpdateOnly(() => new BackgroundJob {
                    RequestId = requestId,
                }, where:x => x.RequestId == null && (x.State == BackgroundJobState.Queued || x.State == BackgroundJobState.Started));
            }
        }

        if (incompleteJobsCount > 0)
        {
            var incompleteJobs = db.Select<BackgroundJob>(x => x.RequestId == requestId);
            if (incompleteJobs.Count > 0)
            {
                log.LogInformation("JOBS Requeueing {Count} Pending Jobs ({TimedOutCount} TimedOut)",
                    incompleteJobs.Count, incompleteJobIds.Count);
                foreach (var job in incompleteJobs)
                {
                    Dispatch(job);
                }
            }
        }
    }

    public static ConcurrentQueue<BackgroundJobStatusUpdate> updates = new();
    public void UpdateJobStatus(BackgroundJobStatusUpdate status)
    {
        updates.Enqueue(status);
    }

    string Table;
    Columns columns;
    record class Columns(string Logs, string Status, string Progress, string Id);

    private void PerformDbUpdates()
    {
        if (updates.Count == 0) return;

        using var db = feature.OpenJobsDb();
        while (updates.TryDequeue(out var update))
        {
            try
            {
                var job = update.Job;
                var dbParams = new Dictionary<string,object?>();
                var fieldUpdates = new List<string>();

                if (update.Status != null)
                {
                    job.Status = update.Status;
                }
                if (update.Log != null)
                {
                    if (job.Status == null)
                    {
                        // Update with last line of log
                        job.Status = update.Log.IndexOf('\n') >= 0 ? update.Log!.LastRightPart('\n') : update.Log;
                    }
                    job.Logs = job.Logs != null
                        ? job.Logs + "\n" + update.Log
                        : update.Log;
                    
                    fieldUpdates.Add($"Logs = CASE WHEN {columns.Logs} IS NOT NULL THEN {columns.Logs} || char(10) || @log ELSE @log END");
                    dbParams["log"] = update.Log;
                }
                if (update.Log != null || update.Status != null)
                {
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

    private long ticks = 0;

    public void Tick()
    {
        Interlocked.Increment(ref ticks);
        if (log.IsEnabled(LogLevel.Debug))
            log.LogDebug("JOBS Tick {Ticks}", ticks);

        DispatchPendingJobs();
        PerformDbUpdates();
    }

    public void EnqueueJob(BackgroundJob job)
    {
        queue.Add(job);
    }

    private Task Run(object? state)
    {
        while (!cts!.IsCancellationRequested)
        {
            foreach (var item in queue.GetConsumingEnumerable(cts.Token))
            {
                try
                {
                    LastJob = item;
                    if (log.IsEnabled(LogLevel.Debug))
                    {
                        log.LogDebug("JOBS [{RequestType} {Request} on {Worker}] Processing Job: {Id}",
                            item.Worker ?? "ANY", item.RequestType, item.Request, item.Id);
                    }

                    Dispatch(item);
                }
                catch (TaskCanceledException)
                {
                    log.LogInformation("JOBS Cancelled");
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "JOBS [{RequestType} {Request} on {Worker}] Failed to Process Job: {Id}",
                        item.Worker ?? "ANY", item.RequestType, item.Request, item.Id);
                }
            }
        }
        
        log.LogInformation("JOBS Stopped");
        return TypeConstants.EmptyTask;
    }

    public void Stop()
    {
        log.LogInformation("JOBS Stopping...");
        cts.Cancel();
    }

    private CommandInfo AssertCommand(string? command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var commandInfo = feature.CommandsFeature!.CommandInfos.FirstOrDefault(x => x.Name == command);
        if (commandInfo == null) 
            throw new InvalidOperationException($"Command '{command}' not found.");
        return commandInfo;
    }

    public void Dispose()
    {
        log.LogInformation("JOBS Disposing...");
        cts.Cancel();
        new IDisposable?[]{ cts, bgTask }.Dispose();
        bgTask = null;
    }
}

public static class BackgroundJobsExtensions
{
    public static BackgroundJobRef EnqueueApi<T>(this IBackgroundJobs jobs, T request, BackgroundJobOptions? options = null) where T : class =>
        jobs.EnqueueApi(request.GetType().Name, request, options);

    public static BackgroundJobRef EnqueueCommand<TCommand>(this IBackgroundJobs jobs, object request, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand =>
        jobs.EnqueueCommand(typeof(TCommand).Name, request, options);

    public static BackgroundJob ExecuteTransientCommand<TCommand>(this IBackgroundJobs jobs, object request, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand =>
        jobs.ExecuteTransientCommand(typeof(TCommand).Name, request, options);

    public static BackgroundJob ToBackgroundJob(this BackgroundJobOptions? options, string requestType, object arg)
    {
        return new BackgroundJob
        {
            RequestId = Guid.NewGuid().ToString("N"),
            State = BackgroundJobState.Queued,
            Attempts = 1,
            RefId = options?.RefId ?? Guid.NewGuid().ToString("N"),
            ParentId = options?.ParentId,
            Worker = options?.Worker,
            Tag = options?.Tag,
            Callback = options?.Callback,
            RunAfter = options?.RunAfter,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = options?.CreatedBy,
            RequestType = requestType,
            Request = arg.GetType().Name,
            RequestBody = ClientConfig.ToJson(arg),
            TimeoutSecs = options?.TimeoutSecs,
            ReplyTo = options?.ReplyTo,
            Args = options?.Args,
            OnSuccess = options?.OnSuccess,
            OnFailed = options?.OnFailed,
        };
    }

    public static T PopulateJob<T>(this BackgroundJob from, T to) where T : BackgroundJob
    {
        to.Id = from.Id;
        to.ParentId = from.ParentId;
        to.RefId = from.RefId;
        to.Worker = from.Worker;
        to.Tag = from.Tag;
        to.Callback = from.Callback;
        to.RunAfter = from.RunAfter;
        to.CreatedDate = from.CreatedDate;
        to.CreatedBy = from.CreatedBy;
        to.RequestId = from.RequestId;
        to.RequestType = from.RequestType;
        to.Command = from.Command;
        to.Request = from.Request;
        to.RequestBody = from.RequestBody;
        to.RequestUserId = from.RequestUserId;
        to.Response = from.Response;
        to.ResponseBody = from.ResponseBody;
        to.State = from.State;
        to.StartedDate = from.StartedDate;
        to.CompletedDate = from.CompletedDate;
        to.NotifiedDate = from.NotifiedDate;
        to.DurationMs = from.DurationMs;
        to.TimeoutSecs = from.TimeoutSecs;
        to.RetryLimit = from.RetryLimit;
        to.Attempts = from.Attempts;
        to.Progress = from.Progress;
        to.Status = from.Status;
        to.Logs = from.Logs;
        to.LastActivityDate = from.LastActivityDate;
        to.ReplyTo = from.ReplyTo;
        to.ErrorCode = from.ErrorCode;
        to.Error = from.Error;
        to.Args = from.Args;
        to.Meta = from.Meta;
        to.Transient = from.Transient;
        to.OnSuccess = from.OnSuccess;
        to.OnFailed = from.OnFailed;
        return to;
    }

    public static JobSummary ToJobSummary(this BackgroundJob from)
    {
        return new JobSummary {
            Id = from.Id,
            ParentId = from.ParentId,
            Tag = from.Tag,
            RefId = from.RefId,
            RequestId = from.RequestId,
            Request = from.Request,
            RequestType = from.RequestType,
            Worker = from.Worker,
            Callback = from.Callback,
            CreatedBy = from.CreatedBy,
            CreatedDate = from.CreatedDate,
        };
    }

    public static void SetBackgroundJob(this IRequest req, BackgroundJob job)
    {
        req.Items[nameof(BackgroundJob)] = job;
    }

    public static BackgroundJob AssertBackgroundJob(this IRequest? req) => req.GetBackgroundJob()
        ?? throw new Exception("BackgroundJob not found");

    public static BackgroundJob? GetBackgroundJob(this IRequest? req)
    {
        return req?.Items.TryGetValue(nameof(BackgroundJob), out var oJob) == true
            ? oJob as BackgroundJob
            : null;
    }

    public static void UpdateBackgroundJobStatus(this IBackgroundJobs jobs, IRequest? req, double? progress=null, string? status=null, string? log=null)
        => jobs.UpdateJobStatus(new(GetBackgroundJob(req) ?? throw new Exception("Background Job not found"), 
            progress: progress, status: status, log: log));
    public static void UpdateBackgroundJobStatus(this IBackgroundJobs jobs, BackgroundJob job, double? progress=null, string? status=null, string? log=null)
    {
        jobs.UpdateJobStatus(new(job, progress:progress, status:status, log:log));
    }
}
