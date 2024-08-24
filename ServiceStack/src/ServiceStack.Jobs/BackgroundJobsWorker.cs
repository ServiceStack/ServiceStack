using System.Collections.Concurrent;
using ServiceStack.Logging;

namespace ServiceStack.Jobs;

public class BackgroundJobsWorker : IDisposable
{
    public string? Name { get; set; }
    public ConcurrentQueue<BackgroundJob> Queue { get; } = new();
    public Task? BackgroundTask => bgTask; 
    private Task? bgTask;
    private long running = 0;
    public bool Running => Interlocked.Read(ref running) == 1;
    DateTime? lastRunStarted = null;
    public TimeSpan? RunningTime => lastRunStarted != null ? DateTime.UtcNow - lastRunStarted.Value : null;
    
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
    private int timeoutMs = 5 * 60 * 1000;

    public BackgroundJobsWorker(IBackgroundJobs jobs, CancellationToken ct, bool transient)
    {
        this.jobs = jobs;
        workerCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        this.ct = workerCts.Token;
        this.transient = transient;
    }

    public WorkerStats GetStats() => new()
    {
        Name = Name ?? "None",
        Queued = Queue.Count,
        Received = received,
        Completed = completed,
        Retries = retries,
        Failed = failed,
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
                        if (job.TimeoutSecs != null)
                            timeoutMs = job.TimeoutSecs.Value * 1000;
                        
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
                    }
                }
            }
        }
        finally
        {
            Interlocked.Decrement(ref running);
        }
    }

    ~BackgroundJobsWorker()
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
                workerCts.Cancel();
                try
                {
                    bgTask?.Wait(timeoutMs); // Wait for the task to complete
                }
                catch (Exception e)
                {
                    LogManager.GetLogger(GetType())
                        .Error($"BackgroundJobsWorker dispose error: {e.Message}", e);
                }
                finally
                {
                    workerCts.Dispose();
                    bgTask?.Dispose();
                }
            }
            // No unmanaged resources to clean up
            disposed = true;
        }
    }
}
