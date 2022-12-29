using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ConsoleTests
{
    public class BlockingRemoveAfterReconnection
    {
        protected internal static RedisManagerPool BasicRedisClientManager;

        public void Execute()
        {
            //RedisConfig.AssumeServerVersion = 4000;
            RedisConfig.DefaultConnectTimeout = 20 * 1000;
            RedisConfig.DefaultRetryTimeout = 20 * 1000;
            BasicRedisClientManager = new RedisManagerPool();
            try
            {
                using (var client = BasicRedisClientManager.GetClient())
                {
                    Console.WriteLine("Blocking...");
                    var fromList = client.BlockingRemoveStartFromList("AnyQueue", TimeSpan.FromMinutes(20));
                    Console.WriteLine($"Received: {fromList.Dump()}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}