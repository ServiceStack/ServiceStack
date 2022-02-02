using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests.Integration
{
    [TestFixture]
    public class MultiThreadedPoolIntegrationTests
         : IntegrationTestBase
    {
        static Dictionary<string, int> hostCountMap;

        public IRedisClientsManager CreateAndStartManager(
            string[] readWriteHosts, string[] readOnlyHosts)
        {
            return new PooledRedisClientManager(readWriteHosts, readOnlyHosts,
                new RedisClientManagerConfig
                {
                    MaxWritePoolSize = readWriteHosts.Length,
                    MaxReadPoolSize = readOnlyHosts.Length,
                    AutoStart = true,
                });
        }

        [SetUp]
        public void BeforeEachTest()
        {
            hostCountMap = new Dictionary<string, int>();
        }

        [TearDown]
        public void AfterEachTest()
        {
            CheckHostCountMap(hostCountMap);
        }

        [Test]
        public void Pool_can_support_64_threads_using_the_client_simultaneously()
        {
            RunSimultaneously(CreateAndStartManager, UseClient);
        }

        [Test]
        public void Basic_can_support_64_threads_using_the_client_simultaneously()
        {
            RunSimultaneously(CreateAndStartBasicManager, UseClient);
        }

        [Test]
        public void ManagerPool_can_support_64_threads_using_the_client_simultaneously()
        {
            RunSimultaneously(CreateAndStartManagerPool, UseClient);
        }

        private static void UseClient(IRedisClientsManager manager, int clientNo)
        {
            using (var client = manager.GetReadOnlyClient())
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

                Log("Client '{0}' is using '{1}'", clientNo, client.Host);
            }
        }

    }
}