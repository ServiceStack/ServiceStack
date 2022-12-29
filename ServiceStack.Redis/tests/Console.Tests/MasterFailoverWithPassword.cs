using System;
using System.Threading;
using ServiceStack;
using ServiceStack.Redis;

namespace ConsoleTests
{
    public class MasterFailoverWithPassword
    {
        public void Execute()
        {
            string AddPassword(string host) => $"password@{host}";

            var sentinelHosts = new[] { "127.0.0.1:26380", "127.0.0.1:26381", "127.0.0.1:26382" };
            var sentinel = new RedisSentinel(sentinelHosts.Map(AddPassword), masterName: "mymaster") {
                HostFilter = AddPassword,
                SentinelHostFilter = AddPassword,
            };
            var manager = sentinel.Start();

            sentinel.OnWorkerError = Console.WriteLine;

            while (true)
            {
                try
                {
                    const string RedisKey = "my Name";
                    using var client = manager.GetClient();
                    var result = client.Get<string>(RedisKey);
                    Console.WriteLine("Redis Key: {0} \t Port: {1}", result, client.Port);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error {0}".Fmt(ex.Message));
                }
                Thread.Sleep(3000);
            }
        } 
    }
}