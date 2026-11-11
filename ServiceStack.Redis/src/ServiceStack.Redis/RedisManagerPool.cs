//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ServiceStack.Caching;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Redis;

///<summary>
/// Configuration class for the RedisManagerPool
///</summary>
public class RedisPoolConfig
{
    /// <summary>
    /// Default pool size used by every new instance of <see cref="RedisPoolConfig"/>. (default: 40)
    /// </summary>
    public static int DefaultMaxPoolSize = 40;

    public RedisPoolConfig()
    {
        // maybe a bit overkill? could be deprecated if you add max int on RedisManagerPool
        MaxPoolSize = RedisConfig.DefaultMaxPoolSize ?? DefaultMaxPoolSize;
    }

    /// <summary>
    /// Maximum number of clients retained for reuse. Overflow is unbounded unless MaxLiveConnections is set.
    /// </summary>
    public int MaxPoolSize { get; set; }

    /// <summary>Optional per-manager cap on live clients, including creation and closing reservations.</summary>
    public int? MaxLiveConnections { get; set; }

    /// <summary>Maximum acquisitions allowed to wait at the live-client limit. Zero fails immediately.</summary>
    public int MaxWaitingClients { get; set; }

    /// <summary>Pool wait timeout in milliseconds for bounded admission. Null or -1 waits indefinitely.</summary>
    public int? PoolTimeout { get; set; } = 30000;
}

