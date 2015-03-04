using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Logging;
using ServiceStack.Messaging.Redis;
using ServiceStack.Redis;
using ServiceStack.Text;
using RedisMessageQueueClient = ServiceStack.Messaging.RedisMessageQueueClient;

namespace ServiceStack.Server.Tests.Messaging
{
    [Explicit("Simulating error conditions")]
    [TestFixture]
    public class RedisFailoverTests
    {
        [Test]
        public void Can_recover_from_server_terminated_client_connection()
        {
            const int SleepHoldingClientMs = 5;
            const int SleepAfterReleasingClientMs = 0;
            const int loop = 1000;

            var admin = new RedisClient("localhost");
            admin.SetConfig("timeout", "0");
            var timeout = admin.GetConfig("timeout");
            timeout.Print("timeout: {0}");

            int remaining = loop;
            var stopwatch = Stopwatch.StartNew();

            var clientManager = new PooledRedisClientManager(new[] { "localhost" })
                {
                    
                };
            loop.Times(i =>
                {
                    ThreadPool.QueueUserWorkItem(x =>
                    {
                        try
                        {
                            using (var client = (RedisClient)clientManager.GetClient())
                            {
                                client.IncrementValue("key");
                                var val = client.Get<long>("key");
                                "#{0}, isConnected: {1}".Print(val, true); //client.IsSocketConnected()
                                Thread.Sleep(SleepHoldingClientMs);
                            }
                            Thread.Sleep(SleepAfterReleasingClientMs);
                        }
                        catch (Exception ex)
                        {
                            ex.Message.Print();
                        }
                        finally
                        {
                            remaining--;
                        }
                    });
                });

            while (remaining > 0)
            {
                Thread.Sleep(10);
            }
            "Elapsed time: {0}ms".Print(stopwatch.ElapsedMilliseconds);

            var managerStats = clientManager.GetStats();
            managerStats.PrintDump();
        }

        public class Incr
        {
            public int Value { get; set; }
        }

        [Test]
        public void Can_MqServer_recover_from_server_terminated_client_connections()
        {
            LogManager.LogFactory = new ConsoleLogFactory();

            var clientManager = new PooledRedisClientManager(new[] { "localhost" })
                {
                    
                };
            var mqHost = new RedisMqServer(clientManager, retryCount: 2);

            var sum = 0;
            mqHost.RegisterHandler<Incr>(c =>
                {
                    var dto = c.GetBody();
                    sum += dto.Value;
                    "Received {0}, sum: {1}".Print(dto.Value, sum); 
                    return null;
                });

            mqHost.Start();

            10.Times(i =>
                {
                    ThreadPool.QueueUserWorkItem(x => { 
                        using (var client = mqHost.CreateMessageQueueClient())
                        {
                            "Publish: {0}...".Print(i);
                            client.Publish(new Incr { Value = i });
                            
                            Thread.Sleep(10);
                        }
                    });
            });

            ThreadPool.QueueUserWorkItem(_ =>
                {
                    using (var client = (RedisClient)clientManager.GetClient())
                    {
                        client.SetConfig("timeout", "1");
                        var clientAddrs = client.GetClientsInfo().ConvertAll(x => x["addr"]);
                        "Killing clients: {0}...".Print(clientAddrs.Dump());
                        try
                        {
                            clientAddrs.ForEach(client.ClientKill);
                        }
                        catch (Exception ex)
                        {
                            "Client exception: {0}".Print(ex.Message);
                        }
                    }
                });

            20.Times(i =>
            {
                using (var client = mqHost.CreateMessageQueueClient())
                {
                    "Publish: {0}...".Print(i);
                    client.Publish(new Incr { Value = i });
                }

                Thread.Sleep(2000);
            });

        }

        [Test]
        public void Can_failover_at_runtime()
        {
            var failoverHost = "redis-failover:6379";
            string key = "test:failover";

            var localClient = new RedisClient("localhost");
            localClient.Remove(key);
            var failoverClient = new RedisClient(failoverHost);
            failoverClient.Remove(key);

            var clientManager = new PooledRedisClientManager(new[] { "localhost" });

            RunInLoop(clientManager, callback:() =>
                {
                    lock (clientManager)
                        Monitor.Pulse(clientManager);
                });

            Thread.Sleep(100);

            clientManager.FailoverTo(failoverHost);

            lock (clientManager)
                Monitor.Wait(clientManager);

            var localIncr = localClient.Get<int>(key);
            var failoverIncr = failoverClient.Get<int>(key);
            Assert.That(localIncr, Is.GreaterThan(0));
            Assert.That(failoverIncr, Is.GreaterThan(0));
            Assert.That(localIncr + failoverIncr, Is.EqualTo(100));
        }

