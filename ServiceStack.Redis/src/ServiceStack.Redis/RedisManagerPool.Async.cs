//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using ServiceStack.Caching;
using ServiceStack.Redis.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis;

public partial class RedisManagerPool
    : IRedisClientsManagerAsync
{
    ValueTask<ICacheClientAsync> IRedisClientsManagerAsync.GetCacheClientAsync(CancellationToken token)
        => new RedisClientManagerCacheClient(this).AsValueTaskResult<ICacheClientAsync>();

    ValueTask<IRedisClientAsync> IRedisClientsManagerAsync.GetClientAsync(CancellationToken token)
        => GetClientAsync(token);

    ValueTask<ICacheClientAsync> IRedisClientsManagerAsync.GetReadOnlyCacheClientAsync(CancellationToken token)
        => new RedisClientManagerCacheClient(this) { ReadOnly = true }.AsValueTaskResult<ICacheClientAsync>();

    ValueTask<IRedisClientAsync> IRedisClientsManagerAsync.GetReadOnlyClientAsync(CancellationToken token)
        => GetClientAsync(token);

    public Func<IRedisNativeClientAsync, CancellationToken, ValueTask> ConnectionFilterAsync { get; set; }

    private void ConfigureAsyncClient(RedisClient client)
    {
        client.ConnectionFilter = ConnectionFilter;
        client.ConnectionFilterAsync = ConnectionFilterAsync;
    }

    private ValueTask<IRedisClientAsync> GetClientAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return MaxLiveConnections != null ? GetAdmittedClientAsInterfaceAsync(token) : GetUnboundedClientAsync(token);
    }

    private async ValueTask<IRedisClientAsync> GetAdmittedClientAsInterfaceAsync(CancellationToken token)
        => await GetAdmittedClientAsync(token).ConfigureAwait(false);

    private async ValueTask<IRedisClientAsync> GetUnboundedClientAsync(CancellationToken token, bool forAsync = true)
    {
        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                RedisPoolReservation reservation;
                RedisClient created = null;
                int index, generation;
                lock (clients)
                {
                    AssertValidPool();
                    if (Volatile.Read(ref disposeAttempts) != 0) throw new ObjectDisposedException(nameof(RedisManagerPool));
                    generation = poolGeneration;
                    index = GetInActiveClient(out var inactive);
                    if (inactive != null)
                    {
                        token.ThrowIfCancellationRequested();
                        inactive.Activate();
                        poolIndex++;
                        return inactive;
                    }
                    reservation = index >= 0 ? reservations[index] : null;
                }
                var handedOff = false;
                try
                {
                    if (reservation?.Replaced != null) RedisState.DeactivateClient(reservation.Replaced);
                    created = forAsync
                        ? await RedisResolver.CreateMasterClientAsync(Math.Max(index, 0), token, ConfigureAsyncClient).ConfigureAwait(false)
                        : RedisResolver.CreateMasterClient(Math.Max(index, 0));
                    InitNewClient(created);
                    lock (clients)
                    {
                        if (Volatile.Read(ref disposeAttempts) != 0) throw new ObjectDisposedException(nameof(RedisManagerPool));
                        token.ThrowIfCancellationRequested();
                        if ((reservation?.Generation ?? generation) != poolGeneration || (index >= 0 && reservations[index] != reservation)) continue;
                        if (index < 0)
                        {
                            Interlocked.Increment(ref RedisState.TotalClientsCreatedOutsidePool);
                            created.ClientManager = null;
                        }
                        else
                        {
                            poolIndex++;
                            clients[index] = created;
                            reservations[index] = null;
                        }
                        handedOff = true;
                        return created;
                    }
                }
                finally
                {
                    if (!handedOff)
                    {
                        try
                        {
                            if (created != null) { created.AbortConnection(); created.ClientManager = null; created.Dispose(); }
                        }
                        finally
                        {
                            lock (clients)
                            {
                                if (index >= 0 && reservations[index] == reservation)
                                { clients[index] = null; reservations[index] = null; }
                            }
                        }
                    }
                }
            }
        }
        finally { RedisState.DisposeExpiredClients(); }
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        Dispose();
        return default;
    }
}