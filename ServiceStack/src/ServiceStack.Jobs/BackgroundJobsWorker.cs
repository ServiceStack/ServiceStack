using System.Collections.Concurrent;

namespace ServiceStack.Jobs;

public class BackgroundJobsWorker : IDisposable
{
    public string? Name { get; set; }
    public ConcurrentQueue<BackgroundJob> Queue { get; } = new();
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
        
        if (transient)
            Dispose();
    }

    public void Dispose()
    {
        workerCts.Dispose();
        bgTask?.Dispose();
    }
}
