using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using ServiceStack;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ConsoleTests
{
    public class LocalRedisSentinelFailoverTests : RedisSentinelFailoverTests
    {
        public static int[] RedisPorts = new[] { 6380, 6381, 6382, };

        public static string[] SentinelHosts = new[]
        {
            "127.0.0.1:26380",
            "127.0.0.1:26381",
            "127.0.0.1:26382",
        };

        public bool StartAndStopRedisServers = false;

        private static void StartRedisServersAndSentinels()
        {

            log.Debug("Starting all Redis Servers...");
            foreach (var port in RedisPorts)
            {
                StartRedisServer(port);
            }
            Thread.Sleep(1500);

            log.Debug("Starting all Sentinels...");
            foreach (var port in RedisPorts)
            {
                StartRedisSentinel(port);
            }
            Thread.Sleep(1500);
        }

        private static void ShutdownRedisSentinelsAndServers()
        {
            log.Debug("Shutting down all Sentinels...");
            foreach (var host in SentinelHosts)
            {
                try
                {
                    var client = new RedisClient(host)
                    {
                        ConnectTimeout = 100,
                        ReceiveTimeout = 100,
                    };
                    client.ShutdownNoSave();
                }
                catch (Exception ex)
                {
                    log.Error("Error trying to shutdown {0}".Fmt(host), ex);
                }
            }

            log.Debug("Shutting down all Redis Servers...");
            foreach (var port in RedisPorts)
            {
                try
                {
                    var client = new RedisClient("127.0.0.1", port)
                    {
                        ConnectTimeout = 100,
                        ReceiveTimeout = 100,
                    };
                    client.ShutdownNoSave();
                }
                catch (Exception ex)
                {
                    "Error trying to shutdown {0}".Print(port);
                    ex.Message.Print();
                }
            }
        }

        public static void StartRedisServer(int port)
        {
            var pInfo = new ProcessStartInfo
            {
                FileName = new FileInfo(@"..\..\..\..\src\sentinel\redis\redis-server.exe").FullName,
                Arguments = new FileInfo(@"..\..\..\..\src\sentinel\redis-{0}\redis.windows.conf".Fmt(port)).FullName,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            ThreadPool.QueueUserWorkItem(state => Process.Start(pInfo));
        }

        public static void StartRedisSentinel(int port)
        {
            var pInfo = new ProcessStartInfo
            {
                FileName = new FileInfo(@"..\..\..\..\src\sentinel\redis\redis-server.exe").FullName,
                Arguments = new FileInfo(@"..\..\..\..\src\sentinel\redis-{0}\sentinel.conf".Fmt(port)).FullName + " --sentinel",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            ThreadPool.QueueUserWorkItem(state => Process.Start(pInfo));
        }

        protected override RedisSentinel CreateSentinel()
        {
            return new RedisSentinel(SentinelHosts);
        }

        protected override void OnSetUp()
        {
            if (StartAndStopRedisServers)
                StartRedisServersAndSentinels();
        }

        protected override void OnTearDown()
        {
            log.Debug("Press Enter to shutdown Redis Sentinels and Servers...");
            Console.ReadLine();
            if (StartAndStopRedisServers)
                ShutdownRedisSentinelsAndServers();

            Console.ReadLine();
        }
    }

}