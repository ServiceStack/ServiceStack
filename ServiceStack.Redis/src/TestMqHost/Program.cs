using System;
using System.Threading;
using ServiceStack;
using ServiceStack.Logging;
using ServiceStack.Redis;
using ServiceStack.Redis.Messaging;
using ServiceStack.Text;

namespace TestMqHost
{
    public class Incr
    {
        public int Value { get; set; }
    }

    class Program
    {
        static void Main2(string[] args)
        {
            var sbLogFactory = new StringBuilderLogFactory();
            LogManager.LogFactory = sbLogFactory;
            var log = LogManager.GetLogger(typeof(Program));

            var clientManager = new PooledRedisClientManager(new[] { "localhost" })
            {
                PoolTimeout = 1000,
            };

            var mqHost = new RedisMqServer(clientManager, retryCount: 2);

            var msgsProcessed = 0;
            var sum = 0;
            mqHost.RegisterHandler<Incr>(c =>
            {
                var dto = c.GetBody();
                sum += dto.Value;
                log.InfoFormat("Received {0}, sum: {1}", dto.Value, sum);
                msgsProcessed++;
                return null;
            });

            mqHost.Start();

            10.Times(i =>
            {
                ThreadPool.QueueUserWorkItem(x =>
                {
                    using (var client = mqHost.CreateMessageQueueClient())
                    {
                        try
                        {
                            log.InfoFormat("Publish: {0}...", i);
                            client.Publish(new Incr { Value = i });
                        }
                        catch (Exception ex)
                        {
                            log.InfoFormat("Start Publish exception: {0}", ex.Message);
                            clientManager.GetClientPoolActiveStates().PrintDump();
                            clientManager.GetReadOnlyClientPoolActiveStates().PrintDump();
                        }
                        Thread.Sleep(10);
                    }
                });
            });

            ThreadPool.QueueUserWorkItem(_ =>
            {
                using (var client = (RedisClient)clientManager.GetClient())
                {
                    client.SetConfig("timeout", "1");
                    var clientAddrs = client.GetClientList().ConvertAll(x => x["addr"]);
                    log.InfoFormat("Killing clients: {0}...", clientAddrs.Dump());

                    try
                    {
                        clientAddrs.ForEach(client.ClientKill);
                    }
                    catch (Exception ex)
                    {
                        log.InfoFormat("Client exception: {0}", ex.Message);
                    }
                }
            });

            20.Times(i =>
            {
                using (var client = mqHost.CreateMessageQueueClient())
                {
                    try
                    {
                        log.InfoFormat("Publish: {0}...", i);
                        client.Publish(new Incr { Value = i });
                    }
                    catch (Exception ex)
                    {
                        log.InfoFormat("Publish exception: {0}", ex.Message);
                        clientManager.GetClientPoolActiveStates().PrintDump();
                        clientManager.GetReadOnlyClientPoolActiveStates().PrintDump();
                    }
                }

                Thread.Sleep(1000);
            });

            Thread.Sleep(2000);
            "Messages processed: {0}".Print(msgsProcessed);
            "Logs: ".Print();
            sbLogFactory.GetLogs().Print();
            Console.ReadKey();
        }
    }
}
