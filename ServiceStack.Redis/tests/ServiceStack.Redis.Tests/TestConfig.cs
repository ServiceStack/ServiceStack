using System;
using ServiceStack.Logging;
using ServiceStack.Support;

namespace ServiceStack.Redis.Tests
{
    public static class TestConfig
    {
        static TestConfig()
        {
            LogManager.LogFactory = new InMemoryLogFactory();
        }

        public static bool IgnoreLongTests = true;

        public static string SingleHost => Environment.GetEnvironmentVariable("CI_REDIS") ?? "localhost";

        public static string GeoHost => Environment.GetEnvironmentVariable("CI_REDIS") ?? "10.0.0.121";

        public static readonly string[] MasterHosts = new[] { "localhost" };
        public static readonly string[] ReplicaHosts = new[] { "localhost" };

        public const int RedisPort = 6379;

        public static string SingleHostConnectionString
        {
            get
            {
                return SingleHost + ":" + RedisPort;
            }
        }

        public static BasicRedisClientManager BasicClientManger
        {
            get
            {
                return new BasicRedisClientManager(new[] {
                    SingleHostConnectionString
                });
            }
        }
    }
}