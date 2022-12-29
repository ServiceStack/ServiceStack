using System;
using System.Collections.Generic;


namespace ServiceStack.Redis.Support.Queue.Implementation
{
    /// <summary>
    /// simple distributed work item queue 
    /// 
    /// 
    /// </summary>
    public class RedisSimpleWorkQueue<T> : RedisWorkQueue<T>, ISimpleWorkQueue<T> where T : class
    {
        public RedisSimpleWorkQueue(int maxReadPoolSize, int maxWritePoolSize, string host, int port)
            : base(maxReadPoolSize, maxWritePoolSize, host, port)
        {
        }


        public RedisSimpleWorkQueue(int maxReadPoolSize, int maxWritePoolSize, string host, int port, string queueName )
            : base(maxReadPoolSize, maxWritePoolSize, host, port, queueName)
        {
        }

        /// <summary>
        /// Queue incoming messages
        /// </summary>
        /// <param name="msg"></param>
        public void Enqueue(T msg)
        {
            var key = queueNamespace.GlobalCacheKey(pendingWorkItemIdQueue);
            using (var disposableClient = clientManager.GetDisposableClient<SerializingRedisClient>())
            {
                var client = disposableClient.Client;
                client.RPush(key, client.Serialize(msg));
            }
        }


        /// <summary>
        /// Dequeue next batch of work items for processing. After this method is called,
        /// no other work items with same id will be available for
        /// dequeuing until PostDequeue is called
        /// </summary>
        /// <returns>KeyValuePair: key is work item id, and value is list of dequeued items.
        /// </returns>
        public IList<T> Dequeue(int maxBatchSize)
        {
            using (var disposableClient = clientManager.GetDisposableClient<SerializingRedisClient>())
            {
                var client = disposableClient.Client;
                var dequeueItems = new List<T>();
                using (var pipe = client.CreatePipeline())
                {
                    var key = queueNamespace.GlobalCacheKey(pendingWorkItemIdQueue);
                    for (var i = 0; i < maxBatchSize; ++i)
                    {
                        pipe.QueueCommand(
                            r => ((RedisNativeClient) r).LPop(key),
                            x =>
                                {
                                    if (x != null)
                                        dequeueItems.Add((T) client.Deserialize(x));
                                });
                        
                    }
                    pipe.Flush();

                }
                return dequeueItems;
            }
        }
    }
}