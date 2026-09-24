#if NET8_0_OR_GREATER
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Jobs;
using ServiceStack.Logging;

namespace ServiceStack;

public class DbJobsWorker : IDisposable
{
    public string? Name { get; set; }
    /// <summary>Pending Jobs ordered by highest Priority first, then FIFO within the same Priority</summary>
    private readonly PriorityQueue<BackgroundJob, (int NegPriority, long Seq)> queue = new();
    /// <summary>Ids of the queued Jobs, so checking whether a Job is already queued stays O(1)</summary>
    private readonly HashSet<long> queuedIds = new();
    private long queuedSeq = 0;
    public int QueuedCount
    {
        get { lock (queueSync) return queue.Count; }
    }
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
    private readonly object queueSync = new();
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
        Queued = QueuedCount,
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
        lock (queueSync)
        {
            queue.Enqueue(job, (-job.Priority, queuedSeq++));
            queuedIds.Add(job.Id);
        }
        StartProcessing();
    }

    public List<BackgroundJob> DrainPending()
    {
        var pending = new List<BackgroundJob>();
        lock (queueSync)
        {
            while (queue.TryDequeue(out var job, out _))
                pending.Add(job);
            queuedIds.Clear();
        }
        return pending;
    }

    private bool TryDequeueHighestPriority(out BackgroundJob? selected)
    {
        lock (queueSync)
        {
            if (!queue.TryDequeue(out selected, out _))
                return false;
            queuedIds.Remove(selected.Id);
            return true;
        }
    }

    public List<BackgroundJob> GetQueuedJobs()
    {
        lock (queueSync)
            return queue.UnorderedItems.Select(x => x.Element).ToList();
    }

    private void StartProcessing()
    {
        if (cancelled || ct.IsCancellationRequested)
            return;
        if (Interlocked.CompareExchange(ref running, 1, 0) == 0)
        {
            Interlocked.Increment(ref tasksStarted);
            bgTask = Task.Factory.StartNew(RunAsync, new JobWorkerContext(jobs, ct),
                CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
        }
    }

    public bool HasJobQueued(long jobId)
    {
        if (runningJob?.Id == jobId)
            return true;
        lock (queueSync)
            return queuedIds.Contains(jobId);
    }

    /// <summary>Jobs this worker holds a lease for, i.e. that it needs to keep renewing</summary>
    public List<BackgroundJob> GetLeasedJobs()
    {
        var to = GetQueuedJobs();
        var running = runningJob;
        if (running != null)
            to.Add(running);
        return to;
    }


    record class JobWorkerContext(IBackgroundJobs Jobs, CancellationToken Token);

    // Runs on Worker Thread
    private async Task RunAsync(object? state)
    {
        try
        {
            // Runs all jobs in the queue, then exits
            var ctx = (JobWorkerContext)state!;
            while (TryDequeueHighestPriority(out var job))
            {
                if (job == null)
                    continue;
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
            Interlocked.Exchange(ref running, 0);
            if (QueuedCount > 0)
                StartProcessing();
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
                    bgTask?.Wait(timeoutMs); // Wait for the task to complete
                }
                catch (Exception e)
                {
                    LogManager.GetLogger(GetType())
                        .Error($"DbJobsWorker dispose error: {e.Message}", e);
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
