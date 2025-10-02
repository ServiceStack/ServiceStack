#if NET8_0_OR_GREATER
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Jobs;
using ServiceStack.Logging;

namespace ServiceStack;

public class DbJobsWorker : IDisposable
{
    public string? Name { get; set; }
    public ConcurrentQueue<BackgroundJob> Queue { get; } = new();
    public Task? BackgroundTask => bgTask; 
    private Task? bgTask;
    private long running = 0;
    public bool Running => Interlocked.Read(ref running) == 1;
    DateTime? lastRunStarted = null;
    public TimeSpan? RunningTime => lastRunStarted != null ? DateTime.UtcNow - (lastRunStarted ?? DateTime.UtcNow) : null;
    
    private long tasksStarted = 0; 
    private long received = 0; 
    private long retries = 0;
    private long failed = 0;
    private long completed = 0;
    private readonly IBackgroundJobs jobs;
    private readonly CancellationToken ct;
    private readonly CancellationTokenSource workerCts;
    private readonly bool transient;
    private bool cancelled;
    private bool disposed;
    private int defaultTimeOutSecs;
    private BackgroundJob? runningJob;
    public BackgroundJob? RunningJob => runningJob;

    public DbJobsWorker(IBackgroundJobs jobs, CancellationToken ct, bool transient, int defaultTimeOutSecs)
    {
        this.jobs = jobs;
        workerCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        this.ct = workerCts.Token;
        this.transient = transient;
        this.defaultTimeOutSecs = defaultTimeOutSecs;
    }

    public WorkerStats GetStats() => new()
    {
        Name = Name ?? "None",
        Queued = Queue.Count,
        Received = received,
        Completed = completed,
        Retries = retries,
        Failed = failed,
        RunningJob = runningJob?.Id,
        RunningTime = RunningTime,
    };

    public void Cancel(bool throwOnFirstException=false)
    {
        cancelled = true;
        workerCts.Cancel(throwOnFirstException);
    }

    public void Enqueue(BackgroundJob job)
    {
        Interlocked.Increment(ref received);
        Queue.Enqueue(job);
        if (Interlocked.CompareExchange(ref running, 1, 0) == 0)
        {
            Interlocked.Increment(ref tasksStarted);
            bgTask = Task.Factory.StartNew(RunAsync, new JobWorkerContext(Queue, jobs, ct), ct);
        }
    }

    public bool HasJobQueued(long jobId)
    {
        return runningJob?.Id == jobId || Queue.Any(x => x.Id == jobId);
    }

    record class JobWorkerContext(ConcurrentQueue<BackgroundJob> Queue, IBackgroundJobs Jobs, CancellationToken Token);

    // Runs on Worker Thread
    private async Task RunAsync(object? state)
    {
        try
        {
            // Runs all jobs in the queue, then exits
            var ctx = (JobWorkerContext)state!;
            while (ctx.Queue.TryDequeue(out var job))
            {
                if (cancelled)
                    return;
                if (!ctx.Token.IsCancellationRequested)
                {
                    try
                    {
                        runningJob = job;
                        if (job.TimeoutSecs != null)
                            defaultTimeOutSecs = job.TimeoutSecs.Value;
                        
                        if (job.Attempts > 1)
                            Interlocked.Increment(ref retries);

                        lastRunStarted = DateTime.UtcNow;
                        await ctx.Jobs.ExecuteJobAsync(job);
                        Interlocked.Increment(ref completed);
                    }
                    catch
                    {
                        Interlocked.Increment(ref failed);
                        throw;
                    }
                    finally
                    {
                        lastRunStarted = null;
                        runningJob = null;
                    }
                }
            }
        }
        finally
        {
            Interlocked.Decrement(ref running);
        }
    }

    ~DbJobsWorker()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                var timeoutMs = defaultTimeOutSecs * 1000;
                workerCts.CancelAfter(timeoutMs);
                try
                {
                    bgTask?.Wait(defaultTimeOutSecs); // Wait for the task to complete
                }
                catch (Exception e)
                {
                    LogManager.GetLogger(GetType())
                        .Error($"BackgroundJobsWorker dispose error: {e.Message}", e);
                }
                finally
                {
                    workerCts.Dispose();
                    // No longer required to dispose of tasks
                    // bgTask?.Dispose();
                }
            }
            // No unmanaged resources to clean up
            disposed = true;
        }
    }
}
#endif