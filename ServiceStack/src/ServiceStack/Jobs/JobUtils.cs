#nullable enable

using System;
using ServiceStack.Web;

namespace ServiceStack.Jobs;

public static class JobUtils
{
    public static BackgroundJobRef EnqueueApi<T>(this IBackgroundJobs jobs, T request, BackgroundJobOptions? options = null) 
        where T : class =>
        jobs.EnqueueApi(request.GetType().Name, request, options);

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

    public static BackgroundJobRef ScheduleCommand<TCommand>(this IBackgroundJobs jobs, object request, DateTime at, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand
    {
        options ??= new();
        options.RunAfter = at;
        return jobs.EnqueueCommand(typeof(TCommand).Name, request, options);
    }

    public static BackgroundJobRef ScheduleCommand<TCommand>(this IBackgroundJobs jobs, object request, TimeSpan after, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand
    {
        options ??= new();
        options.RunAfter = DateTime.UtcNow.Add(after);
        return jobs.EnqueueCommand(typeof(TCommand).Name, request, options);
    }

    public static BackgroundJob ExecuteTransientCommand<TCommand>(this IBackgroundJobs jobs, object request, BackgroundJobOptions? options = null) 
        where TCommand : IAsyncCommand =>
        jobs.ExecuteTransientCommand(typeof(TCommand).Name, request, options);

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
        };
    }

    public static T PopulateJob<T>(this BackgroundJobBase from, T to) where T : BackgroundJobBase
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
            CreatedDate = from.CreatedDate,
            CreatedBy = from.CreatedBy,
            RequestType = from.RequestType,
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
}
