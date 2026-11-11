using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.AsyncEx;

namespace ServiceStack.Redis;

public partial class RedisManagerPool
{
    /// <summary>Optional per-manager live-client limit, including creation/closing reservations. Null preserves unbounded overflow.</summary>
    public int? MaxLiveConnections { get; }

    /// <summary>Maximum acquisitions allowed to wait at saturation. Zero fails immediately.</summary>
    public int MaxWaitingClients { get; }

    /// <summary>One timeout for pool waiting across wakeups, in milliseconds. Null or -1 waits indefinitely.</summary>
    public int? PoolTimeout { get; }

    private sealed class AdmittedClient
    {
        public int Generation;
        public bool InUse = true;
        public bool Closing;
    }

    // All admission state uses the existing clients lock. Capacity is released only after close completes.
    private readonly Dictionary<RedisClient, AdmittedClient> admittedClients = new();
    private readonly RedisPoolWaiters admissionWaiters = new();
    private int liveClients, creatingClients, waitingClients, admissionGeneration;

    private void AssertAdmissionOpen()
    {
        if (Volatile.Read(ref disposeAttempts) != 0)
            throw new ObjectDisposedException(nameof(RedisManagerPool));
        AssertValidPool();
    }

    // One released slot or idle client wakes one waiter; failover and disposal wake all.
    private void WakeOneAdmission() => admissionWaiters.WakeOne();
    private void WakeAllAdmission() => admissionWaiters.WakeAll();

    private void MarkClosing(RedisClient client, AdmittedClient state)
    {
        state.Closing = true;
        for (var i = 0; i < clients.Length; i++)
            if (clients[i] == client)
                clients[i] = null;
    }

    private void CloseAdmissionConnection(RedisClient client)
    {
        client.ClientManager = null;
        try
        {
            client.Dispose();
        }
        catch (Exception e)
        {
            Log.Error($"Error disposing Redis client to {client.GetHostString()}", e);
        }
        finally
        {
            Dispose(client);
        } // Also close the native socket if a custom Dispose override failed.
    }

    private void CloseAdmittedClient(RedisClient client)
    {
        try
        {
            CloseAdmissionConnection(client);
        }
        finally
        {
            lock (clients)
            {
                if (admittedClients.Remove(client)) liveClients--;
                WakeOneAdmission();
            }
        }
    }

