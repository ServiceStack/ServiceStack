using ServiceStack.Text;
using System.Collections.Concurrent;

namespace ServiceStack.AI;

/// <summary>
/// Coordinates background chat tasks and the long-poll update channel that delivers streamed
/// thread updates to the browser (port of active_chat_tasks/thread_update_events in extensions/app).
/// </summary>
public class ThreadUpdates
{
    readonly ConcurrentDictionary<long, CancellationTokenSource> activeChatTasks = new();
    readonly ConcurrentDictionary<long, TaskCompletionSource> updateEvents = new();
    readonly ConcurrentDictionary<long, SemaphoreSlim> threadLocks = new();

    /// <summary>How long GET threads/{id}/updates waits before returning the unchanged thread</summary>
    public TimeSpan LongPollTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public CancellationTokenSource StartChatTask(long threadId)
    {
        var cts = new CancellationTokenSource();
        activeChatTasks.AddOrUpdate(threadId, cts, (_, existing) =>
        {
            existing.Cancel();
            existing.Dispose();
            return cts;
        });
        return cts;
    }

    public void CompleteChatTask(long threadId, CancellationTokenSource cts)
    {
        if (activeChatTasks.TryGetValue(threadId, out var current) && ReferenceEquals(current, cts))
        {
            activeChatTasks.TryRemove(threadId, out _);
        }
        cts.Dispose();
    }

    /// <summary>Cancel a running chat. Returns true when a task was actually cancelled.</summary>
    public bool CancelChatTask(long threadId)
    {
        if (!activeChatTasks.TryRemove(threadId, out var cts))
            return false;
        try
        {
            cts.Cancel();
        }
        catch (ObjectDisposedException) { /* already completed */ }
        return true;
    }

    /// <summary>Wake any long-poll waiters for this thread (port of notify_thread_update)</summary>
    public void NotifyThreadUpdate(long threadId)
    {
        // complete the current signal and install a fresh one so the next NextSignalAsync blocks again
        if (updateEvents.TryGetValue(threadId, out var tcs))
        {
            updateEvents.TryUpdate(threadId, NewSignal(), tcs);
            tcs.TrySetResult();
        }
    }

    /// <summary>
    /// A task that completes on the next NotifyThreadUpdate for this thread. Callers register the
    /// signal <em>before</em> re-reading state so an update can't slip through between the two.
    /// </summary>
    public Task NextSignalAsync(long threadId) =>
        updateEvents.GetOrAdd(threadId, _ => NewSignal()).Task;

    static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Serializes read-modify-write updates of a thread's messages: the streaming writer and
    /// request handlers can otherwise interleave and lose messages.
    /// </summary>
    public async Task<IDisposable> LockThreadAsync(long threadId, CancellationToken token = default)
    {
        var semaphore = threadLocks.GetOrAdd(threadId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(token).ConfigAwait();
        return new Releaser(semaphore);
    }

    sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }
}
