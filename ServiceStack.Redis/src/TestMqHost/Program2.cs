using System;
using System.Diagnostics;
using System.Threading;
using ServiceStack.Redis;
using ServiceStack.Redis.Messaging;
using ServiceStack.Text;

namespace TestMqHost
{
    class Program2
    {

        static void Main(string[] args)
        {
            var clientManager = new PooledRedisClientManager(new[] { "localhost" })
            {
                PoolTimeout = 1000,
            };
            using (var client = clientManager.GetClient())
            {
                client.FlushAll();
            }

            var mqHost = new RedisMqServer(clientManager);

            var msgsProcessed = 0;
            var msgsQueued = 0;
            var sum = 0;
            mqHost.RegisterHandler<Incr>(c =>
            {
                var dto = c.GetBody();
                sum += dto.Value;
                Console.WriteLine("Received {0}, sum: {1}", dto.Value, sum);
                msgsProcessed++;
                return null;
            });

            mqHost.Start();
            var processes = Process.GetProcessesByName("redis-server");
            var timer = new Timer(s =>
            {
                using (var client = mqHost.MessageFactory.CreateMessageProducer())
                {
                    try
                    {
                        client.Publish(new Incr { Value = 1 });
                        msgsQueued++;
                        Console.WriteLine("Message #{0} published.", msgsQueued);
                    }
                    catch { }
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

            Thread.Sleep(5000);
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            Thread.Sleep(1000);

            int msgsQueuedBeforeKill = msgsQueued;
            int msgsProcessedBeforeKill = msgsProcessed;
            processes[0].Kill();

            timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            Thread.Sleep(15000);
            timer.Dispose();

            Thread.Sleep(1000);

            mqHost.GetStats().PrintDump();
            mqHost.GetStatus().Print();

            "Messages queued before kill: {0}".Print(msgsQueuedBeforeKill);
            "Messages processed before kill: {0}".Print(msgsProcessedBeforeKill);

            "Messages queued: {0}".Print(msgsQueued);
            "Messages processed: {0}".Print(msgsProcessed);

            Console.ReadKey();
        }
    }
}