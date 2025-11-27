#if NET8_0_OR_GREATER
#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ServiceStack.Web;

namespace ServiceStack.Jobs;

public static class JobUtils
{
    public static BackgroundJobRef EnqueueCommand<TCommand>(this IBackgroundJobs jobs, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand<NoArgs> =>
        jobs.EnqueueCommand(typeof(TCommand).Name, NoArgs.Value, options);

    public static BackgroundJobRef EnqueueCommand<TCommand>(this IBackgroundJobs jobs, object request, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand =>
        jobs.EnqueueCommand(typeof(TCommand).Name, request, options);

    public static BackgroundJobRef ScheduleApi<T>(this IBackgroundJobs jobs, T request, DateTime at, BackgroundJobOptions? options = null) 
        where T : class
    {
        options ??= new();
        options.RunAfter = at;
        return jobs.EnqueueApi(request, options);
    }

    public static BackgroundJobRef ScheduleApi<T>(this IBackgroundJobs jobs, T request, TimeSpan after, BackgroundJobOptions? options = null) 
        where T : class
    {
        options ??= new();
        options.RunAfter = DateTime.UtcNow.Add(after);
        return jobs.EnqueueApi(request, options);
    }

    public static BackgroundJobRef ScheduleCommand<TCommand>(this IBackgroundJobs jobs, DateTime at, BackgroundJobOptions? options = null)
        where TCommand : IAsyncCommand<NoArgs> => jobs.ScheduleCommand<TCommand>(NoArgs.Value, at, options);

    public static BackgroundJobRef ScheduleCommand<TCommand>(this IBackgroundJobs jobs, object request, DateTime at, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand
    {
        options ??= new();
        options.RunAfter = at;
        return jobs.EnqueueCommand(typeof(TCommand).Name, request, options);
    }

    public static BackgroundJobRef ScheduleCommand<TCommand>(this IBackgroundJobs jobs, TimeSpan after, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand<NoArgs> => jobs.ScheduleCommand<TCommand>(NoArgs.Value, after, options);

    public static BackgroundJobRef ScheduleCommand<TCommand>(this IBackgroundJobs jobs, object request, TimeSpan after, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand
    {
        options ??= new();
        options.RunAfter = DateTime.UtcNow.Add(after);
        return jobs.EnqueueCommand(typeof(TCommand).Name, request, options);
    }

    public static BackgroundJob RunCommand<TCommand>(this IBackgroundJobs jobs, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand<NoArgs> => jobs.RunCommand(typeof(TCommand).Name, NoArgs.Value, options);
    public static Task<object?> RunCommandAsync<TCommand>(this IBackgroundJobs jobs, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand<NoArgs> => jobs.RunCommandAsync(typeof(TCommand).Name, NoArgs.Value, options);
    public static BackgroundJob RunCommand<TCommand>(this IBackgroundJobs jobs, object request, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand => jobs.RunCommand(typeof(TCommand).Name, request, options);
    public static Task<object?> RunCommandAsync<TCommand>(this IBackgroundJobs jobs, object request, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand => jobs.RunCommandAsync(typeof(TCommand).Name, request, options);
    
    public static void RecurringCommand<TCommand>(this IBackgroundJobs jobs, Schedule schedule, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand<NoArgs> => jobs.RecurringCommand(typeof(TCommand).Name, schedule, typeof(TCommand).Name, NoArgs.Value, options);
    public static void RecurringCommand<TCommand>(this IBackgroundJobs jobs, string taskName, Schedule schedule, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand<NoArgs> => jobs.RecurringCommand(taskName, schedule, typeof(TCommand).Name, NoArgs.Value, options);
    public static void RecurringCommand<TCommand>(this IBackgroundJobs jobs, Schedule schedule, object request, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand => jobs.RecurringCommand(typeof(TCommand).Name, schedule, typeof(TCommand).Name, request, options);
    public static void RecurringCommand<TCommand>(this IBackgroundJobs jobs, string taskName, Schedule schedule, object request, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand => jobs.RecurringCommand(taskName, schedule, typeof(TCommand).Name, request, options);

    public static void RecurringApi(this IBackgroundJobs jobs, Schedule schedule, object requestDto, BackgroundJobOptions? options = null) =>
        jobs.RecurringApi(requestDto.GetType().Name, schedule, requestDto, options);
    
    public static BackgroundJob ToBackgroundJob(this BackgroundJobOptions? options, string requestType, object arg)
    {
        return new BackgroundJob
        {
            State = BackgroundJobState.Queued,
            Attempts = 1,
            RefId = options?.RefId ?? Guid.NewGuid().ToString("N"),
            ParentId = options?.ParentId,
            Worker = options?.Worker,
            Tag = options?.Tag,
            BatchId = options?.BatchId,
            Callback = options?.Callback,
            RunAfter = options?.RunAfter,
            DependsOn = options?.DependsOn,
            UserId = options?.UserId,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = options?.CreatedBy,
            RequestType = requestType,
            Request = arg.GetType().Name,
            RequestBody = ClientConfig.ToJson(arg),
            RetryLimit = options?.RetryLimit,
            TimeoutSecs = options?.TimeoutSecs,
            ReplyTo = options?.ReplyTo,
            Args = options?.Args,
            OnSuccess = options?.OnSuccess,
            OnFailed = options?.OnFailed,
            Token = options?.Token,
            LastActivityDate = DateTime.UtcNow,
        };
    }

    public static T PopulateJob<T>(this BackgroundJobBase from, T to) where T : BackgroundJobBase
    {
        to.Id = from.Id;
        to.ParentId = from.ParentId;
        to.RefId = from.RefId;
        to.Worker = from.Worker;
        to.Tag = from.Tag;
        to.BatchId = from.BatchId;
        to.Callback = from.Callback;
        to.RunAfter = from.RunAfter;
        to.CreatedDate = from.CreatedDate;
        to.CreatedBy = from.CreatedBy;
        to.RequestId = from.RequestId;
        to.RequestType = from.RequestType;
        to.Command = from.Command;
        to.Request = from.Request;
        to.RequestBody = from.RequestBody;
        to.UserId = from.UserId;
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
        return to;
    }

    public static JobSummary ToJobSummary(this BackgroundJob from)
    {
        return new JobSummary {
            Id = from.Id,
            ParentId = from.ParentId,
            RefId = from.RefId,
            Worker = from.Worker,
            Tag = from.Tag,
            BatchId = from.BatchId,
            CreatedDate = from.CreatedDate,
            CreatedBy = from.CreatedBy,
            RequestType = from.RequestType,
            Command = from.Command,
            Request = from.Request,
            Response = from.Response,
            UserId = from.UserId,
            Callback = from.Callback,
            StartedDate = from.StartedDate,
            CompletedDate = from.CompletedDate,
            State = from.State,
            DurationMs = from.DurationMs,
            Attempts = from.Attempts,
            ErrorMessage = from.Error?.Message,
            ErrorCode = from.ErrorCode,
        };
    }

    public static void SetCancellationToken(this IRequest req, CancellationToken token) => req.SetItem(nameof(CancellationToken), token);

    public static CancellationToken GetCancellationToken(this IRequest? req) =>
        req?.Items.TryGetValue(nameof(CancellationToken), out var oToken) == true
            ? (CancellationToken)oToken
            : default;

    public static BackgroundJob GetBackgroundJob(this IRequest? req) => req.TryGetBackgroundJob()
        ?? throw new Exception("BackgroundJob not found");

    public static void SetBackgroundJob(this IRequest req, BackgroundJob job) => req.SetItem(nameof(BackgroundJob), job);

    public static BackgroundJob? TryGetBackgroundJob(this IRequest? req)
    {
        return req?.Items.TryGetValue(nameof(BackgroundJob), out var oJob) == true
            ? oJob as BackgroundJob
            : null;
    }

    public static object? CreateRequest(this IBackgroundJobs jobs, JobResult? result)
    {
        var job = result?.Job;
        return job != null ? jobs.CreateRequest(job) : null;
    }
    public static object? CreateResponse(this IBackgroundJobs jobs, JobResult? result)
    {
        var job = result?.Job;
        return job != null ? jobs.CreateResponse(job) : null;
    }
    public static JobLogger CreateJobLogger(this IRequest req, IBackgroundJobs jobs, ILogger log=null) =>
        new(jobs, req.GetBackgroundJob(), log);
}
#endif