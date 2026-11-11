using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.AsyncEx;

namespace ServiceStack.Redis;

/// <summary>
/// FIFO queue of callers waiting for pool capacity. Each released slot wakes one waiter instead of every waiter.
/// All instance members must be called while holding the owning pool's lock.
/// </summary>
internal sealed class RedisPoolWaiters
{
    private readonly LinkedList<TaskCompletionSource<bool>> queue = new();

    public int Count => queue.Count;

    /// <summary>Queue a waiter. A waiter that was already woken once re-queues at the front to keep its place.</summary>
    public LinkedListNode<TaskCompletionSource<bool>> Enqueue(bool front)
    {
        var signal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        return front ? queue.AddFirst(signal) : queue.AddLast(signal);
    }

    public void WakeOne()
    {
        var node = queue.First;
        if (node == null) return;
        queue.RemoveFirst();
        node.Value.TrySetResult(true);
    }

    public void WakeAll()
    {
        while (queue.First != null) WakeOne();
    }

    /// <summary>A waiter stopped without using its wakeup (timeout, cancellation, error): pass any wakeup on.</summary>
    public void Abandon(LinkedListNode<TaskCompletionSource<bool>> node)
    {
        if (node.List != null) queue.Remove(node);
        else WakeOne();
    }

    /// <summary>
    /// Wait synchronously. The releasing thread completes the signal directly, so waking a blocked caller never
    /// depends on a free thread-pool thread (sync-over-async waiting could deadlock a saturated pool).
    /// </summary>
    public void Wait(object poolLock, LinkedListNode<TaskCompletionSource<bool>> node, long startedAt, int? timeout,
        string timeoutError)
    {
        try
        {
            if (timeout == null || timeout == Timeout.Infinite)
            {
                node.Value.Task.Wait();
                return;
            }

            var remaining = timeout.Value - ElapsedMs(startedAt);
            if (remaining <= 0 || !node.Value.Task.Wait((int)remaining))
                throw new TimeoutException(timeoutError);
        }
        catch
        {
            lock (poolLock) Abandon(node);
            throw;
        }
    }

    public async Task WaitAsync(object poolLock, LinkedListNode<TaskCompletionSource<bool>> node, long startedAt,
        int? timeout, string timeoutError, CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            if (timeout == null || timeout == Timeout.Infinite)
            {
                await node.Value.Task.WaitAsync(token).ConfigureAwait(false);
                return;
            }

            var remaining = timeout.Value - ElapsedMs(startedAt);
            if (remaining <= 0) throw new TimeoutException(timeoutError);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter((int)remaining);
            try
            {
                await node.Value.Task.WaitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                token.ThrowIfCancellationRequested();
                throw new TimeoutException(timeoutError);
            }
        }
        catch
        {
            lock (poolLock) Abandon(node);
            throw;
        }
    }

    public static long ElapsedMs(long startedAt) => (Stopwatch.GetTimestamp() - startedAt) * 1000 / Stopwatch.Frequency;

    /// <summary>
    /// Sync acquisition shares the async implementation but never awaits, so its ValueTask is already complete.
    /// Never block on an incomplete ValueTask.
    /// </summary>
    public static T Completed<T>(ValueTask<T> task) =>
        task.IsCompleted ? task.GetAwaiter().GetResult() : task.AsTask().GetAwaiter().GetResult();
}
