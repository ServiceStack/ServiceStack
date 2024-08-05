using System.Collections.Concurrent;

namespace ServiceStack.Jobs;

public class BackgroundJobsWorker(IBackgroundJobs jobs, CancellationToken ct)
{
    public string? Name { get; set; }
    public ConcurrentQueue<BackgroundJob> Queue { get; } = new();
    private Task? bgTask;
    private long running = 0;
    
    private long tasksStarted = 0; 
    private long received = 0; 
    private long retries = 0;
    private long failed = 0;
    private long completed = 0;

    public WorkerStats GetStats() => new()
    {
        Name = Name ?? "None",
        Queued = Queue.Count,
        Received = received,
        Completed = completed,
        Retries = retries,
        Failed = failed,
    };

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
                if (!ctx.Token.IsCancellationRequested)
                {
                    try
                    {
                        if (job.Attempts > 1)
                            Interlocked.Increment(ref retries);

                        await ctx.Jobs.ExecuteJobAsync(job);
                        Interlocked.Increment(ref completed);
                    }
                    catch
                    {
                        Interlocked.Increment(ref failed);
                        throw;
                    }
                }
            }
        }
        finally
        {
            Interlocked.Decrement(ref running);
        }
    }
}
