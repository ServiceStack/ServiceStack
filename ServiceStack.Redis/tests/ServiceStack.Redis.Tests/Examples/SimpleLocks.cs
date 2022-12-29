using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests.Examples
{
    [TestFixture, Ignore("Integration"), Category("Integration")]
    public class SimpleLocks
    {
        [SetUp]
        public void OnTestFixtureSetUp()
        {
            using (var redisClient = new RedisClient(TestConfig.SingleHost))
            {
                redisClient.FlushAll();
            }
        }

        [Test]
        public void Use_multiple_redis_clients_to_safely_execute()
        {
            //The number of concurrent clients to run
            const int noOfClients = 5;
            var asyncResults = new List<IAsyncResult>(noOfClients);
            for (var i = 1; i <= noOfClients; i++)
            {
                var clientNo = i;
                var actionFn = (Action)delegate
                {
                    var redisClient = new RedisClient(TestConfig.SingleHost);
                    using (redisClient.AcquireLock("testlock"))
                    {
                        Debug.WriteLine(String.Format("client {0} acquired lock", clientNo));
                        var counter = redisClient.Get<int>("atomic-counter");

                        //Add an artificial delay to demonstrate locking behaviour
                        Thread.Sleep(100);

                        redisClient.Set("atomic-counter", counter + 1);
                        Debug.WriteLine(String.Format("client {0} released lock", clientNo));
                    }
                };

                //Asynchronously invoke the above delegate in a background thread
                asyncResults.Add(actionFn.BeginInvoke(null, null));
            }

            //Wait at most 1 second for all the threads to complete
            asyncResults.WaitAll(TimeSpan.FromSeconds(1));

            //Print out the 'atomic-counter' result
            using (var redisClient = new RedisClient(TestConfig.SingleHost))
            {
                var counter = redisClient.Get<int>("atomic-counter");
                Debug.WriteLine(String.Format("atomic-counter after 1sec: {0}", counter));
            }
        }

        [Test]
        public void Acquiring_lock_with_timeout()
        {
            var redisClient = new RedisClient(TestConfig.SingleHost);

            //Initialize and set counter to '1'
            redisClient.IncrementValue("atomic-counter");

            //Acquire lock and never release it
            redisClient.AcquireLock("testlock");

            var waitFor = TimeSpan.FromSeconds(2);
            var now = DateTime.Now;

            try
            {
                using (var newClient = new RedisClient(TestConfig.SingleHost))
                {
                    //Attempt to acquire a lock with a 2 second timeout
                    using (newClient.AcquireLock("testlock", waitFor))
                    {
                        //If lock was acquired this would be incremented to '2'
                        redisClient.IncrementValue("atomic-counter");
                    }
                }
            }
            catch (TimeoutException tex)
            {
                var timeTaken = DateTime.Now - now;
                Debug.WriteLine(String.Format("After '{0}', Received TimeoutException: '{1}'", timeTaken, tex.Message));

                var counter = redisClient.Get<int>("atomic-counter");
                Debug.WriteLine(String.Format("atomic-counter remains at '{0}'", counter));
            }
        }

        [Test]
        public void SimulateLockTimeout()
        {
            var redisClient = new RedisClient(TestConfig.SingleHost);
            var waitFor = TimeSpan.FromMilliseconds(20);

            var loc = redisClient.AcquireLock("testlock", waitFor);
            Thread.Sleep(100); //should have lock expire
            using (var newloc = redisClient.AcquireLock("testlock", waitFor))
            {

            }
        }

        [Test]
        public void AcquireLock_using_Tasks()
        {
            const int noOfClients = 4;
            var tasks = new Task[noOfClients];
            for (var i = 0; i < noOfClients; i++)
            {
                Thread.Sleep(2000);
                tasks[i] = Task.Factory.StartNew((object clientNo) =>
                {
                    try
                    {
                        Console.WriteLine("About to process " + clientNo);
                        //var redisClient = new RedisClient("xxxx.redis.cache.windows.net", 6379, "xxxx");
                        var redisClient = new RedisClient(TestConfig.SingleHost, 6379);

                        using (redisClient.AcquireLock("testlock1", TimeSpan.FromMinutes(3)))
                        {
                            Console.WriteLine("client {0} acquired lock", (int)clientNo);
                            var counter = redisClient.Get<int>("atomic-counter");

                            //Add an artificial delay to demonstrate locking behaviour
                            Thread.Sleep(100);

                            redisClient.Set("atomic-counter", counter + 1);
                            Console.WriteLine("client {0} released lock", (int)clientNo);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }

                }, i + 1);
            }
        }

    }


}
