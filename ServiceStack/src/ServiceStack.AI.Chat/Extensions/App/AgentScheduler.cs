using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.AI;

/// <summary>
/// Bounded in-process async scheduler. The RDBMS is authoritative; the wake signal only avoids
/// querying an empty queue every second. No dedicated OS thread or worker process is used.
/// </summary>
public sealed class AgentScheduler(
    ChatDb db,
    Func<AgentRun, IRequest?, CancellationToken, Task> executeSlice,
    ThreadUpdates updates,
    ILogger log,
    int maxConcurrency = 2,
    double pollSeconds = 1,
    int leaseSeconds = 300) : IDisposable
{
    readonly string owner = $"{Environment.ProcessId}:{Guid.NewGuid():N}";
    readonly ConcurrentDictionary<long, Task> active = new();
    readonly ConcurrentDictionary<long, CancellationTokenSource> cancellations = new();
    readonly ConcurrentDictionary<long, IRequest> requests = new();
    readonly SemaphoreSlim wake = new(0, 1);
    readonly CancellationTokenSource stopping = new();
    Task? coordinator;

    public void Start()
    {
        if (coordinator is { IsCompleted: false }) return;
        db.RequeueInterruptedAgentRuns();
        coordinator = RunAsync(stopping.Token);
        Wake(); // claim rows recovered from a previous process without waiting for a new HTTP enqueue
    }

    public void Enqueue(long runId, IRequest? request = null)
    {
        if (request != null) requests[runId] = request;
        Start();
        Wake();
    }

    public void Wake()
    {
        if (wake.CurrentCount == 0)
        {
            try { wake.Release(); } catch (SemaphoreFullException) { }
        }
    }

    public void Cancel(long runId)
    {
        if (cancellations.TryGetValue(runId, out var cts)) cts.Cancel();
        Wake();
    }

    async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            foreach (var entry in active.ToArray())
            {
                if (entry.Value.IsCompleted) active.TryRemove(entry.Key, out _);
            }

            var capacity = Math.Max(1, maxConcurrency) - active.Count;
            if (capacity > 0)
            {
                foreach (var run in db.ClaimAgentRuns(owner, capacity, Math.Max(30, leaseSeconds)))
                {
                    var task = RunClaimedAsync(run, token);
                    active[run.Id] = task;
                }
            }

            try
            {
                if (active.Count > 0)
                    await wake.WaitAsync(TimeSpan.FromSeconds(Math.Max(.1, pollSeconds)), token).ConfigAwait();
                else
                    await wake.WaitAsync(token).ConfigAwait();
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
        }
    }

    async Task RunClaimedAsync(AgentRun run, CancellationToken schedulerToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(schedulerToken);
        cancellations[run.Id] = cts;
        var heartbeat = RenewLeaseAsync(run.Id, cts.Token);
        try
        {
            requests.TryRemove(run.Id, out var request);
            await executeSlice(run, request, cts.Token).ConfigAwait();
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            var current = db.GetAgentRun(run.Id, ChatDb.AllUsers);
            if (current?.Status == AgentRunStatus.Running)
            {
                current.Status = AgentRunStatus.Queued;
                current.LeaseOwner = null;
                current.LeaseExpiresAt = null;
                db.UpdateAgentRun(current);
            }
        }
        catch (Exception e)
        {
            var current = db.GetAgentRun(run.Id, ChatDb.AllUsers);
            if (current?.Status == AgentRunStatus.Running)
            {
                var error = ChatJson.ToErrorMessage(e);
                current.Status = AgentRunStatus.Failed;
                current.Error = error;
                current.CompletedAt = DateTime.Now;
                current.LeaseOwner = null;
                current.LeaseExpiresAt = null;
                db.UpdateAgentRun(current);
                await updates.UpdateThreadTerminalAsync(db, run.ThreadId, run.User, error).ConfigAwait();
            }
            log.LogError(e, "Agent run {RunId} failed", run.Id);
        }
        finally
        {
            cts.Cancel();
            try { await heartbeat.ConfigAwait(); } catch (OperationCanceledException) { }
            cancellations.TryRemove(run.Id, out _);
            active.TryRemove(run.Id, out _);
            updates.NotifyThreadUpdate(run.ThreadId);
            Wake();
        }
    }

    async Task RenewLeaseAsync(long runId, CancellationToken token)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(10, leaseSeconds / 3d));
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(interval, token).ConfigAwait();
            if (!db.RenewAgentRunLease(runId, owner, leaseSeconds)) return;
        }
    }

    public void Dispose()
    {
        stopping.Cancel();
        foreach (var cts in cancellations.Values) cts.Cancel();
        try { Task.WhenAll(active.Values.Append(coordinator ?? Task.CompletedTask)).Wait(TimeSpan.FromSeconds(10)); }
        catch { }
        foreach (var runId in active.Keys)
        {
            var run = db.GetAgentRun(runId, ChatDb.AllUsers);
            if (run?.Status != AgentRunStatus.Running) continue;
            run.Status = AgentRunStatus.Queued;
            run.LeaseOwner = null;
            run.LeaseExpiresAt = null;
            db.UpdateAgentRun(run);
        }
        stopping.Dispose();
        wake.Dispose();
    }
}

public static class ThreadUpdatesDurableExtensions
{
    public static Task UpdateThreadTerminalAsync(this ThreadUpdates updates, ChatDb db,
        long threadId, string? user, string error)
    {
        var row = db.GetThread(threadId, user, includeMessages: false);
        if (row != null)
        {
            row.Error = error;
            row.Status = null;
            row.CompletedAt = DateTime.Now;
            row.StreamingMessage = null;
            row.UpdatedAt = DateTime.Now;
            db.UpdateThreadFields(row,
                [nameof(ChatThread.Error), nameof(ChatThread.Status), nameof(ChatThread.CompletedAt),
                    nameof(ChatThread.StreamingMessage), nameof(ChatThread.UpdatedAt)], user);
        }
        updates.NotifyThreadUpdate(threadId);
        return Task.CompletedTask;
    }
}
