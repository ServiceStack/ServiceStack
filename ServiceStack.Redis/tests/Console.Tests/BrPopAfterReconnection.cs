using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ConsoleTests
{
    public class BrPopAfterReconnection
    {
        protected internal static BasicRedisClientManager BasicRedisClientManager;
        
        public void Execute()
        {
//            RedisConfig.AssumeServerVersion = 4000;
//            RedisConfig.DisableVerboseLogging = false;
//            LogManager.LogFactory = new ConsoleLogFactory();
            
            var host = "localhost";
            var port = "6379";
            var db = "9";

            var redisUri = $"{host}:{port}?db={db}";

            BasicRedisClientManager = new BasicRedisClientManager(redisUri);
            var queue = "FormSaved";

            while (true)
            {
                Task.Run(() => BlockingReceive(queue));
                Thread.Sleep(1000);

                Console.WriteLine("Restart Redis and press Enter...");
                Console.ReadLine();

                Console.WriteLine("Enter something:");
                var item = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(item))
                {
                    using (var client = BasicRedisClientManager.GetClient())
                    {
                        client.AddItemToList(queue, item);
                    }

                    Console.WriteLine("Item added");
                }
        
                Thread.Sleep(1000);
            }
        }
        
        public static void BlockingReceive(string queue)
        {
            using (var client = BasicRedisClientManager.GetReadOnlyClient())
            {
                Console.WriteLine($"Listening to {queue}");

                var fromList = client.BlockingPopItemFromList(queue, TimeSpan.FromSeconds(60));
           
                Console.WriteLine($"Received:{fromList.Dump()}");
            }
        }
    }
}