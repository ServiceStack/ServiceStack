using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Text;
#if NETCORE
using System.Threading.Tasks;
#endif

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Category("Integration")]
    public class PooledRedisClientManagerTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            RedisConfig.VerifyMasterConnections = false;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            RedisConfig.VerifyMasterConnections = true;
        }

        readonly string[] testReadWriteHosts = new[] {
            "readwrite1", "readwrite2:6000", "192.168.0.1", "localhost"
        };

        readonly string[] testReadOnlyHosts = new[] {
            "read1", "read2:7000", "127.0.0.1"
        };

        private string firstReadWriteHost;
        private string firstReadOnlyHost;

        [SetUp]
        public void OnBeforeEachTest()
        {
            firstReadWriteHost = testReadWriteHosts[0];
            firstReadOnlyHost = testReadOnlyHosts[0];
        }

        public PooledRedisClientManager CreateManager(string[] readWriteHosts, string[] readOnlyHosts, int? defaultDb = null)
        {
            return new PooledRedisClientManager(readWriteHosts, readOnlyHosts,
                new RedisClientManagerConfig
                {
                    MaxWritePoolSize = readWriteHosts.Length,
                    MaxReadPoolSize = readOnlyHosts.Length,
                    AutoStart = false,
                    DefaultDb = defaultDb
                });
        }
        public PooledRedisClientManager CreateManager(params string[] readWriteHosts)
        {
            return CreateManager(readWriteHosts, readWriteHosts);
        }

        public PooledRedisClientManager CreateManager()
        {
            return CreateManager(testReadWriteHosts, testReadOnlyHosts);
        }

        public PooledRedisClientManager CreateAndStartManager()
        {
            var manager = CreateManager();
            manager.Start();
            return manager;
        }

        [Test]
        public void Cant_get_client_without_calling_Start()
        {
            using (var manager = CreateManager())
            {
                try
                {
                    var client = manager.GetClient();
                }
                catch (InvalidOperationException)
                {
                    return;
                }
                Assert.Fail("Should throw");
            }
        }

        [Test]
        public void Can_change_db_for_client_PooledRedisClientManager()
        {
            using (var db1 = new PooledRedisClientManager(1, new string[] { TestConfig.SingleHost }))
            using (var db2 = new PooledRedisClientManager(2, new string[] { TestConfig.SingleHost }))
            {
                var val = Environment.TickCount;
                var key = "test" + val;
                var db1c = db1.GetClient();
                var db2c = db2.GetClient();
                try
                {
                    db1c.Set(key, val);
                    Assert.That(db2c.Get<int>(key), Is.EqualTo(0));
                    Assert.That(db1c.Get<int>(key), Is.EqualTo(val));
                }
                finally
                {
                    db1c.Remove(key);
                }
            }
        }

        [Test]
        public void Can_change_db_for_client_RedisManagerPool()
        {
            using (var db1 = new RedisManagerPool(TestConfig.SingleHost + "?db=1"))
            using (var db2 = new RedisManagerPool(TestConfig.SingleHost + "?db=2"))
            {
                var val = Environment.TickCount;
                var key = "test" + val;
                var db1c = db1.GetClient();
                var db2c = db2.GetClient();
                try
                {
                    db1c.Set(key, val);
                    Assert.That(db2c.Get<int>(key), Is.EqualTo(0));
                    Assert.That(db1c.Get<int>(key), Is.EqualTo(val));
                }
                finally
                {
                    db1c.Remove(key);
                }
            }
        }

        [Test]
        public void Can_change_db_for_client_BasicRedisClientManager()
        {
            using (var db1 = new BasicRedisClientManager(1, new string[] { TestConfig.SingleHost }))
            using (var db2 = new BasicRedisClientManager(2, new string[] { TestConfig.SingleHost }))
            {
                var val = Environment.TickCount;
                var key = "test" + val;
                var db1c = db1.GetClient();
                var db2c = db2.GetClient();
                try
                {
                    db1c.Set(key, val);
                    Assert.That(db2c.Get<int>(key), Is.EqualTo(0));
                    Assert.That(db1c.Get<int>(key), Is.EqualTo(val));
                }
                finally
                {
                    db1c.Remove(key);
                }
            }
        }

        [Test]
        public void Can_get_client_after_calling_Start()
        {
            using (var manager = CreateManager())
            {
                manager.Start();
                var client = manager.GetClient();
            }
        }

        [Test]
        public void Can_get_ReadWrite_client()
        {
            using (var manager = CreateAndStartManager())
            {
                var client = manager.GetClient();

                AssertClientHasHost(client, firstReadWriteHost);
            }
        }

        private static void AssertClientHasHost(IRedisClient client, string hostWithOptionalPort)
        {
            var parts = hostWithOptionalPort.Split(':');
            var port = parts.Length > 1 ? int.Parse(parts[1]) : RedisConfig.DefaultPort;

            Assert.That(client.Host, Is.EqualTo(parts[0]));
            Assert.That(client.Port, Is.EqualTo(port));
        }

        [Test]
        public void Can_get_ReadOnly_client()
        {
            using (var manager = CreateAndStartManager())
            {
                var client = manager.GetReadOnlyClient();

                AssertClientHasHost(client, firstReadOnlyHost);
            }
        }

        [Test]
        public void Does_loop_through_ReadWrite_hosts()
        {
            using (var manager = CreateAndStartManager())
            {
                var client1 = manager.GetClient();
                client1.Dispose();
                var client2 = manager.GetClient();
                var client3 = manager.GetClient();
                var client4 = manager.GetClient();
                var client5 = manager.GetClient();

                AssertClientHasHost(client1, testReadWriteHosts[0]);
                AssertClientHasHost(client2, testReadWriteHosts[1]);
                AssertClientHasHost(client3, testReadWriteHosts[2]);
                AssertClientHasHost(client4, testReadWriteHosts[3]);
                AssertClientHasHost(client5, testReadWriteHosts[0]);
            }
        }

        [Test]
        public void Does_loop_through_ReadOnly_hosts()
        {
            using (var manager = CreateAndStartManager())
            {
                var client1 = manager.GetReadOnlyClient();
                client1.Dispose();
                var client2 = manager.GetReadOnlyClient();
                client2.Dispose();
                var client3 = manager.GetReadOnlyClient();
                var client4 = manager.GetReadOnlyClient();
                var client5 = manager.GetReadOnlyClient();

                AssertClientHasHost(client1, testReadOnlyHosts[0]);
                AssertClientHasHost(client2, testReadOnlyHosts[1]);
                AssertClientHasHost(client3, testReadOnlyHosts[2]);
                AssertClientHasHost(client4, testReadOnlyHosts[0]);
                AssertClientHasHost(client5, testReadOnlyHosts[1]);
            }
        }

        [Test]
        public void Can_have_different_pool_size_and_host_configurations()
        {
            var writeHosts = new[] { "readwrite1" };
            var readHosts = new[] { "read1", "read2" };

            const int poolSizeMultiplier = 4;

            using (var manager = new PooledRedisClientManager(writeHosts, readHosts,
                    new RedisClientManagerConfig
                    {
                        MaxWritePoolSize = writeHosts.Length * poolSizeMultiplier,
                        MaxReadPoolSize = readHosts.Length * poolSizeMultiplier,
                        AutoStart = true,
                    }
                )
            )
            {
                //A poolsize of 4 will not block getting 4 clients
                using (var client1 = manager.GetClient())
                using (var client2 = manager.GetClient())
                using (var client3 = manager.GetClient())
                using (var client4 = manager.GetClient())
                {
                    AssertClientHasHost(client1, writeHosts[0]);
                    AssertClientHasHost(client2, writeHosts[0]);
                    AssertClientHasHost(client3, writeHosts[0]);
                    AssertClientHasHost(client4, writeHosts[0]);
                }

                //A poolsize of 8 will not block getting 8 clients
                using (var client1 = manager.GetReadOnlyClient())
                using (var client2 = manager.GetReadOnlyClient())
                using (var client3 = manager.GetReadOnlyClient())
                using (var client4 = manager.GetReadOnlyClient())
                using (var client5 = manager.GetReadOnlyClient())
                using (var client6 = manager.GetReadOnlyClient())
                using (var client7 = manager.GetReadOnlyClient())
                using (var client8 = manager.GetReadOnlyClient())
                {
                    AssertClientHasHost(client1, readHosts[0]);
                    AssertClientHasHost(client2, readHosts[1]);
                    AssertClientHasHost(client3, readHosts[0]);
                    AssertClientHasHost(client4, readHosts[1]);
                    AssertClientHasHost(client5, readHosts[0]);
                    AssertClientHasHost(client6, readHosts[1]);
                    AssertClientHasHost(client7, readHosts[0]);
                    AssertClientHasHost(client8, readHosts[1]);
                }
            }
        }

        [Test]
        public void Does_block_ReadWrite_clients_pool()
        {
            using (var manager = CreateAndStartManager())
            {
                var delay = TimeSpan.FromSeconds(1);
                var client1 = manager.GetClient();
                var client2 = manager.GetClient();
                var client3 = manager.GetClient();
                var client4 = manager.GetClient();

                Action func = delegate
                {
                    Thread.Sleep(delay + TimeSpan.FromSeconds(0.5));
                    client4.Dispose();
                };

#if NETCORE                
                Task.Run(func);
#else
                func.BeginInvoke(null, null);
#endif

                var start = DateTime.Now;

                var client5 = manager.GetClient();

                Assert.That(DateTime.Now - start, Is.GreaterThanOrEqualTo(delay));

                AssertClientHasHost(client1, testReadWriteHosts[0]);
                AssertClientHasHost(client2, testReadWriteHosts[1]);
                AssertClientHasHost(client3, testReadWriteHosts[2]);
                AssertClientHasHost(client4, testReadWriteHosts[3]);
                AssertClientHasHost(client5, testReadWriteHosts[3]);
            }
        }

        [Test]
        public void Does_block_ReadOnly_clients_pool()
        {
            var delay = TimeSpan.FromSeconds(1);

            using (var manager = CreateAndStartManager())
            {
                var client1 = manager.GetReadOnlyClient();
                var client2 = manager.GetReadOnlyClient();
                var client3 = manager.GetReadOnlyClient();

                Action func = delegate
                {
                    Thread.Sleep(delay + TimeSpan.FromSeconds(0.5));
                    client3.Dispose();
                };
#if NETCORE
                Task.Run(func);
#else
                func.BeginInvoke(null, null);
#endif
                var start = DateTime.Now;

                var client4 = manager.GetReadOnlyClient();

                Assert.That(DateTime.Now - start, Is.GreaterThanOrEqualTo(delay));

                AssertClientHasHost(client1, testReadOnlyHosts[0]);
                AssertClientHasHost(client2, testReadOnlyHosts[1]);
                AssertClientHasHost(client3, testReadOnlyHosts[2]);
                AssertClientHasHost(client4, testReadOnlyHosts[2]);
            }
        }

        [Test]
        public void Does_throw_TimeoutException_when_PoolTimeout_exceeded()
        {
            using (var manager = new PooledRedisClientManager(testReadWriteHosts, testReadOnlyHosts,
                new RedisClientManagerConfig
                {
                    MaxWritePoolSize = 4,
                    MaxReadPoolSize = 4,
                    AutoStart = false,
                }))
            {
                manager.PoolTimeout = 100;

                manager.Start();

                var masters = 4.Times(i => manager.GetClient());

                try
                {
                    manager.GetClient();
                    Assert.Fail("Should throw TimeoutException");
                }
                catch (TimeoutException ex)
                {
                    Assert.That(ex.Message, Does.StartWith("Redis Timeout expired."));
                }

                var replicas = 4.Times(i => manager.GetReadOnlyClient());

                try
                {
                    manager.GetReadOnlyClient();
                    Assert.Fail("Should throw TimeoutException");
                }
                catch (TimeoutException ex)
                {
                    Assert.That(ex.Message, Does.StartWith("Redis Timeout expired."));
                }
            }
        }

        //[Ignore("tempromental integration test")]
        //[Test]
        //public void Can_support_64_threads_using_the_client_simultaneously()
        //{
        //    const int noOfConcurrentClients = 64; //WaitHandle.WaitAll limit is <= 64
        //    var clientUsageMap = new Dictionary<string, int>();

        //    var clientAsyncResults = new List<IAsyncResult>();
        //    using (var manager = CreateAndStartManager())
        //    {
        //        for (var i = 0; i < noOfConcurrentClients; i++)
        //        {
        //            var clientNo = i;
        //            var action = (Action)(() => UseClient(manager, clientNo, clientUsageMap));
        //            clientAsyncResults.Add(action.BeginInvoke(null, null));
        //        }
        //    }

        //    WaitHandle.WaitAll(clientAsyncResults.ConvertAll(x => x.AsyncWaitHandle).ToArray());

        //    RedisStats.ToDictionary().PrintDump();

        //    Debug.WriteLine(TypeSerializer.SerializeToString(clientUsageMap));

        //    var hostCount = 0;
        //    foreach (var entry in clientUsageMap)
        //    {
        //        Assert.That(entry.Value, Is.GreaterThanOrEqualTo(2), "Host has unproportionate distribution: " + entry.Value);
        //        Assert.That(entry.Value, Is.LessThanOrEqualTo(30), "Host has unproportionate distribution: " + entry.Value);
        //        hostCount += entry.Value;
        //    }

        //    Assert.That(hostCount, Is.EqualTo(noOfConcurrentClients), "Invalid no of clients used");
        //}

        private static void UseClient(IRedisClientsManager manager, int clientNo, Dictionary<string, int> hostCountMap)
        {
            using (var client = manager.GetClient())
            {
                lock (hostCountMap)
                {
                    int hostCount;
                    if (!hostCountMap.TryGetValue(client.Host, out hostCount))
                    {
                        hostCount = 0;
                    }

                    hostCountMap[client.Host] = ++hostCount;
                }

                Debug.WriteLine(String.Format("Client '{0}' is using '{1}'", clientNo, client.Host));
            }
        }

    }
}