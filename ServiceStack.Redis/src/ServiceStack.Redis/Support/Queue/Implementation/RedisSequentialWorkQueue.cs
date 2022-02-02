using System;
using System.Collections.Generic;
using System.Diagnostics;
using ServiceStack.Redis.Support.Locking;


namespace ServiceStack.Redis.Support.Queue.Implementation
{
    /// <summary>
    /// distributed work item queue. Each message must have an associated
    /// work item  id. For a given id, all work items are guaranteed to be processed
    /// in the order in which they are received.
    /// 
    /// 
    /// </summary>
    public partial class RedisSequentialWorkQueue<T> : RedisWorkQueue<T>, ISequentialWorkQueue<T> where T : class
    {
        private DateTime harvestTime = DateTime.UtcNow;
        private int lockAcquisitionTimeout = 2;
        private int lockTimeout = 2;
        protected const double  CONVENIENTLY_SIZED_FLOAT = 18014398509481984.0;

        // store list of work item ids that have been dequeued
        // this list is checked regularly in harvest zombies method
        private string dequeueIdSet;
        private int dequeueLockTimeout = 300;

        private string workItemIdPriorityQueue;

        private const int numTagsForDequeueLock = RedisNamespace.NumTagsForLockKey + 1;

        public RedisSequentialWorkQueue(int maxReadPoolSize, int maxWritePoolSize, string host, int port, int dequeueLockTimeout)
            : this(maxReadPoolSize, maxWritePoolSize, host, port,null, dequeueLockTimeout)
        {
        }

        public RedisSequentialWorkQueue(int maxReadPoolSize, int maxWritePoolSize, string host, int port, string queueName, int dequeueLockTimeout) 
            : base(maxReadPoolSize, maxWritePoolSize, host, port, queueName)
        {
            dequeueIdSet = queueNamespace.GlobalCacheKey("DequeueIdSet");
            workItemIdPriorityQueue = queueNamespace.GlobalCacheKey("WorkItemIdPriorityQueue");
            this.dequeueLockTimeout = dequeueLockTimeout;
        }

        /// <summary>
        /// Queue incoming messages
        /// </summary>
        /// <param name="workItem"></param>
        /// <param name="workItemId"></param>
        public void Enqueue(string workItemId, T workItem)
        {
            using (var disposableClient = clientManager.GetDisposableClient<SerializingRedisClient>())
            {
                var client = disposableClient.Client;
                var lockKey = queueNamespace.GlobalLockKey(workItemId);
                using (var disposableLock = new DisposableDistributedLock(client, lockKey, lockAcquisitionTimeout, lockTimeout))
                {
                    using (var pipe = client.CreatePipeline())
                    {
                        pipe.QueueCommand(r => ((RedisNativeClient)r).RPush(queueNamespace.GlobalCacheKey(workItemId), client.Serialize(workItem)));
                        pipe.QueueCommand(r => ((RedisNativeClient)r).ZIncrBy(workItemIdPriorityQueue, -1, client.Serialize(workItemId)));
                        pipe.Flush();
                    }
                }
            }
        }

        /// <summary>
        /// Must call this periodically to move work items from priority queue to pending queue
        /// </summary>
        public bool PrepareNextWorkItem()
        {
            //harvest zombies every 5 minutes
            var now = DateTime.UtcNow;
            var ts = now - harvestTime;
            if (ts.TotalMinutes > 5)
            {
                HarvestZombies();
                harvestTime = now;
            }

            using (var disposableClient = clientManager.GetDisposableClient<SerializingRedisClient>())
            {
                var client = disposableClient.Client;

                //1. get next workItemId, or return if there isn't one
                var smallest = client.ZRangeWithScores(workItemIdPriorityQueue, 0, 0);
                if (smallest == null || smallest.Length <= 1 ||
                    RedisNativeClient.ParseDouble(smallest[1]) == CONVENIENTLY_SIZED_FLOAT) return false;
                var workItemId = client.Deserialize(smallest[0]) as string;

                // lock work item id
                var lockKey = queueNamespace.GlobalLockKey(workItemId);
                using (var disposableLock = new DisposableDistributedLock(client, lockKey, lockAcquisitionTimeout, lockTimeout))
                {
                    // if another client has queued this work item id,
                    // then the work item id score will be set to CONVENIENTLY_SIZED_FLOAT
                    // so we return false in this case
                    var score = client.ZScore(workItemIdPriorityQueue, smallest[0]);
                    if (score == CONVENIENTLY_SIZED_FLOAT) return false;

                    using (var pipe = client.CreatePipeline())
                    {
                        var rawWorkItemId = client.Serialize(workItemId);

                        // lock work item id in priority queue
                        pipe.QueueCommand(
                            r =>
                            ((RedisNativeClient) r).ZAdd(workItemIdPriorityQueue, CONVENIENTLY_SIZED_FLOAT, smallest[0]));

                        // track dequeue lock id
                        pipe.QueueCommand(r => ((RedisNativeClient) r).SAdd(dequeueIdSet, rawWorkItemId));

                        // push into pending set
                        pipe.QueueCommand(r => ((RedisNativeClient) r).LPush(pendingWorkItemIdQueue, rawWorkItemId));

                        pipe.Flush();
                    }
                }
            }
            return true;
        }
 
