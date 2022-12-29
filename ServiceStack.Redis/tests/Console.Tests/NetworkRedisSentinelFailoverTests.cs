using System.Collections.Generic;
using ServiceStack.Redis;

namespace ConsoleTests
{
    public class NetworkRedisSentinelFailoverTests : RedisSentinelFailoverTests
    {
        public static string[] SentinelHosts = new[]
        {
            "10.0.0.9:26380",
            "10.0.0.9:26381",
            "10.0.0.9:26382",
        };

        protected override RedisSentinel CreateSentinel()
        {
            var sentinel = new RedisSentinel(SentinelHosts)
            {
                IpAddressMap =
                {
                    {"127.0.0.1", "10.0.0.9"},
                }
            };
            return sentinel;
        }
    }
}