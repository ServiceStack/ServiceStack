using System.Threading;
using ServiceStack.Logging;
using System;
using System.Collections.Generic;

namespace ServiceStack.Redis
{
    internal class RedisSentinelWorker : IDisposable
    {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(RedisSentinelWorker));

        static int IdCounter = 0;
        public int Id { get; }

        private readonly object oLock = new();

        private readonly RedisEndpoint sentinelEndpoint;
        private readonly RedisSentinel sentinel;
        private readonly RedisClient sentinelClient;
        private RedisPubSubServer sentinelPubSub;

        public Action<Exception> OnSentinelError;

        public RedisSentinelWorker(RedisSentinel sentinel, RedisEndpoint sentinelEndpoint)
        {
            this.Id = Interlocked.Increment(ref IdCounter);
            this.sentinel = sentinel;
            this.sentinelEndpoint = sentinelEndpoint;
            this.sentinelClient = new RedisClient(sentinelEndpoint) {
                Db = 0, //Sentinel Servers doesn't support DB, reset to 0
                ConnectTimeout = sentinel.SentinelWorkerConnectTimeoutMs,
                ReceiveTimeout = sentinel.SentinelWorkerReceiveTimeoutMs,
                SendTimeout = sentinel.SentinelWorkerSendTimeoutMs,
            };

            if (Log.IsDebugEnabled)
                Log.Debug($"Set up Redis Sentinel on {sentinelEndpoint}");
        }

        /// <summary>
        /// Event that is fired when the sentinel subscription raises an event
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        private void SentinelMessageReceived(string channel, string message)
        {
            if (Log.IsDebugEnabled)
                Log.Debug($"Received '{channel}' on channel '{message}' from Sentinel");

            // {+|-}sdown is the event for server coming up or down
            var c = channel.ToLower();
            var isSubjectivelyDown = c.Contains("sdown");
            if (isSubjectivelyDown)
                Interlocked.Increment(ref RedisState.TotalSubjectiveServersDown);

            var isObjectivelyDown = c.Contains("odown");
            if (isObjectivelyDown)
                Interlocked.Increment(ref RedisState.TotalObjectiveServersDown);

            if (c == "+failover-end" 
                || c == "+switch-master"
                || (sentinel.ResetWhenSubjectivelyDown && isSubjectivelyDown)
                || (sentinel.ResetWhenObjectivelyDown && isObjectivelyDown))
            {
                if (Log.IsDebugEnabled)
                    Log.Debug($"Sentinel detected server down/up '{channel}' with message: {message}");

                sentinel.ResetClients();
            }

            if (sentinel.OnSentinelMessageReceived != null)
                sentinel.OnSentinelMessageReceived(channel, message);
        }

        internal SentinelInfo GetSentinelInfo()
        {
            var masterHost = GetMasterHostInternal(sentinel.MasterName);
            if (masterHost == null)
                throw new RedisException("Redis Sentinel is reporting no master is available");

            var sentinelInfo = new SentinelInfo(
                sentinel.MasterName,
                new[] { masterHost },
                GetReplicaHosts(sentinel.MasterName));

            return sentinelInfo;
        }

        internal string GetMasterHost(string masterName)
        {
            try
            {
                return GetMasterHostInternal(masterName);
            }
            catch (Exception ex)
            {
                if (OnSentinelError != null)
                    OnSentinelError(ex);

                return null;
            }
        }

        private string GetMasterHostInternal(string masterName)
        {
            List<string> masterInfo;
            lock (oLock)
                masterInfo = sentinelClient.SentinelGetMasterAddrByName(masterName);

            return masterInfo.Count > 0
                ? SanitizeMasterConfig(masterInfo)
                : null;
        }

        private string SanitizeMasterConfig(List<string> masterInfo)
        {
            var ip = masterInfo[0];
            var port = masterInfo[1];

            if (sentinel.IpAddressMap.TryGetValue(ip, out var aliasIp))
                ip = aliasIp;

            return $"{ip}:{port}";
        }

        internal List<string> GetSentinelHosts(string masterName)
        {
            List<Dictionary<string, string>> sentinelSentinels;
            lock (oLock)
                sentinelSentinels = this.sentinelClient.SentinelSentinels(sentinel.MasterName);

            return SanitizeHostsConfig(sentinelSentinels);
        }

        internal List<string> GetReplicaHosts(string masterName)
        {
            List<Dictionary<string, string>> sentinelReplicas;

            lock (oLock)
                sentinelReplicas = sentinelClient.SentinelSlaves(sentinel.MasterName);

            return SanitizeHostsConfig(sentinelReplicas);
        }

        private List<string> SanitizeHostsConfig(IEnumerable<Dictionary<string, string>> replicas)
        {
            var servers = new List<string>();
            foreach (var replica in replicas)
            {
                replica.TryGetValue("flags", out var flags);
                replica.TryGetValue("ip", out var ip);
                replica.TryGetValue("port", out var port);

                if (sentinel.IpAddressMap.TryGetValue(ip, out var aliasIp))
                    ip = aliasIp;
                else if (ip == "127.0.0.1")
                    ip = this.sentinelClient.Host;

                if (ip != null && port != null && !flags.Contains("s_down") && !flags.Contains("o_down"))
                    servers.Add($"{ip}:{port}");
            }
            return servers;
        }

        public void BeginListeningForConfigurationChanges()
        {
            try
            {
                lock (oLock)
                {
                    if (this.sentinelPubSub == null)
                    {
                        var currentSentinelHost = new[] {sentinelEndpoint};
                        var sentinelManager = new BasicRedisClientManager(currentSentinelHost, currentSentinelHost)
                        {
                            //Use BasicRedisResolver which doesn't validate non-Master Sentinel instances
                            RedisResolver = new BasicRedisResolver(currentSentinelHost, currentSentinelHost)
                        };
                        
                        if (Log.IsDebugEnabled)
                            Log.Debug($"Starting subscription to {sentinel.SentinelHosts.ToArray()}, replicas: {sentinel.SentinelHosts.ToArray()}...");
                        
                        this.sentinelPubSub = new RedisPubSubServer(sentinelManager)
                        {
                            HeartbeatInterval = null,
                            IsSentinelSubscription = true,
                            ChannelsMatching = new[] { RedisPubSubServer.AllChannelsWildCard },
                            OnMessage = SentinelMessageReceived
                        };
                    }
                }

                this.sentinelPubSub.Start();
            }
            catch (Exception ex)
            {
                Log.Error($"Error Subscribing to Redis Channel on {sentinelClient.Host}:{sentinelClient.Port}", ex);

                if (OnSentinelError != null)
                    OnSentinelError(ex);
            }
        }

        public void ForceMasterFailover(string masterName)
        {
            lock (oLock)
                this.sentinelClient.SentinelFailover(masterName);
        }

        public void Dispose()
        {
            new IDisposable[] { this.sentinelClient, sentinelPubSub }.Dispose(Log);
        }
    }
}