        public ISequentialData<T> Dequeue(int maxBatchSize)
        {
 
            using (var disposableClient = clientManager.GetDisposableClient<SerializingRedisClient>())
            {
                var client = disposableClient.Client;

                //1. get next workItemId 
                var workItems = new List<T>();
                DequeueManager workItemDequeueManager = null;
                try
                {
                    var rawWorkItemId = client.RPop(pendingWorkItemIdQueue);
                    var workItemId = client.Deserialize(rawWorkItemId) as string;;
                    if (rawWorkItemId != null)
                    {
                        using (var pipe = client.CreatePipeline())
                        {
                            // dequeue items
                            var key = queueNamespace.GlobalCacheKey(workItemId);
                            Action<byte[]> dequeueCallback = x =>
                                                                 {
                                                                     if (x != null)
                                                                         workItems.Add((T) client.Deserialize(x));
                                                                 };

                            for (var i = 0; i < maxBatchSize; ++i)
                            {
                                int index = i;
                                pipe.QueueCommand(
                                    r => ((RedisNativeClient) r).LIndex(key, index),
                                    dequeueCallback);

                            }
                            pipe.Flush();
                        }

                        workItemDequeueManager = new DequeueManager(clientManager, this, workItemId, GlobalDequeueLockKey(workItemId), workItems.Count, dequeueLockTimeout);
                        // don't lock if there are no work items to be processed (can't lock on null lock key)
                        if (workItems.Count > 0)
                           workItemDequeueManager.Lock(lockAcquisitionTimeout, client);

                    }
                    return new SequentialData<T>(workItemId, workItems, workItemDequeueManager);
                           
                }
                catch (Exception)
                {
                    //release resources
                    if (workItemDequeueManager != null)
                        workItemDequeueManager.Unlock(client);

                    throw;
                }
            }
        }

        /// <summary>
        /// Replace existing work item in workItemId queue
        /// </summary>
        /// <param name="workItemId"></param>
        /// <param name="index"></param>
        /// <param name="newWorkItem"></param>
        public void Update(string workItemId, int index, T newWorkItem)
        {
            using (var disposableClient = clientManager.GetDisposableClient<SerializingRedisClient>())
            {
                var client = disposableClient.Client;
                var key = queueNamespace.GlobalCacheKey(workItemId);
                client.LSet(key, index, client.Serialize(newWorkItem));
            }
        }

        /// <summary>
        /// Pop items from list
        /// </summary>
        /// <param name="workItemId"></param>
        /// <param name="itemCount"></param>
        private void Pop(string workItemId, int itemCount)
        {
            if (itemCount <= 0)
                return;

            using (var disposableClient = clientManager.GetDisposableClient<SerializingRedisClient>())
            {
                var client = disposableClient.Client;
                using (var pipe = client.CreatePipeline())
                {
                    var key = queueNamespace.GlobalCacheKey(workItemId);
                    for (var i = 0; i < itemCount; ++i)
                        pipe.QueueCommand(r => ((RedisNativeClient)r).LPop(key));

                    pipe.Flush();
                }
            }
        }

