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
    public partial class RedisSequentialWorkQueue<T> 
    {
        public class DequeueManager
        {
            protected readonly RedisSequentialWorkQueue<T> workQueue;
            protected readonly string workItemId;
            protected readonly PooledRedisClientManager clientManager;
            protected readonly int numberOfDequeuedItems;
            protected int numberOfProcessedItems;
            private readonly DistributedLock myLock;
            private readonly string dequeueLockKey;
            private int dequeueLockTimeout = 300;
            private long lockExpire;

            public DequeueManager(PooledRedisClientManager clientManager, RedisSequentialWorkQueue<T> workQueue, string workItemId, string dequeueLockKey, int numberOfDequeuedItems, int dequeueLockTimeout) 
            {
                this.workQueue = workQueue;
                this.workItemId = workItemId;
                this.clientManager = clientManager;
                this.numberOfDequeuedItems = numberOfDequeuedItems;
                myLock = new DistributedLock();
                this.dequeueLockKey = dequeueLockKey;
                this.dequeueLockTimeout = dequeueLockTimeout;
            }

            public void DoneProcessedWorkItem()
            {
                numberOfProcessedItems++;
                if (numberOfProcessedItems == numberOfDequeuedItems)
                {
                    using (var disposable = new PooledRedisClientManager.DisposablePooledClient<SerializingRedisClient>(clientManager))
                    {
                        Unlock(disposable.Client);
                    }
                }
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="newWorkItem"></param>
            public void UpdateNextUnprocessed(T newWorkItem)
            {
                workQueue.Update(workItemId, numberOfProcessedItems, newWorkItem);
            }

            public long Lock(int acquisitionTimeout, IRedisClient client)
            {
                return myLock.Lock(dequeueLockKey, acquisitionTimeout, dequeueLockTimeout, out lockExpire, client);
            }

            public bool Unlock(IRedisClient client)
            {
                return PopAndUnlock(numberOfDequeuedItems, client);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="numProcessed"></param>
            /// <param name="client"></param>
            /// <returns></returns>
            public bool PopAndUnlock(int numProcessed, IRedisClient client)
            {
                if (numProcessed < 0)
                    numProcessed = 0;
                if (numProcessed > numberOfDequeuedItems)
                    numProcessed = numberOfDequeuedItems;

                //remove items from queue
                workQueue.Pop(workItemId, numProcessed);

                // unlock work queue id
                workQueue.Unlock(workItemId);
                return myLock.Unlock(dequeueLockKey, lockExpire, client);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="numProcessed"></param>
            /// <returns></returns>
            public bool PopAndUnlock(int numProcessed)
            {
                using (var disposable = new PooledRedisClientManager.DisposablePooledClient<SerializingRedisClient>(clientManager))
                {
                    return PopAndUnlock(numProcessed, disposable.Client);
                }
            }
        }
    }
}