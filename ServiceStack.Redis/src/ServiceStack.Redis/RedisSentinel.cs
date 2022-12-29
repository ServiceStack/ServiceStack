//
// Redis Sentinel will connect to a Redis Sentinel Instance and create an IRedisClientsManager based off of the first sentinel that returns data
//
// Upon failure of a sentinel, other sentinels will be attempted to be connected to
// Upon a s_down event, the RedisClientsManager will be failed over to the new set of masters/replicas
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Redis
{
    public class RedisSentinel : IRedisSentinel
    {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(RedisSentinel));

        public static string DefaultMasterName = "mymaster";
        public static string DefaultAddress = "127.0.0.1:26379";

        private readonly object oLock = new object();
        private bool isDisposed = false;

        private readonly string masterName;
        public string MasterName => masterName;

        private int failures = 0;
        private int sentinelIndex = -1;
        public List<string> SentinelHosts { get; private set; }
        internal RedisEndpoint[] SentinelEndpoints { get; private set; }
        private RedisSentinelWorker worker;
        private static int MaxFailures = 5;

        /// <summary>
        /// Change to use a different IRedisClientsManager 
        /// </summary>
        public Func<string[], string[], IRedisClientsManager> RedisManagerFactory { get; set; }

        /// <summary>
        /// Configure the Redis Connection String to use for a Redis Instance Host
        /// </summary>
        public Func<string, string> HostFilter { get; set; }

        /// <summary>
        /// Configure the Redis Connection String to use for a Redis Sentinel Host
        /// </summary>
        public Func<string, string> SentinelHostFilter { get; set; }

        /// <summary>
        /// The configured Redis Client Manager this Sentinel managers
        /// </summary>
        public IRedisClientsManager RedisManager { get; set; }

        /// <summary>
        /// Fired when Sentinel fails over the Redis Client Manager to a new master
        /// </summary>
        public Action<IRedisClientsManager> OnFailover { get; set; }

        /// <summary>
        /// Fired when the Redis Sentinel Worker connection fails
        /// </summary>
        public Action<Exception> OnWorkerError { get; set; }

        /// <summary>
        /// Fired when the Sentinel worker receives a message from the Sentinel Subscription
        /// </summary>
        public Action<string, string> OnSentinelMessageReceived { get; set; }

        /// <summary>
        /// Map the internal IP's returned by Sentinels to its external IP
        /// </summary>
        public Dictionary<string, string> IpAddressMap { get; set; }

        /// <summary>
        /// Whether to routinely scan for other sentinel hosts (default true)
        /// </summary>
        public bool ScanForOtherSentinels { get; set; } = true;

        /// <summary>
        /// What interval to scan for other sentinel hosts (default 10 mins)
        /// </summary>
        public TimeSpan RefreshSentinelHostsAfter { get; set; } = TimeSpan.FromMinutes(10);
        private DateTime lastSentinelsRefresh;

        /// <summary>
        /// How long to wait after failing before connecting to next redis instance (default 250ms)
        /// </summary>
        public TimeSpan WaitBetweenFailedHosts { get; set; } = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// How long to retry connecting to hosts before throwing (default 60 secs)
        /// </summary>
        public TimeSpan MaxWaitBetweenFailedHosts { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// How long to wait after consecutive failed connection attempts to master before forcing 
        /// a Sentinel to failover the current master (default 60 secs)
        /// </summary>
        public TimeSpan WaitBeforeForcingMasterFailover { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// The Max Connection time for Sentinel Worker (default 250ms)
        /// </summary>
        public int SentinelWorkerConnectTimeoutMs { get; set; } = 250;

        /// <summary>
        /// The Max TCP Socket Receive time for Sentinel Worker (default 250ms)
        /// </summary>
        public int SentinelWorkerReceiveTimeoutMs { get; set; } = 250;

        /// <summary>
        /// The Max TCP Socket Send time for Sentinel Worker (default 250ms)
        /// </summary>
        public int SentinelWorkerSendTimeoutMs { get; set; } = 250;

        /// <summary>
        /// Reset client connections when Sentinel reports redis instance is subjectively down (default true)
        /// </summary>
        public bool ResetWhenSubjectivelyDown { get; set; } = true;

        /// <summary>
        /// Reset client connections when Sentinel reports redis instance is objectively down (default true)
        /// </summary>
        public bool ResetWhenObjectivelyDown { get; set; } = true;

        internal string DebugId => $"";

        public RedisSentinel(string sentinelHost = null, string masterName = null)
            : this(new[] { sentinelHost ?? DefaultAddress }, masterName ?? DefaultMasterName) { }

        public RedisSentinel(IEnumerable<string> sentinelHosts, string masterName = null)
        {
            this.SentinelHosts = sentinelHosts?.ToList();

            if (SentinelHosts == null || SentinelHosts.Count == 0)
                throw new ArgumentException("sentinels must have at least one entry");

            this.masterName = masterName ?? DefaultMasterName;
            IpAddressMap = new Dictionary<string, string>();
            RedisManagerFactory = (masters, replicas) => new PooledRedisClientManager(masters, replicas);
        }

        /// <summary>
        /// Initialize Sentinel Subscription and Configure Redis ClientsManager
        /// </summary>
        public IRedisClientsManager Start()
        {
            lock (oLock)
            {
                for (int i = 0; i < SentinelHosts.Count; i++)
                {
                    var parts = SentinelHosts[i].SplitOnLast(':');
                    if (parts.Length == 1)
                    {
                        SentinelHosts[i] = parts[0] + ":" + RedisConfig.DefaultPortSentinel;
                    }
                }

                if (ScanForOtherSentinels)
                    RefreshActiveSentinels();

                SentinelEndpoints = SentinelHosts
                    .Map(x => x.ToRedisEndpoint(defaultPort: RedisConfig.DefaultPortSentinel))
                    .ToArray();

                var sentinelWorker = GetValidSentinelWorker();

                if (this.RedisManager == null || sentinelWorker == null)
                    throw new Exception("Unable to resolve sentinels!");
                    
                return this.RedisManager;
            }
        }

        public List<string> GetActiveSentinelHosts(IEnumerable<string> sentinelHosts)
        {
            var activeSentinelHosts = new List<string>();
            foreach (var sentinelHost in sentinelHosts.ToArray())
            {
                try
                {
                    if (Log.IsDebugEnabled)
                        Log.Debug("Connecting to all available Sentinels to discover Active Sentinel Hosts...");

                    var endpoint = sentinelHost.ToRedisEndpoint(defaultPort: RedisConfig.DefaultPortSentinel);
                    using (var sentinelWorker = new RedisSentinelWorker(this, endpoint))
                    {
                        if (!activeSentinelHosts.Contains(sentinelHost))
                            activeSentinelHosts.Add(sentinelHost);

                        var activeHosts = sentinelWorker.GetSentinelHosts(MasterName);
                        foreach (var activeHost in activeHosts)
                        {
                            if (!activeSentinelHosts.Contains(activeHost))
                            {
                                activeSentinelHosts.Add(SentinelHostFilter != null
                                    ? SentinelHostFilter(activeHost)
                                    : activeHost);
                            }
                        }
                    }

                    if (Log.IsDebugEnabled)
                        Log.Debug("All active Sentinels Found: " + string.Join(", ", activeSentinelHosts));
                }
                catch (Exception ex)
                {
                    Log.Error("Could not get active Sentinels from: {0}".Fmt(sentinelHost), ex);
                }
            }
            return activeSentinelHosts;
        }

        public void RefreshActiveSentinels()
        {
            var activeHosts = GetActiveSentinelHosts(SentinelHosts);
            if (activeHosts.Count == 0) return;

            lock (SentinelHosts)
            {
                lastSentinelsRefresh = DateTime.UtcNow;

                activeHosts.Each(x =>
                {
                    if (!SentinelHosts.Contains(x))
                        SentinelHosts.Add(x);
                });

                SentinelEndpoints = SentinelHosts
                    .Map(x => x.ToRedisEndpoint(defaultPort: RedisConfig.DefaultPortSentinel))
                    .ToArray();
            }
        }

        internal string[] ConfigureHosts(IEnumerable<string> hosts)
        {
            if (hosts == null)
                return TypeConstants.EmptyStringArray;

            return HostFilter == null
                ? hosts.ToArray()
                : hosts.Map(HostFilter).ToArray();
        }

        public SentinelInfo ResetClients()
        {
            var sentinelInfo = GetSentinelInfo();

            if (RedisManager == null)
            {
                if (Log.IsDebugEnabled)
                    Log.Debug($"Configuring initial Redis Clients: {sentinelInfo}");

                RedisManager = CreateRedisManager(sentinelInfo);
            }
            else
            {
                if (Log.IsDebugEnabled)
                    Log.Debug($"Failing over to Redis Clients: {sentinelInfo}");

                ((IRedisFailover)RedisManager).FailoverTo(
                    ConfigureHosts(sentinelInfo.RedisMasters),
                    ConfigureHosts(sentinelInfo.RedisSlaves));
            }

            return sentinelInfo;
        }

        private IRedisClientsManager CreateRedisManager(SentinelInfo sentinelInfo)
        {
            var masters = ConfigureHosts(sentinelInfo.RedisMasters);
            var replicas = ConfigureHosts(sentinelInfo.RedisSlaves);
            var redisManager = RedisManagerFactory(masters, replicas);

            var hasRedisResolver = (IHasRedisResolver)redisManager;
            hasRedisResolver.RedisResolver = new RedisSentinelResolver(this, masters, replicas);

            if (redisManager is IRedisFailover canFailover && this.OnFailover != null)
            {
                canFailover.OnFailover.Add(this.OnFailover);
            }
            return redisManager;
        }

        public IRedisClientsManager GetRedisManager() => 
            RedisManager ??= CreateRedisManager(GetSentinelInfo());

        private RedisSentinelWorker GetValidSentinelWorker()
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().Name);

            if (this.worker != null)
                return this.worker;

            RedisException lastEx = null;

            while (this.worker == null && ShouldRetry())
            {
                var step = 0;
                try
                {
                    this.worker = GetNextSentinel();
                    step = 1;
                    GetRedisManager();

                    step = 2;
                    this.worker.BeginListeningForConfigurationChanges();
                    this.failures = 0; //reset
                    return this.worker;
                }
                catch (RedisException ex)
                {
                    if (Log.IsDebugEnabled)
                    {
                        var name = step switch {
                            0 => "GetNextSentinel()",
                            1 => "GetRedisManager()",
                            2 => "BeginListeningForConfigurationChanges()",
                            _ => $"Step {step}",
                        };
                        Log.Debug($"Failed to {name}: {ex.Message}");
                    }
                    
                    if (OnWorkerError != null)
                        OnWorkerError(ex);

                    lastEx = ex;
                    this.worker = null;
                    this.failures++;
                    Interlocked.Increment(ref RedisState.TotalFailedSentinelWorkers);
                }
            }

            this.failures = 0; //reset
            TaskUtils.Sleep(WaitBetweenFailedHosts);
            throw new RedisException("No Redis Sentinels were available", lastEx);
        }

        public RedisEndpoint GetMaster()
        {
            var sentinelWorker = GetValidSentinelWorker();
            var host = sentinelWorker.GetMasterHost(masterName);

            if (ScanForOtherSentinels && DateTime.UtcNow - lastSentinelsRefresh > RefreshSentinelHostsAfter)
            {
                RefreshActiveSentinels();
            }

            return host != null
                ? (HostFilter != null ? HostFilter(host) : host).ToRedisEndpoint()
                : null;
        }

        public List<RedisEndpoint> GetSlaves()
        {
            var sentinelWorker = GetValidSentinelWorker();
            var hosts = sentinelWorker.GetReplicaHosts(masterName);
            return ConfigureHosts(hosts).Map(x => x.ToRedisEndpoint());
        }

        /// <summary>
        /// Check if GetValidSentinel should try the next sentinel server
        /// </summary>
        /// <returns></returns>
        /// <remarks>This will be true if the failures is less than either RedisSentinel.MaxFailures or the # of sentinels, whatever is greater</remarks>
        private bool ShouldRetry()
        {
            return this.failures < Math.Max(MaxFailures, this.SentinelEndpoints.Length);
        }

        private RedisSentinelWorker GetNextSentinel()
        {
            RedisSentinelWorker disposeWorker = null;

            try
            {
                lock (oLock)
                {
                    if (this.worker != null)
                    {
                        disposeWorker = this.worker;
                        this.worker = null;
                    }

                    if (++sentinelIndex >= SentinelEndpoints.Length)
                        sentinelIndex = 0;
                    
                    if (Log.IsDebugEnabled)
                        Log.Debug($"Attempt to connect to next sentinel '{SentinelEndpoints[sentinelIndex]}'...");

                    var sentinelWorker = new RedisSentinelWorker(this, SentinelEndpoints[sentinelIndex])
                    {
                        OnSentinelError = OnSentinelError
                    };

                    return sentinelWorker;
                }
            }
            finally
            {
                disposeWorker?.Dispose();
            }
        }

        private void OnSentinelError(Exception ex)
        {
            if (this.worker != null)
            {
                Log.Error("Error on existing SentinelWorker, reconnecting...");

                if (OnWorkerError != null)
                    OnWorkerError(ex);

                this.worker = GetNextSentinel();
                this.worker.BeginListeningForConfigurationChanges();
            }
        }

        public void ForceMasterFailover()
        {
            var sentinelWorker = GetValidSentinelWorker();
            sentinelWorker.ForceMasterFailover(masterName);
        }

        public SentinelInfo GetSentinelInfo()
        {
            var sentinelWorker = GetValidSentinelWorker();
            return sentinelWorker.GetSentinelInfo();
        }

        public void Dispose()
        {
            this.isDisposed = true;

            new IDisposable[] { RedisManager, worker }.Dispose();
        }
    }
}

public class SentinelInfo
{
    public string MasterName { get; set; }
    public string[] RedisMasters { get; set; }
    public string[] RedisSlaves { get; set; }

    public SentinelInfo(string masterName, IEnumerable<string> redisMasters, IEnumerable<string> redisReplicas)
    {
        MasterName = masterName;
        RedisMasters = redisMasters?.ToArray() ?? TypeConstants.EmptyStringArray;
        RedisSlaves = redisReplicas?.ToArray() ?? TypeConstants.EmptyStringArray;
    }

    public override string ToString()
    {
        return $"{MasterName} primary: {string.Join(", ", RedisMasters)}, replicas: {string.Join(", ", RedisSlaves)}";
    }
}