        public static bool RunInLoop(PooledRedisClientManager clientManager, int iterations = 100, int sleepMs = 10, Action callback=null)
        {
            int count = 0;
            int errors = 0;
            
            10.Times(i =>
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    while (Interlocked.Decrement(ref iterations) >= 0)
                    {
                        using (var client = clientManager.GetClient())
                        {
                            try
                            {
                                var result = client.Increment("test:failover", 1);
                                Interlocked.Increment(ref count);
                                if (count % (iterations / 10) == 0)
                                    lock (clientManager)
                                        Console.WriteLine("count: {0}, errors: {1}", count, errors);
                            }
                            catch (Exception ex)
                            {
                                Interlocked.Increment(ref errors);
                            }
                            Thread.Sleep(sleepMs);
                        }
                    }

                    if (callback != null)
                    {
                        callback();
                        callback = null;
                    }
                });
            });

            return true;
        }


        public class Msg
        {
            public string Host { get; set; }
        }

        [Test]
        public void Can_failover_MqServer_at_runtime()
        {
            const int iterations = 100;
            var failoverHost = "redis-failover:6379";
            var localClient = new RedisClient("localhost:6379");

            localClient.FlushDb();
            var failoverClient = new RedisClient(failoverHost);
            failoverClient.FlushDb();

            var clientManager = new PooledRedisClientManager(new[] { "localhost" });
            var mqHost = new RedisMqServer(clientManager);

            var map = new Dictionary<string, int>();
            var received = 0;
            mqHost.RegisterHandler<Msg>(c =>
            {
                var dto = c.GetBody();
                received++;
                int count;
                map.TryGetValue(dto.Host, out count);
                map[dto.Host] = count + 1;

                lock (clientManager)
                {
                    "Received #{0} from {1}".Print(received, dto.Host);
                    if (received == iterations)
                        Monitor.Pulse(clientManager);
                }

                return null;
            });

            mqHost.Start();

            RunMqInLoop(mqHost, iterations: iterations, callback: () =>
            {
                lock (clientManager)
                    "{0} msgs were published.".Print(iterations);
            });

            Thread.Sleep(500);

            clientManager.FailoverTo(failoverHost);

            lock (clientManager)
                Monitor.Wait(clientManager);

            map.PrintDump();
            "localclient inq: {0}, outq: {1}".Print(
                localClient.GetListCount("mq:Msg.inq"),
                localClient.GetListCount("mq:Msg.outq"));
            "failoverClient inq: {0}, outq: {1}".Print(
                failoverClient.GetListCount("mq:Msg.inq"),
                failoverClient.GetListCount("mq:Msg.outq"));

            Assert.That(received, Is.EqualTo(100));
            Assert.That(map.Count, Is.EqualTo(2));
            var msgsFromAllHosts = 0;
            foreach (var count in map.Values)
            {
                Assert.That(count, Is.GreaterThan(0));
                msgsFromAllHosts += count;
            }
            Assert.That(msgsFromAllHosts, Is.EqualTo(iterations));
        }

        public static bool RunMqInLoop(RedisMqServer mqServer, int iterations = 100, int sleepMs = 10, Action callback = null)
        {
            int count = 0;
            int errors = 0;

            10.Times(i =>
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    while (Interlocked.Decrement(ref iterations) >= 0)
                    {
                        using (var client = mqServer.CreateMessageQueueClient())
                        {
                            try
                            {
                                var redis = (RedisNativeClient)((RedisMessageQueueClient)client).ReadWriteClient;

                                client.Publish(new Msg { Host = redis.Host + ":" + redis.Port });
                                Interlocked.Increment(ref count);
                                if (count % (iterations / 10) == 0)
                                    lock (mqServer)
                                        "count: {0}, errors: {1}".Print(count, errors);
                            }
                            catch (Exception ex)
                            {
                                Interlocked.Increment(ref errors);
                            }
                            Thread.Sleep(sleepMs);
                        }
                    }

                    lock (mqServer)
                    {
                        if (callback != null)
                        {
                            callback();
                            callback = null;
                        }
                    }
                });
            });

            return true;
        }
    }
}