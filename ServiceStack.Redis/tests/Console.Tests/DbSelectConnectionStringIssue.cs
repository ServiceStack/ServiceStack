using System;
using System.Threading;
using ServiceStack;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace ConsoleTests;

class DbSelectConnectionStringIssue
{
    public void Execute()
    {
        LogManager.LogFactory = new ConsoleLogFactory();

        Licensing.RegisterLicense("<removed>");

        var redisManagerPool = new RedisManagerPool("redis://redisHost?db=7");

        for (int i = 0; i < 5; i++)
        {
            try
            {
                using (IRedisClient client = redisManagerPool.GetClient())
                {
                    string value = client.GetValue("status");

                    Console.WriteLine($"Successfully retrieved value => '{value}'");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception handled \n{ex}");
            }

            Console.WriteLine("Sleeping for 25 seconds to allow client to be garbage collected");
            Thread.Sleep(TimeSpan.FromSeconds(25));
        }
    }
}