/// <summary>
/// Provides thread-safe pooling of redis client connections. All connections are treated as read and write hosts.
/// </summary>
public partial class RedisManagerPool
    : IRedisClientsManager, IRedisFailover, IHandleClientDispose, IHasRedisResolver, IRedisClientCacheManager, IHasStats
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(RedisManagerPool));

    private const string PoolTimeoutError =
        "Redis Timeout expired. The timeout period elapsed prior to obtaining a connection from the pool. This may have occurred because all pooled connections were in use.";

    public int RecheckPoolAfterMs = 100;

    public List<Action<IRedisClientsManager>> OnFailover { get; private set; }

    private readonly RedisClient[] clients;
    private readonly RedisPoolReservation[] reservations;
    private int poolGeneration;
    protected int poolIndex;

    protected int RedisClientCounter = 0;

    public Func<RedisEndpoint, RedisClient> ClientFactory { get; set; }

    public Action<IRedisNativeClient> ConnectionFilter { get; set; }

    public IRedisResolver RedisResolver { get; set; }

    public int MaxPoolSize { get; private set; }

    public bool AssertAccessOnlyOnSameThread { get; set; }

    public RedisManagerPool() : this(RedisConfig.DefaultHost) { }
    public RedisManagerPool(string host) : this(new[] { host }) { }
    public RedisManagerPool(string host, RedisPoolConfig config) : this(new[] { host }, config) { }
    public RedisManagerPool(IEnumerable<string> hosts) : this(hosts, null) { }

    public RedisManagerPool(IEnumerable<string> hosts, RedisPoolConfig config)
    {
        if (hosts == null)
            throw new ArgumentNullException(nameof(hosts));

        RedisResolver = new RedisResolver(hosts, null);

        if (config == null)
            config = new RedisPoolConfig();

        this.OnFailover = new List<Action<IRedisClientsManager>>();

        this.MaxPoolSize = config.MaxPoolSize;

        if (config.MaxLiveConnections <= 0)
            throw new ArgumentOutOfRangeException(nameof(config.MaxLiveConnections));
        if (config.MaxWaitingClients < 0)
            throw new ArgumentOutOfRangeException(nameof(config.MaxWaitingClients));
        if (config.PoolTimeout < Timeout.Infinite)
            throw new ArgumentOutOfRangeException(nameof(config.PoolTimeout));
        if (config.MaxLiveConnections != null && config.MaxPoolSize < 1)
            throw new ArgumentOutOfRangeException(nameof(config.MaxPoolSize));
        MaxLiveConnections = config.MaxLiveConnections;
        MaxWaitingClients = config.MaxWaitingClients;
        PoolTimeout = config.PoolTimeout;

        clients = new RedisClient[MaxPoolSize];
        reservations = new RedisPoolReservation[MaxPoolSize];
        poolIndex = 0;

        this.AssertAccessOnlyOnSameThread = RedisConfig.AssertAccessOnlyOnSameThread;

        JsConfig.InitStatics();
    }

    public void FailoverTo(params string[] readWriteHosts)
    {
        Interlocked.Increment(ref RedisState.TotalFailovers);

        Log.Info($"FailoverTo: {readWriteHosts.ToSafeHostsString()} Total: {RedisState.TotalFailovers}");
            
        if (MaxLiveConnections != null)
        {
            FailoverAdmission(readWriteHosts);
        }
        else lock (clients)
        {
            poolGeneration++;
            Array.Clear(reservations, 0, reservations.Length);
            for (var i = 0; i < clients.Length; i++)
            {
                var redis = clients[i];
                if (redis != null)
                    RedisState.DeactivateClient(redis);

                clients[i] = null;
            }
            RedisResolver.ResetMasters(readWriteHosts);
        }

        if (this.OnFailover != null)
        {
            foreach (var callback in OnFailover)
            {
                try
                {
                    callback(this);
                }
                catch (Exception ex)
                {
                    Log.Error("Error firing OnFailover callback(): ", ex);
                }
            }
        }
    }

    public void FailoverTo(IEnumerable<string> readWriteHosts, IEnumerable<string> readOnlyHosts)
    {
        FailoverTo(readWriteHosts.ToArray()); //only use readWriteHosts
    }

    /// <summary>
    /// Returns a Read/Write client (The default) using the hosts defined in ReadWriteHosts
    /// </summary>
    /// <returns></returns>
    public IRedisClient GetClient() => GetClient(false);
    private RedisClient GetClient(bool forAsync)
    {
        if (MaxLiveConnections != null)
        {
            var client = RedisPoolWaiters.Completed(GetAdmittedClientAsync(CancellationToken.None, forAsync: false));
            return !AssertAccessOnlyOnSameThread || forAsync
                ? client
                : client.LimitAccessToThread(Thread.CurrentThread.ManagedThreadId, Environment.StackTrace);
        }
        var acquired = (RedisClient)RedisPoolWaiters.Completed(GetUnboundedClientAsync(CancellationToken.None, forAsync: false));
        return !AssertAccessOnlyOnSameThread || forAsync ? acquired
            : acquired.LimitAccessToThread(Thread.CurrentThread.ManagedThreadId, Environment.StackTrace);
    }

    public IRedisClient GetReadOnlyClient()
    {
        return GetClient(false);
    }

    class ReservedClient : RedisClient
    {
        public ReservedClient()
        {
            this.DeactivatedAt = DateTime.UtcNow;
        }

        public override void Dispose() { }
    }

    static readonly ReservedClient reservedSlot = new ReservedClient();


    /// <summary>
    /// Called within a lock
    /// </summary>
    /// <returns></returns>
    private int GetInActiveClient(out RedisClient inactiveClient)
    {
        //this will loop through all hosts in readClients once even though there are 2 for loops
        //both loops are used to try to get the preferred host according to the round robin algorithm
        var readWriteTotal = RedisResolver.ReadWriteHostsCount;
        var desiredIndex = poolIndex % clients.Length;
        for (int x = 0; x < readWriteTotal; x++)
        {
            var nextHostIndex = (desiredIndex + x) % readWriteTotal;
            for (var i = nextHostIndex; i < clients.Length; i += readWriteTotal)
            {
                if (clients[i] != null && !clients[i].Active && !clients[i].HadExceptions)
                {
                    inactiveClient = clients[i];
                    return i;
                }

                if (clients[i] == null)
                {
                    clients[i] = reservedSlot;
                    reservations[i] = new RedisPoolReservation(poolGeneration);
                    inactiveClient = null;
                    return i;
                }

                if (clients[i] != reservedSlot && clients[i].HadExceptions)
                {
                    reservations[i] = new RedisPoolReservation(poolGeneration, clients[i]);
                    clients[i] = reservedSlot;
                    inactiveClient = null;
                    return i;
                }
            }
        }
        inactiveClient = null;
        return -1;
    }

    private RedisClient InitNewClient(RedisClient client)
    {
        client.Id = Interlocked.Increment(ref RedisClientCounter);
        client.Activate(newClient:true);
        client.ClientManager = this;
        client.ConnectionFilter = ConnectionFilter;
        client.ConnectionFilterAsync = ConnectionFilterAsync;

        return client;
    }

    public void DisposeClient(RedisNativeClient client)
    {
        if (MaxLiveConnections != null)
        {
            ReturnAdmittedClient(client);
            return;
        }
        lock (clients)
        {
            for (var i = 0; i < clients.Length; i++)
            {
                var writeClient = clients[i];
                if (client != writeClient) continue;
                if (client.IsDisposed)
                {
                    clients[i] = null;
                }
                else
                {
                    client.TrackThread = null;
                    client.Deactivate();
                }

                Monitor.PulseAll(clients);
                return;
            }
        }
    }

    /// <summary>
    /// Disposes the write client.
    /// </summary>
    /// <param name="client">The client.</param>
    public void DisposeWriteClient(RedisNativeClient client)
    {
        if (MaxLiveConnections != null)
        {
            ReturnAdmittedClient(client);
            return;
        }
        lock (clients)
        {
            client.Deactivate();
        }
    }

    public Dictionary<string, string> GetStats()
    {
        var clientsPoolSize = clients.Length;
        var clientsCreated = 0;
        var clientsWithExceptions = 0;
        var clientsInUse = 0;
        var clientsConnected = 0;

        foreach (var client in clients)
        {
            if (client == null || client == reservedSlot)
                continue;

            clientsCreated++;

            if (client.HadExceptions)
                clientsWithExceptions++;
            if (client.Active)
                clientsInUse++;
            if (client.IsSocketConnected())
                clientsConnected++;
        }

        var ret = new Dictionary<string, string>
        {
            {"VersionString", "" + Text.Env.VersionString},

            {"clientsPoolSize", "" + clientsPoolSize},
            {"clientsCreated", "" + clientsCreated},
            {"clientsWithExceptions", "" + clientsWithExceptions},
            {"clientsInUse", "" + clientsInUse},
            {"clientsConnected", "" + clientsConnected},

            {"RedisResolver.ReadOnlyHostsCount", "" + RedisResolver.ReadOnlyHostsCount},
            {"RedisResolver.ReadWriteHostsCount", "" + RedisResolver.ReadWriteHostsCount},
        };

        AddAdmissionStats(ret);
        return ret;
    }

    private void AssertValidPool()
    {
        if (clients.Length < 1)
            throw new InvalidOperationException("Need a minimum pool size of 1");
    }

    public int[] GetClientPoolActiveStates()
    {
        lock (clients)
        {
            var activeStates = new int[clients.Length];
            for (int i = 0; i < clients.Length; i++)
            {
                var client = clients[i];
                activeStates[i] = client == null
                    ? -1
                    : client.Active ? 1 : 0;
            }
            return activeStates;
        }
    }

    ~RedisManagerPool()
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
        if (Interlocked.Increment(ref disposeAttempts) > 1) return;

        if (MaxLiveConnections != null)
        {
            DisposeAdmission();
            return;
        }

        if (disposing)
        {
            // get rid of managed resources
        }

        try
        {
            // get rid of unmanaged resources
            foreach (var client in clients)
            {
                Dispose(client);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error when trying to dispose of PooledRedisClientManager", ex);
        }

        RedisState.DisposeAllDeactivatedClients();
    }

    private int disposeAttempts = 0;

    protected void Dispose(RedisClient redisClient)
    {
        if (redisClient == null) return;
        try
        {
            redisClient.DisposeConnection();
        }
        catch (Exception ex)
        {
            Log.Error($"Error when trying to dispose of RedisClient to host {redisClient.Host}:{redisClient.Port}", ex);
        }
    }

    public ICacheClient GetCacheClient()
    {
        return new RedisClientManagerCacheClient(this);
    }

    public ICacheClient GetReadOnlyCacheClient()
    {
        return new RedisClientManagerCacheClient(this) { ReadOnly = true };
    }

    public Dictionary<string, long> Stats => RedisStats.ToDictionary();
}