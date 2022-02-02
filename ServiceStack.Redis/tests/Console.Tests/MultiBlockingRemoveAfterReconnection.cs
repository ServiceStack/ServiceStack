using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ConsoleTests
{
    public class MultiBlockingRemoveAfterReconnection
    {
        protected internal static RedisManagerPool RedisManager;
        
        public void Execute()
        {
//            LogManager.LogFactory = new ConsoleLogFactory();
//            RedisConfig.EnableVerboseLogging = true;
            
            RedisConfig.DefaultConnectTimeout = 20 * 1000;
            RedisConfig.DefaultRetryTimeout = 20 * 1000;

            RedisManager = new RedisManagerPool($"localhost:6379?db=9");
            
            MultipleBlocking(3);

            Console.ReadLine();
        }
        
        private static void MultipleBlocking(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var queue = $"Q{i + 1}";
                RunTask(() => BlockingRemoveStartFromList(queue), $"Receive from {queue}");
            }
        }
        public static void BlockingRemoveStartFromList(string queue)
        {
            using (var client = RedisManager.GetClient() as RedisClient)
            {
                client.Ping();
                Console.WriteLine($"#{client.ClientId} Listening to {queue}");

                var fromList = client.BlockingRemoveStartFromList(queue, TimeSpan.FromHours(10));
                Console.WriteLine($"#{client.ClientId} Received: '{fromList.Dump()}' from '{queue}'");
            }
        }

        private static void RunTask(Action action, string name)
        {
            Task.Run(() =>
            {

                while (true)
                {
                    try
                    {
                        Console.WriteLine($"Invoking {name}");
                        action.Invoke();
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine($"Exception in {name}: {exception}");
                        //Thread.Sleep(5000);// Give redis some time to wake up!
                    }

                    Thread.Sleep(100);
                }
            });
        }
    }
}