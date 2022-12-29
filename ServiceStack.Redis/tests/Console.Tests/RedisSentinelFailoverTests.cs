using System;
using System.Threading;
using ServiceStack;
using ServiceStack.Logging;
using ServiceStack.Redis;
using ServiceStack.Text;
using Timer = System.Timers.Timer;

namespace ConsoleTests
{
    /*
     *  1. Start all Redis Servers + Sentinels
     *  2. Failover the first Sentinel
     *  3. Kill the current master
     */
    public abstract class RedisSentinelFailoverTests
    {
        protected static ILog log;

        public int MessageInterval = 1000;

        public bool UseRedisManagerPool = false;

        public void Execute()
        {
            RedisConfig.EnableVerboseLogging = false;
            LogManager.LogFactory = new ConsoleLogFactory(debugEnabled: true);
            log = LogManager.GetLogger(GetType());

            RedisConfig.DefaultReceiveTimeout = 10000;

            OnSetUp();

            using (var sentinel = CreateSentinel())
            {
                if (UseRedisManagerPool)
                {
                    sentinel.RedisManagerFactory = (masters, replicas) =>
                        new RedisManagerPool(masters);
                }

                var redisManager = sentinel.Start();

                int i = 0;
                var clientTimer = new Timer
                {
                    Interval = MessageInterval,
                    Enabled = true
                };
                clientTimer.Elapsed += (sender, args) =>
                {
                    log.Debug("clientTimer.Elapsed: " + (i++));

                    try
                    {
                        string key = null;
                        using (var master = (RedisClient)redisManager.GetClient())
                        {
                            var counter = master.Increment("key", 1);
                            key = "key" + counter;
                            log.DebugFormat("Set key {0} in read/write client #{1}@{2}", key, master.Id, master.GetHostString());
                            master.SetValue(key, "value" + 1);
                        }
                        using (var readOnly = (RedisClient)redisManager.GetReadOnlyClient())
                        {
                            log.DebugFormat("Get key {0} in read-only client #{1}@{2}", key, readOnly.Id, readOnly.GetHostString());
                            var value = readOnly.GetValue(key);
                            log.DebugFormat("{0} = {1}", key, value);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        log.DebugFormat("ObjectDisposedException detected, disposing timer...");
                        clientTimer.Dispose();
                    }
                    catch (Exception ex)
                    {
                        log.Error("Error in Timer", ex);
                    }

                    if (i % 10 == 0)
                        log.Debug(RedisStats.ToDictionary().Dump());
                };

                log.Debug("Sleeping for 5000ms...");
                Thread.Sleep(5000);

                log.Debug("Failing over master...");
                sentinel.ForceMasterFailover();
                log.Debug("master was failed over");

                log.Debug("Sleeping for 20000ms...");
                Thread.Sleep(20000);

                try
                {
                    var debugConfig = sentinel.GetMaster();
                    using (var master = new RedisClient(debugConfig))
                    {
                        log.Debug("Putting master '{0}' to sleep for 35 seconds...".Fmt(master.GetHostString()));
                        master.DebugSleep(35);
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Error retrieving master for DebugSleep()", ex);
                }

                log.Debug("After DEBUG SLEEP... Sleeping for 5000ms...");
                Thread.Sleep(5000);

                log.Debug("RedisStats:");
                log.Debug(RedisStats.ToDictionary().Dump());

                System.Console.ReadLine();
            }

            OnTearDown();
        }

        protected abstract RedisSentinel CreateSentinel();

        protected virtual void OnSetUp() { }
        protected virtual void OnTearDown() { }
    }
}