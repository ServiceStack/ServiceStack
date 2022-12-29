using System.Collections.Generic;


namespace ServiceStack.Redis.Support.Queue.Implementation
{
    /// <summary>
    /// distributed work item queue. Messages are processed in chronological order
    /// </summary>
    public class RedisChronologicalWorkQueue<T> : RedisWorkQueue<T>, IChronologicalWorkQueue<T> where T : class
    {

        public RedisChronologicalWorkQueue(int maxReadPoolSize, int maxWritePoolSize, string host, int port) : 
                                               this(maxReadPoolSize, maxWritePoolSize, host, port, null)
        {
           
        }

        public RedisChronologicalWorkQueue(int maxReadPoolSize, int maxWritePoolSize, string host, int port, string queueName) 
            : base(maxReadPoolSize, maxWritePoolSize, host, port, queueName)
        {
        }

        /// <summary>
        /// Enqueue incoming messages
        /// </summary>
        /// <param name="workItem"></param>
        /// <param name="workItemId"></param>
        /// <param name="time"></param>
        public void Enqueue(string workItemId, T workItem, double time)
        {
            using (var disposableClient = clientManager.GetDisposableClient<SerializingRedisClient>())
            {
                var client = disposableClient.Client;
                var workItemIdRaw = client.Serialize(workItemId);
                using (var pipe = client.CreatePipeline())
                {
                    pipe.QueueCommand(r => ((RedisNativeClient)r).HSet(workQueue, workItemIdRaw, client.Serialize(workItem)));
                    pipe.QueueCommand(r => ((RedisNativeClient)r).ZAdd(pendingWorkItemIdQueue, time, workItemIdRaw));
                    pipe.Flush();
                }
            }
        }
        

        /// <summary>
        /// Dequeue next batch of work items
        /// </summary>
        /// <param name="minTime"></param>
        /// <param name="maxTime"></param>
        /// <param name="maxBatchSize"></param>
        /// <returns></returns>
        public IList<KeyValuePair<string, T>> Dequeue(double minTime, double maxTime, int maxBatchSize)
        {
            using (var disposableClient = clientManager.GetDisposableClient<SerializingRedisClient>())
            {
                var client = disposableClient.Client;

                //1. get next work item batch 
                var dequeueItems = new List<KeyValuePair<string, T>>();
                var itemIds = client.ZRangeByScore(pendingWorkItemIdQueue, minTime, maxTime, null, maxBatchSize);
                if (itemIds != null && itemIds.Length > 0 )
                {
                   
                    var rawItems = client.HMGet(workQueue, itemIds);
                    IList<byte[]> toDelete = new List<byte[]>();
                    for (int i = 0; i < itemIds.Length; ++i)
                    {
                        dequeueItems.Add(new KeyValuePair<string, T>(client.Deserialize(itemIds[i]) as string, 
                                                                     client.Deserialize(rawItems[i]) as T));
                        toDelete.Add( itemIds[i]);
                    }

                    //delete batch of keys
                    using (var pipe = client.CreatePipeline())
                    {
                        foreach (var rawId in toDelete)
                        {
                            var myRawId = rawId;
                            pipe.QueueCommand(r => ((RedisNativeClient)r).HDel(workQueue, myRawId));
                            pipe.QueueCommand(r => ((RedisNativeClient)r).ZRem(pendingWorkItemIdQueue, myRawId));          
                        }
                        pipe.Flush();
                    }
                }
                return dequeueItems;
            }
        }
    }
}