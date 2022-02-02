using System.Collections.Generic;
using ServiceStack.Text;


namespace ServiceStack.Redis.Support.Queue.Implementation
{
    /// <summary>
    /// distributed work item queue
    /// </summary>
    public class RedisWorkQueue<T>
    {
        protected readonly RedisNamespace queueNamespace;
        protected string pendingWorkItemIdQueue;
        protected string workQueue;
        protected readonly PooledRedisClientManager clientManager;

        public RedisWorkQueue(int maxReadPoolSize, int maxWritePoolSize, string host, int port)
            : this(maxReadPoolSize, maxWritePoolSize, host, port, null) { }

        public RedisWorkQueue(int maxReadPoolSize, int maxWritePoolSize, string host, int port, string queueName)
        {
            var qname = queueName ?? typeof(T) + "_Shared_Work_Queue";
            queueNamespace = new RedisNamespace(qname);
            pendingWorkItemIdQueue = queueNamespace.GlobalCacheKey("PendingWorkItemIdQueue");
            workQueue = queueNamespace.GlobalCacheKey("WorkQueue");

            var poolConfig = new RedisClientManagerConfig
            {
                MaxReadPoolSize = maxReadPoolSize,
                MaxWritePoolSize = maxWritePoolSize
            };

            clientManager = new PooledRedisClientManager(new[] { host + ":" + port }, 
                TypeConstants.EmptyStringArray, 
                poolConfig) {
                RedisResolver = { ClientFactory = config => new SerializingRedisClient(config) }
            };
        }

        public void Dispose()
        {
            clientManager.Dispose();
        }
    }
}