        /// <summary>
        /// Force release of locks held by crashed servers
        /// </summary>
        public bool HarvestZombies()
        {
            bool rc = false;
            using (var disposableClient = clientManager.GetDisposableClient<SerializingRedisClient>())
            {
                var client = disposableClient.Client;
                var dequeueWorkItemIds = client.SMembers(dequeueIdSet);
                if (dequeueWorkItemIds.Length == 0) return false;

                var keys = new string[dequeueWorkItemIds.Length];
                for (int i = 0; i < dequeueWorkItemIds.Length; ++i)
                    keys[i] = GlobalDequeueLockKey(client.Deserialize(dequeueWorkItemIds[i]));
                var dequeueLockVals = client.MGet(keys);

                var ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
                for (int i = 0; i < dequeueLockVals.Length; ++i)
                {
                    double lockValue = (dequeueLockVals[i] != null) ? BitConverter.ToInt64(dequeueLockVals[i], 0) : 0;
                    if (lockValue < ts.TotalSeconds)
                        rc |= TryForceReleaseLock(client, (string) client.Deserialize(dequeueWorkItemIds[i]));
                }
            }
            return rc;
        }


        /// <summary>
        /// release lock held by crashed server
        /// </summary>
        /// <param name="client"></param>
        /// <param name="workItemId"></param>
        /// <returns>true if lock is released, either by this method or by another client; false otherwise</returns>
        public bool TryForceReleaseLock(SerializingRedisClient client, string workItemId)
        {
            if (workItemId == null)
                return false;
            
            var rc = false;

            var dequeueLockKey = GlobalDequeueLockKey(workItemId);
            // handle possibliity of crashed client still holding the lock
            long lockValue = 0;
            using (var pipe = client.CreatePipeline())
            {
                
                pipe.QueueCommand(r => ((RedisNativeClient) r).Watch(dequeueLockKey));
                pipe.QueueCommand(r => ((RedisNativeClient) r).Get(dequeueLockKey),
                                  x => lockValue = (x != null) ? BitConverter.ToInt64(x, 0) : 0);
                pipe.Flush();
            }

            var ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            // no lock to release
            if (lockValue == 0)
            {
                client.UnWatch();
            }
            //lock still fresh
            else if (lockValue >= ts.TotalSeconds)
            {
                client.UnWatch();
            }
            else
            {
                // lock value is expired; try to release it, and other associated resources
                var len = client.LLen(queueNamespace.GlobalCacheKey(workItemId));
                using (var trans = client.CreateTransaction())
                {
                    //untrack dequeue lock
                    trans.QueueCommand(r => ((RedisNativeClient)r).SRem(dequeueIdSet, client.Serialize(workItemId)));

                    //delete dequeue lock
                    trans.QueueCommand(r => ((RedisNativeClient)r).Del(dequeueLockKey));
                    
                    // update priority queue : this will allow other clients to access this workItemId
                    if (len == 0)
                        trans.QueueCommand(r => ((RedisNativeClient)r).ZRem(workItemIdPriorityQueue, client.Serialize(workItemId)));
                    else
                        trans.QueueCommand(r => ((RedisNativeClient)r).ZAdd(workItemIdPriorityQueue, len, client.Serialize(workItemId)));

                    rc = trans.Commit();
                }

            }
            return rc;
        }

        /// <summary>
        /// Unlock work item id, so other servers can process items for this id
        /// </summary>
        /// <param name="workItemId"></param>
        private void Unlock(string workItemId)
        {
            if (workItemId == null)
                return;

            var key = queueNamespace.GlobalCacheKey(workItemId);
            var lockKey = queueNamespace.GlobalLockKey(workItemId);

            using (var disposableClient = clientManager.GetDisposableClient<SerializingRedisClient>())
            {
                var client = disposableClient.Client;
                using (var disposableLock = new DisposableDistributedLock(client, lockKey, lockAcquisitionTimeout, lockTimeout))
                {
                    var len = client.LLen(key);
                    using (var pipe = client.CreatePipeline())
                    {
                        //untrack dequeue lock
                        pipe.QueueCommand(r => ((RedisNativeClient)r).SRem(dequeueIdSet, client.Serialize(workItemId)));

                        // update priority queue
                        if (len == 0)
                            pipe.QueueCommand(r => ((RedisNativeClient)r).ZRem(workItemIdPriorityQueue, client.Serialize(workItemId)));
                        else
                            pipe.QueueCommand(r => ((RedisNativeClient)r).ZAdd(workItemIdPriorityQueue, len, client.Serialize(workItemId)));


                        pipe.Flush();
                    }
                   
                }
            }
        }
        private string GlobalDequeueLockKey(object key)
        {
            return queueNamespace.GlobalKey(key, numTagsForDequeueLock) + "_DEQUEUE_LOCK";
        }
    }
}