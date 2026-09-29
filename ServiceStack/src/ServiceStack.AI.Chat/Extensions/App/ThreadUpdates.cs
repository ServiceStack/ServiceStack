using ServiceStack.Text;
using System.Collections.Concurrent;

namespace ServiceStack.AI;

/// <summary>
/// Coordinates thread update signals and serializes per-thread read/modify/write operations.
/// </summary>
public class ThreadUpdates
{
    readonly ConcurrentDictionary<long, TaskCompletionSource> updateEvents = new();
    readonly ConcurrentDictionary<long, SemaphoreSlim> threadLocks = new();

    /// <summary>How long GET threads/{id}/updates waits before returning the unchanged thread</summary>
    public TimeSpan LongPollTimeout { get; set; } = TimeSpan.FromSeconds(10);

    TaskCompletionSource sidebarSignal = NewSignal();

    /// <summary>Cached sidebar revision per user, cleared by <see cref="NotifySidebar"/></summary>
    public ConcurrentDictionary<string, string> SidebarRevisions { get; } = new();

    /// <summary>
    /// Something the project sidebar shows may have changed (threads, runs, titles, projects).
    /// Sidebar subscribers recompute a user's revision only after this, so an idle sidebar costs no
    /// database queries (port of llms-py's SidebarSignal).
    /// </summary>
    public void NotifySidebar()
    {
        SidebarRevisions.Clear();
        Interlocked.Exchange(ref sidebarSignal, NewSignal()).TrySetResult();
    }

    /// <summary>Completes on the next <see cref="NotifySidebar"/>; capture before reading state</summary>
    public Task NextSidebarSignalAsync() => Volatile.Read(ref sidebarSignal).Task;

    /// <summary>Wake any long-poll waiters for this thread (port of notify_thread_update)</summary>
    public void NotifyThreadUpdate(long threadId)
    {
        // title, membership, activity and run changes all arrive here
        NotifySidebar();
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
