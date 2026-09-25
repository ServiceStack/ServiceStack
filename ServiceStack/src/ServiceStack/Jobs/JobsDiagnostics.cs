#if NET8_0_OR_GREATER
#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using ServiceStack.Web;

namespace ServiceStack.Jobs;

/// <summary>
/// Observability for Background Jobs: an ActivitySource and Meter for OpenTelemetry, plus
/// DiagnosticListener events for ServiceStack's own profiling.
/// </summary>
public static class JobsDiagnostics
{
    public const string Name = "ServiceStack.Jobs";
    public static readonly ActivitySource ActivitySource = new(Name, typeof(JobsDiagnostics).Assembly.GetName().Version?.ToString());
    public static readonly Meter Meter = new(Name, typeof(JobsDiagnostics).Assembly.GetName().Version?.ToString());

    private static readonly Counter<long> QueuedCounter =
        Meter.CreateCounter<long>("servicestack.jobs.queued", "job", "Jobs added to a queue");
    private static readonly Counter<long> StartedCounter =
        Meter.CreateCounter<long>("servicestack.jobs.started", "job", "Jobs that began executing");
    private static readonly Counter<long> CompletedCounter =
        Meter.CreateCounter<long>("servicestack.jobs.completed", "job", "Jobs that completed successfully");
    private static readonly Counter<long> FailedCounter =
        Meter.CreateCounter<long>("servicestack.jobs.failed", "job", "Jobs that failed permanently");
    private static readonly Counter<long> RetriedCounter =
        Meter.CreateCounter<long>("servicestack.jobs.retried", "job", "Job attempts that were retried");
    private static readonly Counter<long> CancelledCounter =
        Meter.CreateCounter<long>("servicestack.jobs.cancelled", "job", "Jobs that were cancelled or expired");
    private static readonly Histogram<double> DurationHistogram =
        Meter.CreateHistogram<double>("servicestack.jobs.duration", "ms", "How long Jobs took to execute");
    private static readonly Histogram<double> WaitTimeHistogram =
        Meter.CreateHistogram<double>("servicestack.jobs.wait_time", "ms",
            "How long Jobs waited between being queued and starting");

    private static readonly DiagnosticListener Listener = new(Diagnostics.Listeners.ServiceStack);

    /// <summary>Tags shared by every metric and Activity, kept low cardinality</summary>
    private static TagList GetTags(BackgroundJobBase job) => new() {
        { "job.queue", job.Queue },
        { "job.type", job.RequestType },
        { "job.name", job.Command ?? job.Request },
        { "job.worker", job.Worker },
    };

    public static void RecordQueued(BackgroundJobBase job) => QueuedCounter.Add(1, GetTags(job));

    public static void RecordStarted(BackgroundJobBase job)
    {
        var tags = GetTags(job);
        StartedCounter.Add(1, tags);
        if (job.StartedDate != null)
        {
            var waitMs = (job.StartedDate.Value - (job.RunAfter ?? job.CreatedDate)).TotalMilliseconds;
            if (waitMs >= 0)
                WaitTimeHistogram.Record(waitMs, tags);
        }
    }

    public static void RecordCompleted(BackgroundJobBase job)
    {
        var tags = GetTags(job);
        CompletedCounter.Add(1, tags);
        DurationHistogram.Record(job.DurationMs, tags);
    }

    public static void RecordFailed(BackgroundJobBase job, bool willRetry)
    {
        var tags = GetTags(job);
        if (willRetry)
        {
            RetriedCounter.Add(1, tags);
            return;
        }
        tags.Add("error.code", job.ErrorCode);
        FailedCounter.Add(1, tags);
        DurationHistogram.Record(job.DurationMs, tags);
    }

    public static void RecordCancelled(BackgroundJobBase job)
    {
        var tags = GetTags(job);
        tags.Add("error.code", job.ErrorCode);
        CancelledCounter.Add(1, tags);
    }

