using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Logging;

namespace ServiceStack.Redis
{
    public class BasicRedisResolver : IRedisResolver, IRedisResolverExtended
    {
        static ILog log = LogManager.GetLogger(typeof(BasicRedisResolver));

        public Func<RedisEndpoint, RedisClient> ClientFactory { get; set; }

        public int ReadWriteHostsCount { get; private set; }
        public int ReadOnlyHostsCount { get; private set; }

        private RedisEndpoint[] masters;
        private RedisEndpoint[] replicas;

        public RedisEndpoint[] Masters => masters;
        public RedisEndpoint[] Replicas => replicas;

        public BasicRedisResolver(IEnumerable<RedisEndpoint> masters, IEnumerable<RedisEndpoint> replicas)
        {
            ResetMasters(masters.ToList());
            ResetSlaves(replicas.ToList());
            ClientFactory = RedisConfig.ClientFactory;
        }

        public virtual void ResetMasters(IEnumerable<string> hosts)
        {
            ResetMasters(hosts.ToRedisEndPoints());
        }

        public virtual void ResetMasters(List<RedisEndpoint> newMasters)
        {
            if (newMasters == null || newMasters.Count == 0)
                throw new Exception("Must provide at least 1 master");

            masters = newMasters.ToArray();
            ReadWriteHostsCount = masters.Length;

            if (log.IsDebugEnabled)
                log.Debug("New Redis Masters: " + string.Join(", ", masters.Map(x => x.GetHostString())));
        }

        public virtual void ResetSlaves(IEnumerable<string> hosts)
        {
            ResetSlaves(hosts.ToRedisEndPoints());
        }

        public virtual void ResetSlaves(List<RedisEndpoint> newReplicas)
        {
            replicas = (newReplicas ?? TypeConstants<RedisEndpoint>.EmptyList).ToArray();
            ReadOnlyHostsCount = replicas.Length;

            if (log.IsDebugEnabled)
                log.Debug("New Redis Replicas: " + string.Join(", ", replicas.Map(x => x.GetHostString())));
        }

        public RedisClient CreateRedisClient(RedisEndpoint config, bool master)
        {
            return ClientFactory(config);
        }

        public RedisEndpoint GetReadWriteHost(int desiredIndex)
        {
            return masters[desiredIndex % masters.Length];
        }

        public RedisEndpoint GetReadOnlyHost(int desiredIndex)
        {
            return ReadOnlyHostsCount > 0
                ? replicas[desiredIndex % replicas.Length]
                : GetReadWriteHost(desiredIndex);
        }

        public RedisClient CreateMasterClient(int desiredIndex)
        {
            return CreateRedisClient(GetReadWriteHost(desiredIndex), master: true);
        }

        public RedisClient CreateSlaveClient(int desiredIndex)
        {
            return CreateRedisClient(GetReadOnlyHost(desiredIndex), master: false);
        }
    }
}