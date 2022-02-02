using System;
using System.Collections.Generic;
using System.Threading;
using ServiceStack.Redis;
using ServiceStack.Redis.Generic;
using ServiceStack.Text;

namespace ConsoleTests
{
    class DeviceInfo
    {
        public Guid PlayerID { get; set; }
        public DateTime? LastErrTime { get; set; }
        public DateTime? LastWarnTime { get; set; }

        protected bool Equals(DeviceInfo other)
        {
            return PlayerID.Equals(other.PlayerID) 
                && LastErrTime.Equals(other.LastErrTime) 
                && LastWarnTime.Equals(other.LastWarnTime);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DeviceInfo) obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class HashStressTest
    {
        public RedisManagerPool redisManager;
        private DeviceInfo data = new DeviceInfo
        {
            PlayerID = new Guid("560531b06bc945b688f3a6a8ade65354"),
            LastErrTime = new DateTime(2000, 1, 1),
            LastWarnTime = new DateTime(2001, 1, 1),
        };

        private int running = 0;
        private string _collectionKey = typeof (HashStressTest).Name;
        private TimeSpan? waitBeforeRetry = null;
        //private TimeSpan? waitBeforeRetry = TimeSpan.FromMilliseconds(1);

        private long writeCount = 0;
        private long readCount = 0;

        public void Execute(string ipAddress, int noOfThreads = 64)
        {
            redisManager = new RedisManagerPool(new[]{ ipAddress }, new RedisPoolConfig {
                MaxPoolSize = noOfThreads
            });

            var StartedAt = DateTime.UtcNow;
            Interlocked.Increment(ref running);

            "Starting HashStressTest with {0} threads".Print(noOfThreads);
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

            //Uncomment to wait for all threads to finish
            //threads.Each(t => t.Join());

            "\nPress Enter to Quit...".Print();
            Console.ReadLine();
        }

        public void WorkerLoop()
        {
            while (Interlocked.CompareExchange(ref running, 0, 0) > 0)
            {
                using (var client = redisManager.GetClient())
                {
                    try
                    {
                        GetCollection<Guid, DeviceInfo>(client)[data.PlayerID] = data;
                        Interlocked.Increment(ref writeCount);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("WRITE ERROR: " + ex.Message);
                    }

                    try
                    {
                        var readData = GetCollection<Guid, DeviceInfo>(client)[data.PlayerID];
                        Interlocked.Increment(ref readCount);

                        if (!readData.Equals(data))
                        {
                            Console.WriteLine("Data Error: " + readData.Dump());
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("READ ERROR: " + ex.Message);
                    }
                }

                if (waitBeforeRetry != null)
                    Thread.Sleep(waitBeforeRetry.Value);
            }
        }

        private IRedisHash<TKey, TValue> GetCollection<TKey, TValue>(IRedisClient redis)
        {
            var _redisTypedClient = redis.As<TValue>();
            return _redisTypedClient.GetHash<TKey>(_collectionKey);
        }
    }
}