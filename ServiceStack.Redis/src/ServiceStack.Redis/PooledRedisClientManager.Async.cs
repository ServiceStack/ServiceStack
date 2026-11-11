//
// https://github.com/ServiceStack/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using ServiceStack.Caching;
using ServiceStack.Redis.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.AsyncEx;

namespace ServiceStack.Redis;

public partial class PooledRedisClientManager
    : IRedisClientsManagerAsync
{
    /// <summary>
    /// Use previous client resolving behavior
    /// </summary>
    public static bool UseGetClientBlocking = false;

    public Func<IRedisNativeClientAsync, CancellationToken, ValueTask> ConnectionFilterAsync { get; set; }

    private void ConfigureAsyncClient(RedisClient client)
    {
        client.ConnectionFilter = ConnectionFilter;
        client.ConnectionFilterAsync = ConnectionFilterAsync;
        InitClient(client, deferDb: true);
    }

    private async ValueTask InitClientAsync(RedisClient client, CancellationToken token)
    {
        ConfigureAsyncClient(client);
        if (Db != null && client.Db != Db)
            await ((IRedisClientAsync)client).SelectAsync(Db.Value, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
    }

    private async ValueTask InitNewClientAsync(RedisClient client, CancellationToken token)
    {
        client.Id = Interlocked.Increment(ref RedisClientCounter);
        client.Activate(newClient: true);
        client.ClientManager = this;
        await InitClientAsync(client, token).ConfigureAwait(false);
    }
        
    ValueTask<ICacheClientAsync> IRedisClientsManagerAsync.GetCacheClientAsync(CancellationToken token)
        => new RedisClientManagerCacheClient(this).AsValueTaskResult<ICacheClientAsync>();

    ValueTask<IRedisClientAsync> IRedisClientsManagerAsync.GetClientAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return UseGetClientBlocking
            ? GetClientBlocking().AsValueTaskResult<IRedisClientAsync>()
            : GetClientAsync(token);
    }

    ValueTask<ICacheClientAsync> IRedisClientsManagerAsync.GetReadOnlyCacheClientAsync(CancellationToken token)
        => new RedisClientManagerCacheClient(this) { ReadOnly = true }.AsValueTaskResult<ICacheClientAsync>();

    ValueTask<IRedisClientAsync> IRedisClientsManagerAsync.GetReadOnlyClientAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return UseGetClientBlocking
            ? GetReadOnlyClientBlocking().AsValueTaskResult<IRedisClientAsync>()
            : GetReadOnlyClientAsync(token);
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        Dispose();
        return default;
    }

    // FIFO waiters: each released slot wakes one caller; failover and disposal wake all.
    private readonly RedisPoolWaiters readWaiters = new();
    private readonly RedisPoolWaiters writeWaiters = new();

    partial void PulseAllReadAsync() => readWaiters.WakeAll();
    partial void PulseAllWriteAsync() => writeWaiters.WakeAll();
    private void WakeOneRead() => readWaiters.WakeOne();
    private void WakeOneWrite() => writeWaiters.WakeOne();

    private ValueTask<IRedisClientAsync> GetClientAsync(CancellationToken token)
        => GetPooledClientAsync(false, token);

    private ValueTask<IRedisClientAsync> GetReadOnlyClientAsync(CancellationToken token)
        => GetPooledClientAsync(true, token);

    private async ValueTask<IRedisClientAsync> GetPooledClientAsync(bool reader, CancellationToken token, bool forAsync = true)
    {
        var startedAt = Stopwatch.GetTimestamp();
        var timeout = PoolTimeout;
        var pool = reader ? readClients : writeClients;
        var reservations = reader ? readReservations : writeReservations;
        var waiters = reader ? readWaiters : writeWaiters;
        var woken = false;
        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                RedisClient reused, created = null;
                RedisPoolReservation reservation;
                LinkedListNode<TaskCompletionSource<bool>> waiter = null;
                int index, generation;
                lock (pool)
                {
                    if (reader) AssertValidReadOnlyPool(); else AssertValidReadWritePool();
                    generation = reader ? readGeneration : writeGeneration;
                    index = reader ? GetInActiveReadClient(out reused) : GetInActiveWriteClient(out reused);
                    reservation = index >= 0 ? reservations[index] : null;
                    if (reused != null) reused.Activate();
                    if (index < 0)
                    {
                        waiter = waiters.Enqueue(front: woken);
                        woken = false; // the queued waiter now owns any wakeup (Wait/WaitAsync abandon it)
                    }
                }

                if (waiter != null)
                {
                    // Sync callers must block on the signal itself, never on an async continuation.
                    if (forAsync) await waiters.WaitAsync(pool, waiter, startedAt, timeout, PoolTimeoutError, token).ConfigureAwait(false);
                    else waiters.Wait(pool, waiter, startedAt, timeout, PoolTimeoutError);
                    woken = true;
                    continue;
                }

                var handedOff = false;
                try
                {
                    if (reused != null)
                    {
                        if (forAsync) await InitClientAsync(reused, token).ConfigureAwait(false);
                        else InitClient(reused);
                    }
                    else
                    {
                        if (reservation.Replaced != null) RedisState.DeactivateClient(reservation.Replaced);
                        token.ThrowIfCancellationRequested();
                        if (forAsync)
                        {
                            created = reader
                                ? await RedisResolver.CreateSlaveClientAsync(index, token, ConfigureAsyncClient).ConfigureAwait(false)
                                : await RedisResolver.CreateMasterClientAsync(index, token, ConfigureAsyncClient).ConfigureAwait(false);
                            await InitNewClientAsync(created, token).ConfigureAwait(false);
                        }
                        else
                        {
                            created = reader ? RedisResolver.CreateSlaveClient(index) : RedisResolver.CreateMasterClient(index);
                            InitNewClient(created);
                        }
                    }

                    lock (pool)
                    {
                        if (reader) AssertValidReadOnlyPool(); else AssertValidReadWritePool();
                        token.ThrowIfCancellationRequested();
                        // Never hand out a creation/reinitialized lease from an obsolete topology.
                        if ((reservation?.Generation ?? generation) != (reader ? readGeneration : writeGeneration)
                            || (reused != null ? pool[index] != reused : reservations[index] != reservation))
                            continue;
                        if (created != null) { pool[index] = created; reservations[index] = null; }
                        if (reader) ReadPoolIndex++; else WritePoolIndex++;
                        handedOff = true;
                        return reused ?? created;
                    }
                }
                catch (Exception ex) when (reused != null && (ex is RedisException || ex is ObjectDisposedException))
                {
                    lock (pool)
                    {
                        token.ThrowIfCancellationRequested();
                        if (reader) AssertValidReadOnlyPool(); else AssertValidReadWritePool();
                        if (generation == (reader ? readGeneration : writeGeneration)) throw;
                    }
                    // Failover can close a connection while SELECT resets a reused lease.
                    continue;
                }
                finally
                {
                    if (!handedOff)
                    {
                        try
                        {
                            var abandoned = reused ?? created;
                            if (abandoned != null)
                            {
                                abandoned.AbortConnection();
                                abandoned.ClientManager = null;
                                abandoned.Dispose();
                            }
                        }
                        finally
                        {
                            lock (pool)
                            {
                                if (reused != null ? pool[index] == reused : reservations[index] == reservation)
                                { pool[index] = null; reservations[index] = null; }
                                if (reader) WakeOneRead(); else WakeOneWrite();
                            }
                        }
                    }
                }
            }
        }
        catch when (woken)
        {
            // Woken, then failed before acquiring (e.g. cancellation): pass the wakeup on so it isn't lost.
            // A spurious wakeup only makes the next waiter recheck; a lost one strands it until PoolTimeout.
            lock (pool) waiters.WakeOne();
            throw;
        }
        finally { RedisState.DisposeExpiredClients(); }
    }
}
