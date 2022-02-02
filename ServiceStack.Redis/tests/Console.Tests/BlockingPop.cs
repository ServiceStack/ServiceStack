using System;
using ServiceStack;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace ConsoleTests
{
    public class BlockingPop
    {
        public void Execute()
        {
            LogManager.LogFactory = new ConsoleLogFactory();
            var log = LogManager.LogFactory.GetLogger("redistest");

            // ********************
            // set REDIS CONFIGS
            // ********************
            RedisConfig.DefaultConnectTimeout = 1 * 1000;
            RedisConfig.DefaultSendTimeout = 1 * 1000;
            RedisConfig.DefaultReceiveTimeout = 1 * 1000;
            //RedisConfig.DefaultRetryTimeout = 15 * 1000;
            RedisConfig.DefaultIdleTimeOutSecs = 240;
            RedisConfig.BackOffMultiplier = 10;
            RedisConfig.BufferPoolMaxSize = 500000;
            RedisConfig.VerifyMasterConnections = true;
            RedisConfig.HostLookupTimeoutMs = 1000;
            RedisConfig.DeactivatedClientsExpiry = TimeSpan.FromSeconds(15);
            RedisConfig.EnableVerboseLogging = true;

            var redisManager = new RedisManagerPool("localhost?connectTimeout=1000");

            // how many test items to create
            var items = 5;
            // how long to try popping
            var waitForSeconds = 30;
            // name of list
            var listId = "testlist";

            var startedAt = DateTime.Now;

            log.Info("--------------------------");
            log.Info("push {0} items to a list, then try pop for {1} seconds. repeat.".Fmt(items, waitForSeconds));
            log.Info("--------------------------");

            using (var redis = redisManager.GetClient())
            {
                do
                {
                    // add items to list
                    for (int i = 1; i <= items; i++)
                    {
                        redis.PushItemToList(listId, $"item {i}");
                    }

                    do
                    {
                        var item = redis.BlockingPopItemFromList(listId, null);

                        // log the popped item.  if BRPOP timeout is null and list empty, I do not expect to print anything
                        log.InfoFormat("{0}", string.IsNullOrEmpty(item) ? " list empty " : item);

                        System.Threading.Thread.Sleep(1000);

                    } while (DateTime.Now - startedAt < TimeSpan.FromSeconds(waitForSeconds));

                    log.Info("--------------------------");
                    log.Info("completed first loop");
                    log.Info("--------------------------");

                } while (DateTime.Now - startedAt < TimeSpan.FromSeconds(2 * waitForSeconds));

                log.Info("--------------------------");
                log.Info("completed outer loop");
            }
        }
    }
}