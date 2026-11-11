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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ServiceStack.Caching;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Redis;

/// <summary>
/// Provides thread-safe pooling of redis client connections.
/// Allows load-balancing of master-write and read-replica hosts, ideal for
/// 1 master and multiple replicated read replicas.
/// </summary>
public partial class PooledRedisClientManager
    : IRedisClientsManager, IRedisFailover, IHandleClientDispose, IHasRedisResolver, IHasStats
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(PooledRedisClientManager));

    private const string PoolTimeoutError =
        "Redis Timeout expired. The timeout period elapsed prior to obtaining a connection from the pool. This may have occurred because all pooled connections were in use.";

    protected readonly int PoolSizeMultiplier = 20;
    public int RecheckPoolAfterMs = 100;
    public int? PoolTimeout { get; set; }
    public int? ConnectTimeout { get; set; }
    public int? SocketSendTimeout { get; set; }
    public int? SocketReceiveTimeout { get; set; }
    public int? IdleTimeOutSecs { get; set; }
    public bool AssertAccessOnlyOnSameThread { get; set; }
        
    /// <summary>
    /// Gets or sets object key prefix.
    /// </summary>
    public string NamespacePrefix { get; set; }

    public IRedisResolver RedisResolver { get; set; }
    public List<Action<IRedisClientsManager>> OnFailover { get; private set; }

    private RedisClient[] writeClients = TypeConstants<RedisClient>.EmptyArray;
    private RedisPoolReservation[] writeReservations = Array.Empty<RedisPoolReservation>();
    private int writeGeneration;
    protected int WritePoolIndex;

    private RedisClient[] readClients = TypeConstants<RedisClient>.EmptyArray;
    private RedisPoolReservation[] readReservations = Array.Empty<RedisPoolReservation>();
    private int readGeneration;
    protected int ReadPoolIndex;

    protected int RedisClientCounter = 0;

    protected RedisClientManagerConfig Config { get; set; }

    public long? Db { get; private set; }

    public Action<IRedisNativeClient> ConnectionFilter { get; set; }

    public PooledRedisClientManager() : this(RedisConfig.DefaultHost) { }

    public PooledRedisClientManager(int poolSize, int poolTimeOutSeconds, params string[] readWriteHosts)
        : this(readWriteHosts, readWriteHosts, null, null, poolSize, poolTimeOutSeconds)
    {
    }

    public PooledRedisClientManager(long initialDb, params string[] readWriteHosts)
        : this(readWriteHosts, readWriteHosts, initialDb) { }

    public PooledRedisClientManager(params string[] readWriteHosts)
        : this(readWriteHosts, readWriteHosts)
    {
    }

    public PooledRedisClientManager(IEnumerable<string> readWriteHosts, IEnumerable<string> readOnlyHosts)
        : this(readWriteHosts, readOnlyHosts, null)
    {
    }

    /// <summary>
    /// Hosts can be an IP Address or Hostname in the format: host[:port]
    /// e.g. 127.0.0.1:6379
    /// default is: localhost:6379
    /// </summary>
    /// <param name="readWriteHosts">The write hosts.</param>
    /// <param name="readOnlyHosts">The read hosts.</param>
    /// <param name="config">The config.</param>
    public PooledRedisClientManager(
        IEnumerable<string> readWriteHosts,
        IEnumerable<string> readOnlyHosts,
        RedisClientManagerConfig config)
        : this(readWriteHosts, readOnlyHosts, config, null, null, null)
    {
    }

    public PooledRedisClientManager(
        IEnumerable<string> readWriteHosts,
        IEnumerable<string> readOnlyHosts,
        long initialDb)
        : this(readWriteHosts, readOnlyHosts, null, initialDb, null, null)
    {
    }

    public PooledRedisClientManager(
        IEnumerable<string> readWriteHosts,
        IEnumerable<string> readOnlyHosts,
        RedisClientManagerConfig config,
        long? initialDb,
        int? poolSizeMultiplier,
        int? poolTimeOutSeconds)
    {
        this.Db = config != null
            ? config.DefaultDb ?? initialDb
            : initialDb;

        var masters = (readWriteHosts ?? TypeConstants.EmptyStringArray).ToArray();
        var replicas = (readOnlyHosts ?? TypeConstants.EmptyStringArray).ToArray();

        RedisResolver = new RedisResolver(masters, replicas);

        this.PoolSizeMultiplier = poolSizeMultiplier ?? RedisConfig.DefaultPoolSizeMultiplier;

        this.Config = config ?? new RedisClientManagerConfig
        {
            MaxWritePoolSize = RedisConfig.DefaultMaxPoolSize ?? masters.Length * PoolSizeMultiplier,
            MaxReadPoolSize = RedisConfig.DefaultMaxPoolSize ?? replicas.Length * PoolSizeMultiplier,
        };

        this.OnFailover = new List<Action<IRedisClientsManager>>();

        // if timeout provided, convert into milliseconds
        this.PoolTimeout = poolTimeOutSeconds != null
            ? poolTimeOutSeconds * 1000
            : 2000; //Default Timeout

        this.AssertAccessOnlyOnSameThread = RedisConfig.AssertAccessOnlyOnSameThread;

        JsConfig.InitStatics();

        if (this.Config.AutoStart)
        {
            this.OnStart();
        }
    }

    public void FailoverTo(params string[] readWriteHosts)
    {
        FailoverTo(readWriteHosts, readWriteHosts);
    }

    public void FailoverTo(IEnumerable<string> readWriteHosts, IEnumerable<string> readOnlyHosts)
    {
        Interlocked.Increment(ref RedisState.TotalFailovers);

        var masters = readWriteHosts.ToList();
        var replicas = readOnlyHosts.ToList();

        Log.Info($"FailoverTo: {masters.ToSafeHostsString()} : {replicas.ToSafeHostsString()} Total: {RedisState.TotalFailovers}");

        lock (readClients)
        {
            readGeneration++;
            Array.Clear(readReservations, 0, readReservations.Length);
            for (var i = 0; i < readClients.Length; i++)
            {
                var redis = readClients[i];
                if (redis != null)
                    RedisState.DeactivateClient(redis);

                readClients[i] = null;
            }
            RedisResolver.ResetSlaves(replicas);
            PulseAllRead();
        }

        lock (writeClients)
        {
            writeGeneration++;
            Array.Clear(writeReservations, 0, writeReservations.Length);
            for (var i = 0; i < writeClients.Length; i++)
            {
                var redis = writeClients[i];
                if (redis != null)
                    RedisState.DeactivateClient(redis);

                writeClients[i] = null;
            }
            RedisResolver.ResetMasters(masters);
            PulseAllWrite();
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

    protected virtual void OnStart()
    {
        this.Start();
    }

    /// <summary>
    /// Returns a Read/Write client (The default) using the hosts defined in ReadWriteHosts
    /// </summary>
    /// <returns></returns>
    public IRedisClient GetClient() => GetClientBlocking();

    private RedisClient GetClientBlocking()
    {
        var client = (RedisClient)RedisPoolWaiters.Completed(GetPooledClientAsync(false, CancellationToken.None, forAsync: false));
        return !AssertAccessOnlyOnSameThread ? client
            : client.LimitAccessToThread(Thread.CurrentThread.ManagedThreadId, Environment.StackTrace);
    }

    class ReservedClient : RedisClient
    {
        public ReservedClient()
        {
            this.DeactivatedAt = DateTime.UtcNow;
        }

        public override void Dispose() {}
    }

    static readonly ReservedClient reservedSlot = new ReservedClient();

    /// <summary>
    /// Called within a lock
    /// </summary>
    /// <returns></returns>
    private int GetInActiveWriteClient(out RedisClient inactiveClient)
    {
        //this will loop through all hosts in readClients once even though there are 2 for loops
        //both loops are used to try to get the preferred host according to the round robin algorithm
        var readWriteTotal = RedisResolver.ReadWriteHostsCount;
        var desiredIndex = WritePoolIndex % writeClients.Length;
        for (int x = 0; x < readWriteTotal; x++)
        {
            var nextHostIndex = (desiredIndex + x) % readWriteTotal;
            for (var i = nextHostIndex; i < writeClients.Length; i += readWriteTotal)
            {
                if (writeClients[i] != null && !writeClients[i].Active && !writeClients[i].HadExceptions)
                {
                    inactiveClient = writeClients[i];
                    return i;
                }

                if (writeClients[i] == null)
                {
                    writeClients[i] = reservedSlot;
                    writeReservations[i] = new RedisPoolReservation(writeGeneration);
                    inactiveClient = null;
                    return i;
                }

                if (writeClients[i] != reservedSlot && writeClients[i].HadExceptions)
                {
                    writeReservations[i] = new RedisPoolReservation(writeGeneration, writeClients[i]);
                    writeClients[i] = reservedSlot;
                    inactiveClient = null;
                    return i;
                }
            }
        }
        inactiveClient = null;
        return -1;
    }

    /// <summary>
    /// Returns a ReadOnly client using the hosts defined in ReadOnlyHosts.
    /// </summary>
    /// <returns></returns>
    public virtual IRedisClient GetReadOnlyClient() => GetReadOnlyClientBlocking();

    private RedisClient GetReadOnlyClientBlocking()
        => (RedisClient)RedisPoolWaiters.Completed(GetPooledClientAsync(true, CancellationToken.None, forAsync: false));

    /// <summary>
    /// Called within a lock
    /// </summary>
    /// <returns></returns>
    private int GetInActiveReadClient(out RedisClient inactiveClient)
    {
        var desiredIndex = ReadPoolIndex % readClients.Length;
        //this will loop through all hosts in readClients once even though there are 2 for loops
        //both loops are used to try to get the preferred host according to the round robin algorithm
        var readOnlyTotal = RedisResolver.ReadOnlyHostsCount;
        for (int x = 0; x < readOnlyTotal; x++)
        {
            var nextHostIndex = (desiredIndex + x) % readOnlyTotal;
            for (var i = nextHostIndex; i < readClients.Length; i += readOnlyTotal)
            {
                if (readClients[i] != null && !readClients[i].Active && !readClients[i].HadExceptions)
                {
                    inactiveClient = readClients[i];
                    return i;
                }

                if (readClients[i] == null)
                {
                    readClients[i] = reservedSlot;
                    readReservations[i] = new RedisPoolReservation(readGeneration);
                    inactiveClient = null;
                    return i;
                }

                if (readClients[i] != reservedSlot && readClients[i].HadExceptions)
                {
                    readReservations[i] = new RedisPoolReservation(readGeneration, readClients[i]);
                    readClients[i] = reservedSlot;
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
        if (NamespacePrefix != null)
            client.NamespacePrefix = NamespacePrefix;

        return InitClient(client);
    }

    private RedisClient InitClient(RedisClient client, bool deferDb = false)
    {
        if (this.ConnectTimeout != null)
            client.ConnectTimeout = this.ConnectTimeout.Value;
        if (this.SocketSendTimeout.HasValue)
            client.SendTimeout = this.SocketSendTimeout.Value;
        if (this.SocketReceiveTimeout.HasValue)
            client.ReceiveTimeout = this.SocketReceiveTimeout.Value;
        if (this.IdleTimeOutSecs.HasValue)
            client.IdleTimeOutSecs = this.IdleTimeOutSecs.Value;
        if (this.NamespacePrefix != null)
            client.NamespacePrefix = NamespacePrefix;
        if (Db != null && client.Db != Db) //Reset database to default if changed
        {
            if (!deferDb) client.ChangeDb(Db.Value);
            else if (!client.HasConnected) client.Db = Db.Value;
        }
        return client;
    }

    partial void PulseAllReadAsync();
    private void PulseAllRead()
    {
        PulseAllReadAsync();
        Monitor.PulseAll(readClients);
    }

    partial void PulseAllWriteAsync();
    private void PulseAllWrite()
    {
        PulseAllWriteAsync();
        Monitor.PulseAll(writeClients);
    }

    public void DisposeClient(RedisNativeClient client)
    {
        lock (readClients)
        {
            for (var i = 0; i < readClients.Length; i++)
            {
                var readClient = readClients[i];
                if (client != readClient) continue;
                if (client.IsDisposed)
                {
                    readClients[i] = null;
                }
                else
                {
                    client.TrackThread = null;
                    client.Deactivate();
                }

                WakeOneRead();
                return;
            }
        }

        lock (writeClients)
        {
            for (var i = 0; i < writeClients.Length; i++)
            {
                var writeClient = writeClients[i];
                if (client != writeClient) continue;
                if (client.IsDisposed)
                {
                    writeClients[i] = null;
                }
                else
                {
                    client.TrackThread = null;
                    client.Deactivate();
                }

                WakeOneWrite();
                return;
            }
        }

        //Client not found in any pool, wake a waiter in both pools.
        lock (readClients)
        {
            WakeOneRead();
        }

        lock (writeClients)
        {
            WakeOneWrite();
        }
    }

    /// <summary>
    /// Disposes the read only client.
    /// </summary>
    /// <param name="client">The client.</param>
    public void DisposeReadOnlyClient(RedisNativeClient client)
    {
        lock (readClients)
        {
            client.Deactivate();
            WakeOneRead();
        }
    }

    /// <summary>
    /// Disposes the write client.
    /// </summary>
    /// <param name="client">The client.</param>
    public void DisposeWriteClient(RedisNativeClient client)
    {
        lock (writeClients)
        {
            client.Deactivate();
            WakeOneWrite();
        }
    }

    public void Start()
    {
        if (writeClients.Length > 0 || readClients.Length > 0)
            throw new InvalidOperationException("Pool has already been started");

        writeClients = new RedisClient[Config.MaxWritePoolSize];
        writeReservations = new RedisPoolReservation[writeClients.Length];
        WritePoolIndex = 0;

        readClients = new RedisClient[Config.MaxReadPoolSize];
        readReservations = new RedisPoolReservation[readClients.Length];
        ReadPoolIndex = 0;
    }

    public Dictionary<string, string> GetStats()
    {
        var writeClientsPoolSize = writeClients.Length;
        var writeClientsCreated = 0;
        var writeClientsWithExceptions = 0;
        var writeClientsInUse = 0;
        var writeClientsConnected = 0;

        foreach (var client in writeClients)
        {
            if (client == null || client == reservedSlot)
                continue;

            writeClientsCreated++;

            if (client.HadExceptions)
                writeClientsWithExceptions++;
            if (client.Active)
                writeClientsInUse++;
            if (client.IsSocketConnected())
                writeClientsConnected++;
        }

        var readClientsPoolSize = readClients.Length;
        var readClientsCreated = 0;
        var readClientsWithExceptions = 0;
        var readClientsInUse = 0;
        var readClientsConnected = 0;

        foreach (var client in readClients)
        {
            if (client == null || client == reservedSlot)
                continue;

            readClientsCreated++;

            if (client.HadExceptions)
                readClientsWithExceptions++;
            if (client.Active)
                readClientsInUse++;
            if (client.IsSocketConnected())
                readClientsConnected++;
        }

        var ret = new Dictionary<string, string>
        {
            {"VersionString", "" + Env.VersionString},

            {"writeClientsPoolSize", "" + writeClientsPoolSize},
            {"writeClientsCreated", "" + writeClientsCreated},
            {"writeClientsWithExceptions", "" + writeClientsWithExceptions},
            {"writeClientsInUse", "" + writeClientsInUse},
            {"writeClientsConnected", "" + writeClientsConnected},

            {"readClientsPoolSize", "" + readClientsPoolSize},
            {"readClientsCreated", "" + readClientsCreated},
            {"readClientsWithExceptions", "" + readClientsWithExceptions},
            {"readClientsInUse", "" + readClientsInUse},
            {"readClientsConnected", "" + readClientsConnected},

            {"RedisResolver.ReadOnlyHostsCount", "" + RedisResolver.ReadOnlyHostsCount},
            {"RedisResolver.ReadWriteHostsCount", "" + RedisResolver.ReadWriteHostsCount},
        };

        return ret;
    }

    private void AssertValidReadWritePool()
    {
        if (Volatile.Read(ref disposeAttempts) > 0)
            throw new ObjectDisposedException(nameof(PooledRedisClientManager));
        if (writeClients.Length < 1)
            throw new InvalidOperationException("Need a minimum read-write pool size of 1, then call Start()");
    }

    private void AssertValidReadOnlyPool()
    {
        if (Volatile.Read(ref disposeAttempts) > 0)
            throw new ObjectDisposedException(nameof(PooledRedisClientManager));
        if (readClients.Length < 1)
            throw new InvalidOperationException("Need a minimum read pool size of 1, then call Start()");
    }

    public int[] GetClientPoolActiveStates()
    {
        var activeStates = new int[writeClients.Length];
        lock (writeClients)
        {
            for (int i = 0; i < writeClients.Length; i++)
            {
                activeStates[i] = writeClients[i] == null
                    ? -1
                    : writeClients[i].Active ? 1 : 0;
            }
        }
        return activeStates;
    }

    public int[] GetReadOnlyClientPoolActiveStates()
    {
        var activeStates = new int[readClients.Length];
        lock (readClients)
        {
            for (int i = 0; i < readClients.Length; i++)
            {
                activeStates[i] = readClients[i] == null
                    ? -1
                    : readClients[i].Active ? 1 : 0;
            }
        }
        return activeStates;
    }

    ~PooledRedisClientManager()
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

        lock (readClients)
            PulseAllRead();
        lock (writeClients)
            PulseAllWrite();

        if (disposing)
        {
            // get rid of managed resources
        }

        try
        {
            // get rid of unmanaged resources
            for (var i = 0; i < writeClients.Length; i++)
            {
                Dispose(writeClients[i]);
            }
            for (var i = 0; i < readClients.Length; i++)
            {
                Dispose(readClients[i]);
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
}

public partial class PooledRedisClientManager : IRedisClientCacheManager
{
    /// <summary>
    /// Manage a client acquired from the PooledRedisClientManager
    /// Dispose method will release the client back to the pool.
    /// </summary>
    public class DisposablePooledClient<T> : IDisposable where T : RedisNativeClient
    {
        private T client;
        private readonly PooledRedisClientManager clientManager;

        /// <summary>
        /// wrap the acquired client
        /// </summary>
        /// <param name="clientManager"></param>
        public DisposablePooledClient(PooledRedisClientManager clientManager)
        {
            this.clientManager = clientManager;
            if (clientManager != null)
                client = (T)clientManager.GetClient();
        }

        /// <summary>
        /// access the wrapped client
        /// </summary>
        public T Client => client;

        /// <summary>
        /// release the wrapped client back to the pool
        /// </summary>
        public void Dispose()
        {
            if (client != null)
                clientManager.DisposeClient(client);
            client = null;
        }
    }

    public DisposablePooledClient<T> GetDisposableClient<T>() where T : RedisNativeClient
    {
        return new DisposablePooledClient<T>(this);
    }

    public Dictionary<string, long> Stats => RedisStats.ToDictionary();
}