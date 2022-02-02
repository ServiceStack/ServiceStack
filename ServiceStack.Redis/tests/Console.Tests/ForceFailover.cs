using System;
using ServiceStack.Logging;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ConsoleTests
{
    public class ForceFailover
    {
        public void Execute()
        {
            RedisConfig.EnableVerboseLogging = false;
            LogManager.LogFactory = new ConsoleLogFactory(debugEnabled:true);

            var sentinel = new RedisSentinel(new [] {
                    "127.0.0.1:26380",
                    "127.0.0.1:26381",
                    "127.0.0.1:26382",
                }, "mymaster");

            var redisManager = sentinel.Start();

            using (var client = redisManager.GetClient())
            {
                client.FlushAll();
            }

            using (var client = redisManager.GetClient())
            {
                client.IncrementValue("counter").ToString().Print();
            }

            "Force 'SENTINEL failover mymaster' then press enter...".Print();
            Console.ReadLine();

            try
            {
                using (var client = redisManager.GetClient())
                {
                    client.IncrementValue("counter").ToString().Print();
                }
            }
            catch (Exception ex)
            {
                ex.Message.Print();
            }

            try
            {
                using (var client = redisManager.GetClient())
                {
                    client.IncrementValue("counter").ToString().Print();
                }
            }
            catch (Exception ex)
            {
                ex.Message.Print();
            }

            try
            {
                using (var client = redisManager.GetClient())
                {
                    client.IncrementValue("counter").ToString().Print();
                }
            }
            catch (Exception ex)
            {
                ex.Message.Print();
            }

            Console.ReadLine();
        }
    }
}