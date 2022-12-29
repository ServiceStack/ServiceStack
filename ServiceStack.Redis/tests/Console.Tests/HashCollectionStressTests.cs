using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ServiceStack.Redis;
using ServiceStack.Redis.Generic;
using ServiceStack.Text;

namespace ConsoleTests
{
    public class HashCollectionStressTests
    {
        private IRedisClientsManager clientsManager;
        private RedisCachedCollection<string, string> redisCollection;
        
        private int running = 0;
        private long writeCount = 0;
        private long readCount = 0;

        public void Execute(string ipAddress, int noOfThreads = 64)
        {
            clientsManager = new PooledRedisClientManager(ipAddress);
    
            redisCollection = new RedisCachedCollection<string, string>(
                clientsManager, "Threads: " + 64);

            var StartedAt = DateTime.UtcNow;
            Interlocked.Increment(ref running);

            "Starting HashCollectionStressTests with {0} threads".Print(noOfThreads);
            var threads = new List<Thread>();
            for (int i = 0; i < noOfThreads; i++)
            {
                threads.Add(new Thread(WorkerLoop));
            }
            threads.ForEach(t => t.Start());

            "Press Enter to Stop...".Print();
            Console.ReadLine();

            Interlocked.Decrement(ref running);

            "Writes: {0}, Reads: {1}".Print(writeCount, readCount);
            "{0} EndedAt: {1}".Print(GetType().Name, DateTime.UtcNow.ToLongTimeString());
            "{0} TimeTaken: {1}s".Print(GetType().Name, (DateTime.UtcNow - StartedAt).TotalSeconds);

            "\nPress Enter to Quit...".Print();
            Console.ReadLine();
        }

        public void WorkerLoop()
        {
            while (Interlocked.CompareExchange(ref running, 0, 0) > 0)
            {
                redisCollection.ContainsKey("key");
                Interlocked.Increment(ref readCount);

                redisCollection["key"] = "value " + readCount;
                Interlocked.Increment(ref writeCount);

                var value = redisCollection["key"];
                Interlocked.Increment(ref readCount);

                if (value == null)
                    Console.WriteLine("value == null");
            }
        }
    }

    public class RedisCachedCollection<TKey, TValue> : IEnumerable<TValue>
    {
        private readonly string collectionKey;
        private Func<TValue, TKey> idAction;
        private readonly IRedisClientsManager clientsManager;

        public RedisCachedCollection(IRedisClientsManager clientsManager, string collectionKey)
        {
            this.clientsManager = clientsManager;
            this.collectionKey = string.Format("urn:{0}:{1}", "XXXXX", collectionKey);
        }

        public IRedisClient RedisConnection
        {
            get
            {
                return clientsManager.GetClient();
            }
        }

        private IRedisHash<TKey, TValue> GetCollection(IRedisClient redis)
        {
            var _redisTypedClient = redis.As<TValue>();
            return _redisTypedClient.GetHash<TKey>(collectionKey);
        }

        public void Add(TValue obj)
        {
            TKey Id = GetUniqueIdAction(obj);

            RetryAction((redis) =>
            {
                GetCollection(redis).Add(Id, obj);
            });
        }

        public bool Remove(TValue obj)
        {
            TKey Id = GetUniqueIdAction(obj);
            TKey defaultv = default(TKey);

            return RetryAction<bool>((redis) =>
            {
                if (!Id.Equals(defaultv))
                {
                    {
                        return GetCollection(redis).Remove(Id);
                    }
                }
                return false;
            });

        }

        public TValue this[TKey id]
        {
            get
            {
                return RetryAction<TValue>((redis) =>
                {
                    if (GetCollection(redis).ContainsKey(id))
                        return GetCollection(redis)[id];
                    return default(TValue);
                });
            }
            set
            {
                RetryAction((redis) =>
                {
                    GetCollection(redis)[id] = value;
                });
            }
        }
        public int Count
        {
            get
            {
                return RetryAction<int>((redis) =>
                {
                    return GetCollection(redis).Count;
                });
            }
        }

        public IEnumerable<TValue> Where(Func<TValue, bool> predicate)
        {
            return RetryAction((redis) =>
            {
                return GetCollection(redis).Values.Where(predicate);
            });
        }

        public bool Any(Func<TValue, bool> predicate)
        {
            return RetryAction<bool>((redis) =>
            {
                return GetCollection(redis).Values.Any(predicate);
            });
        }


        public IEnumerator<TValue> GetEnumerator()
        {
            return RetryAction<IEnumerator<TValue>>((redis) =>
            {
                return GetCollection(redis).Values.GetEnumerator();
            });
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return RetryAction(redis =>
            {
                return ((System.Collections.IEnumerable)GetCollection(redis).Values).GetEnumerator();
            });

        }

        public void Clear()
        {
            RetryAction((redis) =>
            {
                GetCollection(redis).Clear();
            });
        }

        public bool Contains(TValue obj)
        {
            TKey Id = GetUniqueIdAction(obj);
            return RetryAction<bool>((redis) =>
            {
                return GetCollection(redis).ContainsKey(Id);
            });
        }

        public bool ContainsKey(TKey obj)
        {
            return RetryAction<bool>((redis) =>
            {
                return GetCollection(redis).ContainsKey(obj);
            });
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            RetryAction((redis) =>
            {
                GetCollection(redis).Values.CopyTo(array, arrayIndex);
            });
        }

        public bool IsReadOnly
        {
            get
            {
                return RetryAction<bool>((redis) =>
                {
                    return GetCollection(redis).IsReadOnly;
                });
            }
        }

        public Func<TValue, TKey> GetUniqueIdAction
        {
            get
            {
                return idAction;
            }
            set
            {
                idAction = value;
            }
        }

        private void RetryAction(Action<IRedisClient> action)
        {
            try
            {
                using (var redis = RedisConnection)
                {
                    action(redis);
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        private TOut RetryAction<TOut>(Func<IRedisClient, TOut> action)
        {
            int i = 0;

            while (true)
            {
                try
                {
                    using (var redis = RedisConnection)
                    {
                        TOut result = action(redis);
                        return result;
                    }
                }
                catch (Exception)
                {

                    if (i++ < 3)
                    {

                        continue;
                    }

                    throw;
                }
            }
        }
    }
}