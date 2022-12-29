using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests.Issues
{
    [Ignore("Can't be included in Unit tests since it shutsdown redis server")]
    [TestFixture]
    public class PooledRedisClientManagerIssues
        : RedisClientTestsBase
    {
        private static PooledRedisClientManager pool;

        public static void Stuff()
        {
            while (true)
            {
                RedisClient redisClient = null;
                try
                {
                    using (redisClient = (RedisClient)pool.GetClient())
                    {
                        redisClient.Set("test", DateTime.Now);
                    }
                }
                catch (NotSupportedException nse)
                {
                    Debug.WriteLine(redisClient.ToString());
                    Assert.Fail(nse.Message);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                Thread.Sleep(10);
            }
        }

        [Test]
        public void Issue37_Cannot_add_unknown_client_back_to_pool_exception()
        {
            pool = new PooledRedisClientManager();
            try
            {
                var threads = new Thread[100];
                for (var i = 0; i < threads.Length; i++)
                {
                    threads[i] = new Thread(Stuff);
                    threads[i].Start();
                }
                Debug.WriteLine("running, waiting 10secs..");
                Thread.Sleep(10000);
                using (var redisClient = (RedisClient)pool.GetClient())
                {
                    Debug.WriteLine("shutdown Redis!");
                    redisClient.Shutdown();
                }
            }
            catch (NotSupportedException nse)
            {
                Assert.Fail(nse.Message);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            Thread.Sleep(5000);
        }
    }
}