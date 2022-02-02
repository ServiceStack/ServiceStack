using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Category("Integration"), Category("Async")]
    public class PooledRedisClientManagerTestsAsync
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

        public IRedisClientsManagerAsync CreateManager(string[] readWriteHosts, string[] readOnlyHosts, int? defaultDb = null)
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
        public IRedisClientsManagerAsync CreateManager(params string[] readWriteHosts)
        {
            return CreateManager(readWriteHosts, readWriteHosts);
        }

        public IRedisClientsManagerAsync CreateManager()
        {
            return CreateManager(testReadWriteHosts, testReadOnlyHosts);
        }

        public IRedisClientsManagerAsync CreateAndStartManager()
        {
            var manager = CreateManager();
            ((PooledRedisClientManager)manager).Start();
            return manager;
        }

        [Test]
        public async Task Cant_get_client_without_calling_Start()
        {
            await using var manager = CreateManager();
            try
            {
                var client = await manager.GetClientAsync();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            Assert.Fail("Should throw");
        }

        [Test]
        public async Task Can_change_db_for_client_PooledRedisClientManager()
        {
            await using IRedisClientsManagerAsync db1 = new PooledRedisClientManager(1, new string[] { TestConfig.SingleHost });
            await using IRedisClientsManagerAsync db2 = new PooledRedisClientManager(2, new string[] { TestConfig.SingleHost });
            var val = Environment.TickCount;
            var key = "test" + val;
            var db1c = await db1.GetClientAsync();
            var db2c = await db2.GetClientAsync();
            try
            {
                await db1c.SetAsync(key, val);
                Assert.That(await db2c.GetAsync<int>(key), Is.EqualTo(0));
                Assert.That(await db1c.GetAsync<int>(key), Is.EqualTo(val));
            }
            finally
            {
                await db1c.RemoveAsync(key);
            }
        }

        [Test]
        public async Task Can_change_db_for_client_RedisManagerPool()
        {
            await using IRedisClientsManagerAsync db1 = new RedisManagerPool(TestConfig.SingleHost + "?db=1");
            await using IRedisClientsManagerAsync db2 = new RedisManagerPool(TestConfig.SingleHost + "?db=2");
            var val = Environment.TickCount;
            var key = "test" + val;
            var db1c = await db1.GetClientAsync();
            var db2c = await db2.GetClientAsync();
            try
            {
                await db1c.SetAsync(key, val);
                Assert.That(await db2c.GetAsync<int>(key), Is.EqualTo(0));
                Assert.That(await db1c.GetAsync<int>(key), Is.EqualTo(val));
            }
            finally
            {
                await db1c.RemoveAsync(key);
            }
        }

        [Test]
        public async Task Can_change_db_for_client_BasicRedisClientManager()
        {
            await using IRedisClientsManagerAsync db1 = new BasicRedisClientManager(1, new string[] { TestConfig.SingleHost });
            await using IRedisClientsManagerAsync db2 = new BasicRedisClientManager(2, new string[] { TestConfig.SingleHost });
            var val = Environment.TickCount;
            var key = "test" + val;
            var db1c = await db1.GetClientAsync();
            var db2c = await db2.GetClientAsync();
            try
            {
                await db1c.SetAsync(key, val);
                Assert.That(await db2c.GetAsync<int>(key), Is.EqualTo(0));
                Assert.That(await db1c.GetAsync<int>(key), Is.EqualTo(val));
            }
            finally
            {
                await db1c.RemoveAsync(key);
            }
        }

        [Test]
        public async Task Can_get_client_after_calling_Start()
        {
            await using var manager = CreateAndStartManager();
            var client = await manager.GetClientAsync();
        }

        [Test]
        public async Task Can_get_ReadWrite_client()
        {
            await using var manager = CreateAndStartManager();
            var client = await manager.GetClientAsync();

            AssertClientHasHost(client, firstReadWriteHost);
        }

        private static void AssertClientHasHost(IRedisClientAsync client, string hostWithOptionalPort)
        {
            var parts = hostWithOptionalPort.Split(':');
            var port = parts.Length > 1 ? int.Parse(parts[1]) : RedisConfig.DefaultPort;

            Assert.That(client.Host, Is.EqualTo(parts[0]));
            Assert.That(client.Port, Is.EqualTo(port));
        }

        [Test]
        public async Task Can_get_ReadOnly_client()
        {
            await using var manager = CreateAndStartManager();
            var client = await manager.GetReadOnlyClientAsync();

            AssertClientHasHost(client, firstReadOnlyHost);
        }

        [Test]
        public async Task Does_loop_through_ReadWrite_hosts()
        {
            await using var manager = CreateAndStartManager();
            var client1 = await manager.GetClientAsync();
            await client1.DisposeAsync();
            var client2 = await manager.GetClientAsync();
            var client3 = await manager.GetClientAsync();
            var client4 = await manager.GetClientAsync();
            var client5 = await manager.GetClientAsync();

            AssertClientHasHost(client1, testReadWriteHosts[0]);
            AssertClientHasHost(client2, testReadWriteHosts[1]);
            AssertClientHasHost(client3, testReadWriteHosts[2]);
            AssertClientHasHost(client4, testReadWriteHosts[3]);
            AssertClientHasHost(client5, testReadWriteHosts[0]);
        }

        [Test]
        public async Task Does_loop_through_ReadOnly_hosts()
        {
            await using var manager = CreateAndStartManager();
            var client1 = await manager.GetReadOnlyClientAsync();
            await client1.DisposeAsync();
            var client2 = await manager.GetReadOnlyClientAsync();
            await client2.DisposeAsync();
            var client3 = await manager.GetReadOnlyClientAsync();
            var client4 = await manager.GetReadOnlyClientAsync();
            var client5 = await manager.GetReadOnlyClientAsync();

            AssertClientHasHost(client1, testReadOnlyHosts[0]);
            AssertClientHasHost(client2, testReadOnlyHosts[1]);
            AssertClientHasHost(client3, testReadOnlyHosts[2]);
            AssertClientHasHost(client4, testReadOnlyHosts[0]);
            AssertClientHasHost(client5, testReadOnlyHosts[1]);
        }

        [Test]
        public async Task Can_have_different_pool_size_and_host_configurations()
        {
            var writeHosts = new[] { "readwrite1" };
            var readHosts = new[] { "read1", "read2" };

            const int poolSizeMultiplier = 4;

            await using IRedisClientsManagerAsync manager = new PooledRedisClientManager(writeHosts, readHosts,
                    new RedisClientManagerConfig
                    {
                        MaxWritePoolSize = writeHosts.Length * poolSizeMultiplier,
                        MaxReadPoolSize = readHosts.Length * poolSizeMultiplier,
                        AutoStart = true,
                    }
                );
            //A poolsize of 4 will not block getting 4 clients
            await using (var client1 = await manager.GetClientAsync())
            await using (var client2 = await manager.GetClientAsync())
            await using (var client3 = await manager.GetClientAsync())
            await using (var client4 = await manager.GetClientAsync())
            {
                AssertClientHasHost(client1, writeHosts[0]);
                AssertClientHasHost(client2, writeHosts[0]);
                AssertClientHasHost(client3, writeHosts[0]);
                AssertClientHasHost(client4, writeHosts[0]);
            }

            //A poolsize of 8 will not block getting 8 clients
            await using (var client1 = await manager.GetReadOnlyClientAsync())
            await using (var client2 = await manager.GetReadOnlyClientAsync())
            await using (var client3 = await manager.GetReadOnlyClientAsync())
            await using (var client4 = await manager.GetReadOnlyClientAsync())
            await using (var client5 = await manager.GetReadOnlyClientAsync())
            await using (var client6 = await manager.GetReadOnlyClientAsync())
            await using (var client7 = await manager.GetReadOnlyClientAsync())
            await using (var client8 = await manager.GetReadOnlyClientAsync())
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

        [Test]
        public async Task Does_block_ReadWrite_clients_pool()
        {
            await using IRedisClientsManagerAsync manager = CreateAndStartManager();
            var delay = TimeSpan.FromSeconds(1);
            var client1 = await manager.GetClientAsync();
            var client2 = await manager.GetClientAsync();
            var client3 = await manager.GetClientAsync();
            var client4 = await manager.GetClientAsync();

#pragma warning disable IDE0039 // Use local function
            Action func = async delegate
#pragma warning restore IDE0039 // Use local function
            {
                await Task.Delay(delay + TimeSpan.FromSeconds(0.5));
                await client4.DisposeAsync();
            };

#if NETCORE
            _ = Task.Run(func);
#else
            func.BeginInvoke(null, null);
#endif

            var start = DateTime.Now;

            var client5 = await manager.GetClientAsync();

            Assert.That(DateTime.Now - start, Is.GreaterThanOrEqualTo(delay));

            AssertClientHasHost(client1, testReadWriteHosts[0]);
            AssertClientHasHost(client2, testReadWriteHosts[1]);
            AssertClientHasHost(client3, testReadWriteHosts[2]);
            AssertClientHasHost(client4, testReadWriteHosts[3]);
            AssertClientHasHost(client5, testReadWriteHosts[3]);
        }

        [Test]
        public async Task Does_block_ReadOnly_clients_pool()
        {
            var delay = TimeSpan.FromSeconds(1);

            await using var manager = CreateAndStartManager();
            var client1 = await manager.GetReadOnlyClientAsync();
            var client2 = await manager.GetReadOnlyClientAsync();
            var client3 = await manager.GetReadOnlyClientAsync();

#pragma warning disable IDE0039 // Use local function
            Action func = async delegate
#pragma warning restore IDE0039 // Use local function
            {
                await Task.Delay(delay + TimeSpan.FromSeconds(0.5));
                await client3.DisposeAsync();
            };
#if NETCORE
            _ =Task.Run(func);
#else
            func.BeginInvoke(null, null);
#endif
            var start = DateTime.Now;

            var client4 = await manager.GetReadOnlyClientAsync();

            Assert.That(DateTime.Now - start, Is.GreaterThanOrEqualTo(delay));

            AssertClientHasHost(client1, testReadOnlyHosts[0]);
            AssertClientHasHost(client2, testReadOnlyHosts[1]);
            AssertClientHasHost(client3, testReadOnlyHosts[2]);
            AssertClientHasHost(client4, testReadOnlyHosts[2]);
        }

        [Test]
        public async Task Does_throw_TimeoutException_when_PoolTimeout_exceeded()
        {
            await using IRedisClientsManagerAsync manager = new PooledRedisClientManager(testReadWriteHosts, testReadOnlyHosts,
                new RedisClientManagerConfig
                {
                    MaxWritePoolSize = 4,
                    MaxReadPoolSize = 4,
                    AutoStart = false,
                });
            ((PooledRedisClientManager)manager).PoolTimeout = 100;

            ((PooledRedisClientManager)manager).Start();

            var masters = 4.Times(i => manager.GetClientAsync());

            try
            {
                await manager.GetClientAsync();
                Assert.Fail("Should throw TimeoutException");
            }
            catch (TimeoutException ex)
            {
                Assert.That(ex.Message, Does.StartWith("Redis Timeout expired."));
            }

            for (int i = 0; i < 4; i++)
            {
                await manager.GetReadOnlyClientAsync();
            }

            try
            {
                await manager.GetReadOnlyClientAsync();
                Assert.Fail("Should throw TimeoutException");
            }
            catch (TimeoutException ex)
            {
                Assert.That(ex.Message, Does.StartWith("Redis Timeout expired."));
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

        //private static void UseClient(IRedisClientsManager manager, int clientNo, Dictionary<string, int> hostCountMap)
        //{
        //    using (var client = manager.GetClient())
        //    {
        //        lock (hostCountMap)
        //        {
        //            int hostCount;
        //            if (!hostCountMap.TryGetValue(client.Host, out hostCount))
        //            {
        //                hostCount = 0;
        //            }

        //            hostCountMap[client.Host] = ++hostCount;
        //        }

        //        Debug.WriteLine(String.Format("Client '{0}' is using '{1}'", clientNo, client.Host));
        //    }
        //}

    }
}