    /// <summary>
    /// Starts an Activity for a Job execution. Profiling retains a local correlation Id even
    /// without an OpenTelemetry listener.
    /// </summary>
    public static Activity? StartActivity(BackgroundJobBase job)
    {
        var name = job.Command ?? job.Request;
        // Continue the trace of whatever queued the Job so an API request and the work it queued
        // appear in one trace, instead of the Job starting a disconnected root.
        var activity = job.TraceId != null &&
            ActivityContext.TryParse(job.TraceId, null, isRemote: true, out var parent)
            ? ActivitySource.StartActivity($"job {name}", ActivityKind.Consumer, parent)
            : ActivitySource.StartActivity($"job {name}", ActivityKind.Consumer);
        activity ??= StartProfilingActivity($"job {name}", job.TraceId);
        if (activity == null)
            return null;

        activity.SetTag("job.id", job.Id);
        activity.SetTag("job.queue", job.Queue);
        activity.SetTag("job.type", job.RequestType);
        activity.SetTag("job.name", name);
        activity.SetTag("job.attempt", job.Attempts);
        if (job.RefId != null)
            activity.SetTag("job.ref_id", job.RefId);
        if (job.BatchId != null)
            activity.SetTag("job.batch_id", job.BatchId);
        if (job.Worker != null)
            activity.SetTag("job.worker", job.Worker);
        if (job.Tag != null)
            activity.SetTag("job.tag", job.Tag);
        return activity;
    }

    /// <summary>Groups the database work of a background jobs pass in local Profiling history.</summary>
    public static Activity? StartInternalActivity(string name) =>
        ActivitySource.StartActivity(name, ActivityKind.Internal) ?? StartProfilingActivity(name);

    /// <summary>
    /// Groups the database work of a frequent background jobs pass (e.g. each poll) in local Profiling history only.
    /// Never uses the exported ActivitySource, so it doesn't create a root trace per tick.
    /// </summary>
    public static Activity? StartProfilingScope(string name) =>
        Activity.Current == null ? StartProfilingActivity(name, ignoreListeners: true) : null;

    private static Activity? StartProfilingActivity(string name, string? parentId = null, bool ignoreListeners = false)
    {
        if ((!ignoreListeners && ActivitySource.HasListeners()) || HostContext.AppHost?.HasPlugin<ProfilingFeature>() != true)
            return null;

        var traceId = parentId != null && ActivityContext.TryParse(parentId, null, out var parent)
            ? parent.TraceId.ToString()
            : parentId ?? Guid.NewGuid().ToString();
        return new Activity(name).SetParentId(traceId).Start();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Supports(string name) =>
        HostContext.AppHost?.HasPlugin<ProfilingFeature>() == true && Listener.IsEnabled(name);

    public static Guid WriteJobBefore(BackgroundJobBase job, IRequest? req = null)
    {
        if (!Supports(Diagnostics.Events.ServiceStack.WriteJobBefore))
            return Guid.Empty;
        var operationId = Guid.NewGuid();
        Listener.Write(Diagnostics.Events.ServiceStack.WriteJobBefore, new JobDiagnosticEvent {
            EventType = Diagnostics.Events.ServiceStack.WriteJobBefore,
            OperationId = operationId,
            Operation = job.Command ?? job.Request,
            Job = job,
            Request = req,
        });
        return operationId;
    }

    public static void WriteJobAfter(Guid operationId, BackgroundJobBase job, IRequest? req = null)
    {
        if (operationId == Guid.Empty || !Supports(Diagnostics.Events.ServiceStack.WriteJobAfter))
            return;
        Listener.Write(Diagnostics.Events.ServiceStack.WriteJobAfter, new JobDiagnosticEvent {
            EventType = Diagnostics.Events.ServiceStack.WriteJobAfter,
            OperationId = operationId,
            Operation = job.Command ?? job.Request,
            Job = job,
            Request = req,
        });
    }

    public static void WriteJobError(Guid operationId, BackgroundJobBase job, Exception? ex,
        IRequest? req = null)
    {
        if (operationId == Guid.Empty || !Supports(Diagnostics.Events.ServiceStack.WriteJobError))
            return;
        Listener.Write(Diagnostics.Events.ServiceStack.WriteJobError, new JobDiagnosticEvent {
            EventType = Diagnostics.Events.ServiceStack.WriteJobError,
            OperationId = operationId,
            Operation = job.Command ?? job.Request,
            Job = job,
            Request = req,
            Exception = ex,
            StackTrace = ex?.StackTrace ?? (Diagnostics.IncludeStackTrace ? Environment.StackTrace : null),
        });
    }
}
#endif