    private async ValueTask<RedisClient> GetAdmittedClientAsync(CancellationToken token, bool forAsync = true)
    {
        var started = Stopwatch.GetTimestamp();
        var waiting = false;
        var woken = false;
        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                LinkedListNode<TaskCompletionSource<bool>> waiter = null;
                RedisClient toClose = null;
                var generation = 0;
                var create = false;
                var hostIndex = 0;
                lock (clients)
                {
                    AssertAdmissionOpen();
                    token.ThrowIfCancellationRequested();
                    for (var n = 0; n < clients.Length; n++)
                    {
                        var i = (int)((uint)poolIndex % (uint)clients.Length + (uint)n) % clients.Length;
                        var client = clients[i];
                        if (client == null) continue;
                        var state = admittedClients[client];
                        if (state.InUse || state.Closing) continue;
                        if (client.HadExceptions || client.IsDisposed)
                        {
                            MarkClosing(client, state);
                            toClose = client;
                            break;
                        }

                        try
                        {
                            client.Activate();
                            token.ThrowIfCancellationRequested();
                        }
                        catch
                        {
                            client.Deactivate();
                            WakeOneAdmission();
                            throw;
                        }

                        state.InUse = true;
                        if (waiting)
                        {
                            waitingClients--;
                            waiting = false;
                        }

                        poolIndex++;
                        return client;
                    }

                    if (toClose == null)
                    {
                        if (liveClients < MaxLiveConnections.Value)
                        {
                            if (RedisResolver.ReadWriteHostsCount < 1)
                                throw new InvalidOperationException("Need at least one Redis host.");
                            if (waiting)
                            {
                                waitingClients--;
                                waiting = false;
                            }

                            liveClients++;
                            creatingClients++;
                            generation = admissionGeneration;
                            hostIndex = (int)((uint)poolIndex++ % (uint)RedisResolver.ReadWriteHostsCount);
                            create = true;
                        }
                        else
                        {
                            if (!waiting)
                            {
                                if (waitingClients >= MaxWaitingClients)
                                    throw new InvalidOperationException(
                                        "RedisManagerPool admission limit reached; no waiting capacity is available.");
                                waitingClients++;
                                waiting = true;
                            }

                            waiter = admissionWaiters.Enqueue(front: woken);
                            woken = false; // the queued waiter now owns any wakeup (Wait/WaitAsync abandon it)
                        }
                    }
                }

                if (toClose != null)
                {
                    CloseAdmittedClient(toClose);
                    continue;
                }

                if (!create)
                {
                    // Sync callers must block on the signal itself, never on an async continuation.
                    if (forAsync)
                        await admissionWaiters.WaitAsync(clients, waiter, started, PoolTimeout, PoolTimeoutError, token)
                            .ConfigureAwait(false);
                    else admissionWaiters.Wait(clients, waiter, started, PoolTimeout, PoolTimeoutError);
                    woken = true;
                    continue;
                }

                RedisClient created = null;
                var handedOff = false;
                try
                {
                    token.ThrowIfCancellationRequested();
                    created = forAsync
                        ? await RedisResolver.CreateMasterClientAsync(hostIndex, token, ConfigureAsyncClient)
                            .ConfigureAwait(false)
                        : RedisResolver.CreateMasterClient(hostIndex);
                    InitNewClient(created);
                    token.ThrowIfCancellationRequested();
                    lock (clients)
                    {
                        AssertAdmissionOpen();
                        token.ThrowIfCancellationRequested();
                        // A connection created against the previous topology is never leased after failover.
                        if (generation != admissionGeneration) continue;
                        admittedClients.Add(created, new AdmittedClient { Generation = generation });
                        for (var i = 0; i < clients.Length; i++)
                        {
                            if (clients[i] != null) continue;
                            clients[i] = created;
                            break;
                        }

                        handedOff = true;
                        return created;
                    }
                }
                finally
                {
                    try
                    {
                        if (!handedOff && created != null)
                        {
                            if (forAsync) created.AbortConnection();
                            CloseAdmissionConnection(created);
                        }
                    }
                    finally
                    {
                        lock (clients)
                        {
                            creatingClients--;
                            if (!handedOff)
                            {
                                liveClients--;
                                WakeOneAdmission();
                            }
                        }
                    }
                }
            }
        }
        catch when (woken)
        {
            // Woken, then failed before acquiring (e.g. cancellation): pass the wakeup on so it isn't lost.
            lock (clients) admissionWaiters.WakeOne();
            throw;
        }
        finally
        {
            if (waiting)
            {
                lock (clients) waitingClients--;
            }
        }
    }

    private void ReturnAdmittedClient(RedisNativeClient returned)
    {
        RedisClient toClose = null;
        lock (clients)
        {
            if (returned is not RedisClient client || !admittedClients.TryGetValue(client, out var state)
                                                   || state.Closing || !state.InUse) return;
            state.InUse = false;
            client.TrackThread = null;
            client.Deactivate();
            if (disposeAttempts != 0 || state.Generation != admissionGeneration || client.HadExceptions
                || client.IsDisposed || Array.IndexOf(clients, client) < 0)
            {
                MarkClosing(client, state);
                toClose = client;
            }
            else WakeOneAdmission();
        }

        if (toClose != null) CloseAdmittedClient(toClose);
    }

    private void FailoverAdmission(string[] hosts)
    {
        List<RedisClient> idle;
        lock (clients)
        {
            AssertAdmissionOpen();
            RedisResolver.ResetMasters(hosts);
            admissionGeneration++;
            idle = admittedClients.Where(x => !x.Value.InUse && !x.Value.Closing).Select(x => x.Key).ToList();
            foreach (var client in idle) MarkClosing(client, admittedClients[client]);
            Array.Clear(clients, 0, clients.Length);
            WakeAllAdmission();
        }

        foreach (var client in idle) CloseAdmittedClient(client);
    }

    private void DisposeAdmission()
    {
        List<RedisClient> owned;
        lock (clients)
        {
            owned = admittedClients.Where(x => !x.Value.Closing).Select(x => x.Key).ToList();
            foreach (var client in owned) MarkClosing(client, admittedClients[client]);
            WakeAllAdmission();
        }

        foreach (var client in owned) CloseAdmittedClient(client);
    }

    private void AddAdmissionStats(Dictionary<string, string> stats)
    {
        if (MaxLiveConnections == null) return;
        lock (clients)
        {
            stats["clientsLive"] = liveClients.ToString();
            stats["clientsCreating"] = creatingClients.ToString();
            stats["clientsWaiting"] = waitingClients.ToString();
            stats["clientsMaxLive"] = MaxLiveConnections.Value.ToString();
            stats["clientsMaxWaiting"] = MaxWaitingClients.ToString();
        }
    }
}