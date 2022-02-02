using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests.Sentinel
{
    [TestFixture]
    public class RedisResolverTests
        : RedisSentinelTestBase
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            StartAllRedisServers();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ShutdownAllRedisServers();
        }

        [Test]
        public void RedisResolver_does_reset_when_detects_invalid_master()
        {
            var invalidMaster = new[] { ReplicaHosts[0] };
            var invalidReplicas = new[] { MasterHosts[0], ReplicaHosts[1] };

            using (var redisManager = new PooledRedisClientManager(invalidMaster, invalidReplicas))
            {
                var resolver = (RedisResolver)redisManager.RedisResolver;

                using (var master = redisManager.GetClient())
                {
                    master.SetValue("KEY", "1");
                    Assert.That(master.GetHostString(), Is.EqualTo(MasterHosts[0]));
                }
                using (var master = redisManager.GetClient())
                {
                    master.Increment("KEY", 1);
                    Assert.That(master.GetHostString(), Is.EqualTo(MasterHosts[0]));
                }

                "Masters:".Print();
                resolver.Masters.PrintDump();
                "Replicas:".Print();
                resolver.Slaves.PrintDump();
            }
        }

        [Test]
        public void PooledRedisClientManager_alternates_hosts()
        {
            using var redisManager = new PooledRedisClientManager(MasterHosts, ReplicaHosts);
            using (var master = redisManager.GetClient())
            {
                Assert.That(master.GetHostString(), Is.EqualTo(MasterHosts[0]));
                master.SetValue("KEY", "1");
            }
            using (var master = redisManager.GetClient())
            {
                Assert.That(master.GetHostString(), Is.EqualTo(MasterHosts[0]));
                master.Increment("KEY", 1);
            }

            5.Times(i => {
                using var readOnly = redisManager.GetReadOnlyClient();
                Assert.That(readOnly.GetHostString(), Is.EqualTo(ReplicaHosts[i % ReplicaHosts.Length]));
                Assert.That(readOnly.GetValue("KEY"), Is.EqualTo("2"));
            });

            using (var cache = redisManager.GetCacheClient())
            {
                Assert.That(cache.Get<string>("KEY"), Is.EqualTo("2"));
            }
        }

        [Test]
        public void RedisManagerPool_alternates_hosts()
        {
            using var redisManager = new RedisManagerPool(MasterHosts);
            using (var master = redisManager.GetClient())
            {
                Assert.That(master.GetHostString(), Is.EqualTo(MasterHosts[0]));
                master.SetValue("KEY", "1");
            }
            using (var master = redisManager.GetClient())
            {
                Assert.That(master.GetHostString(), Is.EqualTo(MasterHosts[0]));
                master.Increment("KEY", 1);
            }

            5.Times(i => {
                using var readOnly = redisManager.GetReadOnlyClient();
                Assert.That(readOnly.GetHostString(), Is.EqualTo(MasterHosts[0]));
                Assert.That(readOnly.GetValue("KEY"), Is.EqualTo("2"));
            });

            using (var cache = redisManager.GetCacheClient())
            {
                Assert.That(cache.Get<string>("KEY"), Is.EqualTo("2"));
            }
        }

        [Test]
        public void BasicRedisClientManager_alternates_hosts()
        {
            using (var redisManager = new BasicRedisClientManager(MasterHosts, ReplicaHosts))
            {
                using (var master = redisManager.GetClient())
                {
                    Assert.That(master.GetHostString(), Is.EqualTo(MasterHosts[0]));
                    master.SetValue("KEY", "1");
                }
                using (var master = redisManager.GetClient())
                {
                    Assert.That(master.GetHostString(), Is.EqualTo(MasterHosts[0]));
                    master.Increment("KEY", 1);
                }

                5.Times(i => {
                    using var readOnly = redisManager.GetReadOnlyClient();
                    Assert.That(readOnly.GetHostString(), Is.EqualTo(ReplicaHosts[i % ReplicaHosts.Length]));
                    Assert.That(readOnly.GetValue("KEY"), Is.EqualTo("2"));
                });

                using (var cache = redisManager.GetCacheClient())
                {
                    Assert.That(cache.Get<string>("KEY"), Is.EqualTo("2"));
                }
            }
        }

        public class FixedResolver : IRedisResolver
        {
            private readonly RedisEndpoint master;
            private readonly RedisEndpoint replica;
            public int NewClientsInitialized = 0;

            public FixedResolver(RedisEndpoint master, RedisEndpoint replica)
            {
                this.master = master;
                this.replica = replica;
                this.ClientFactory = RedisConfig.ClientFactory;
            }

            public Func<RedisEndpoint, RedisClient> ClientFactory { get; set; }

            public int ReadWriteHostsCount => 1;
            public int ReadOnlyHostsCount => 1;

            public void ResetMasters(IEnumerable<string> hosts) { }
            public void ResetSlaves(IEnumerable<string> hosts) { }

            public RedisClient CreateRedisClient(RedisEndpoint config, bool master)
            {
                NewClientsInitialized++;
                return ClientFactory(config);
            }

            public RedisClient CreateMasterClient(int desiredIndex)
            {
                return CreateRedisClient(master, master: true);
            }

            public RedisClient CreateSlaveClient(int desiredIndex)
            {
                return CreateRedisClient(replica, master: false);
            }
        }

        [Test]
        public void PooledRedisClientManager_can_execute_CustomResolver()
        {
            var resolver = new FixedResolver(MasterHosts[0].ToRedisEndpoint(), ReplicaHosts[0].ToRedisEndpoint());
            using var redisManager = new PooledRedisClientManager("127.0.0.1:8888")
            {
                RedisResolver = resolver
            };
            using (var master = redisManager.GetClient())
            {
                Assert.That(master.GetHostString(), Is.EqualTo(MasterHosts[0]));
                master.SetValue("KEY", "1");
            }
            using (var master = redisManager.GetClient())
            {
                Assert.That(master.GetHostString(), Is.EqualTo(MasterHosts[0]));
                master.Increment("KEY", 1);
            }
            Assert.That(resolver.NewClientsInitialized, Is.EqualTo(1));

            5.Times(i =>
            {
                using (var replica = redisManager.GetReadOnlyClient())
                {
                    Assert.That(replica.GetHostString(), Is.EqualTo(ReplicaHosts[0]));
                    Assert.That(replica.GetValue("KEY"), Is.EqualTo("2"));
                }
            });
            Assert.That(resolver.NewClientsInitialized, Is.EqualTo(2));

            redisManager.FailoverTo("127.0.0.1:9999", "127.0.0.1:9999");

            5.Times(i =>
            {
                using (var master = redisManager.GetClient())
                {
                    Assert.That(master.GetHostString(), Is.EqualTo(MasterHosts[0]));
                    Assert.That(master.GetValue("KEY"), Is.EqualTo("2"));
                }
                using (var replica = redisManager.GetReadOnlyClient())
                {
                    Assert.That(replica.GetHostString(), Is.EqualTo(ReplicaHosts[0]));
                    Assert.That(replica.GetValue("KEY"), Is.EqualTo("2"));
                }
            });
            Assert.That(resolver.NewClientsInitialized, Is.EqualTo(4));
        }

        [Test]
        public void RedisManagerPool_can_execute_CustomResolver()
        {
            var resolver = new FixedResolver(MasterHosts[0].ToRedisEndpoint(), ReplicaHosts[0].ToRedisEndpoint());
            using var redisManager = new RedisManagerPool("127.0.0.1:8888")
            {
                RedisResolver = resolver
            };
            using (var master = redisManager.GetClient())
            {
                Assert.That(master.GetHostString(), Is.EqualTo(MasterHosts[0]));
                master.SetValue("KEY", "1");
            }
            using (var master = redisManager.GetClient())
            {
                Assert.That(master.GetHostString(), Is.EqualTo(MasterHosts[0]));
                master.Increment("KEY", 1);
            }
            Assert.That(resolver.NewClientsInitialized, Is.EqualTo(1));

            5.Times(i => {
                using var replica = redisManager.GetReadOnlyClient();
                Assert.That(replica.GetHostString(), Is.EqualTo(MasterHosts[0]));
                Assert.That(replica.GetValue("KEY"), Is.EqualTo("2"));
            });
            Assert.That(resolver.NewClientsInitialized, Is.EqualTo(1));

            redisManager.FailoverTo("127.0.0.1:9999", "127.0.0.1:9999");

            5.Times(i =>
            {
                using (var master = redisManager.GetClient())
                {
                    Assert.That(master.GetHostString(), Is.EqualTo(MasterHosts[0]));
                    Assert.That(master.GetValue("KEY"), Is.EqualTo("2"));
                }
                using (var replica = redisManager.GetReadOnlyClient())
                {
                    Assert.That(replica.GetHostString(), Is.EqualTo(MasterHosts[0]));
                    Assert.That(replica.GetValue("KEY"), Is.EqualTo("2"));
                }
            });
            Assert.That(resolver.NewClientsInitialized, Is.EqualTo(2));
        }

        private static void InitializeEmptyRedisManagers(IRedisClientsManager redisManager, string[] masters, string[] replicas)
        {
            var hasResolver = (IHasRedisResolver)redisManager;
            hasResolver.RedisResolver.ResetMasters(masters);
            hasResolver.RedisResolver.ResetSlaves(replicas);

            using (var master = redisManager.GetClient())
            {
                Assert.That(master.GetHostString(), Is.EqualTo(masters[0]));
                master.SetValue("KEY", "1");
            }
            using (var replica = redisManager.GetReadOnlyClient())
            {
                Assert.That(replica.GetHostString(), Is.EqualTo(replicas[0]));
                Assert.That(replica.GetValue("KEY"), Is.EqualTo("1"));
            }
        }

        [Test]
        public void Can_initialize_ClientManagers_with_no_hosts()
        {
            InitializeEmptyRedisManagers(new PooledRedisClientManager(), MasterHosts, ReplicaHosts);
            InitializeEmptyRedisManagers(new RedisManagerPool(), MasterHosts, MasterHosts);
            InitializeEmptyRedisManagers(new BasicRedisClientManager(), MasterHosts, ReplicaHosts);
        }
    }
}