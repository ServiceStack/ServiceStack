using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using ServiceStack.Redis;
using ServiceStack.Text;
using Timer = System.Timers.Timer;

namespace ConsoleTests
{
    public class LongRunningRedisPubSubServer
    {
        private const string Channel = "longrunningtest";
        private static DateTime StartedAt;

        private static long MessagesSent = 0;
        private static long HeartbeatsSent = 0;
        private static long HeartbeatsReceived = 0;
        private static long StartCount = 0;
        private static long StopCount = 0;
        private static long DisposeCount = 0;
        private static long ErrorCount = 0;
        private static long FailoverCount = 0;
        private static long UnSubscribeCount = 0;

        public static RedisManagerPool Manager { get; set; }
        public static RedisPubSubServer PubSubServer { get; set; }

        public void Execute(string ipAddress)
        {
            Manager = new RedisManagerPool(ipAddress);
            StartedAt = DateTime.UtcNow;

            var q = new Timer { Interval = 1000 };
            q.Elapsed += OnInterval;
            q.Enabled = true;

            using (PubSubServer = new RedisPubSubServer(Manager, Channel)
            {
                OnStart = () =>
                {
                    Console.WriteLine("OnStart: #" + Interlocked.Increment(ref StartCount));
                },
                OnHeartbeatSent = () =>
                {
                    Console.WriteLine("OnHeartbeatSent: #" + Interlocked.Increment(ref HeartbeatsSent));
                },
                OnHeartbeatReceived = () =>
                {
                    Console.WriteLine("OnHeartbeatReceived: #" + Interlocked.Increment(ref HeartbeatsReceived));
                },
                OnMessage = (channel, msg) =>
                {
                    Console.WriteLine("OnMessage: @" + channel + ": " + msg);
                },
                OnStop = () =>
                {
                    Console.WriteLine("OnStop: #" + Interlocked.Increment(ref StopCount));
                },
                OnError = ex =>
                {
                    Console.WriteLine("OnError: #" + Interlocked.Increment(ref ErrorCount) + " ERROR: " + ex);
                },
                OnFailover = server =>
                {
                    Console.WriteLine("OnFailover: #" + Interlocked.Increment(ref FailoverCount));
                },
                OnDispose = () =>
                {
                    Console.WriteLine("OnDispose: #" + Interlocked.Increment(ref DisposeCount));
                },
                OnUnSubscribe = channel =>
                {
                    Console.WriteLine("OnUnSubscribe: #" + Interlocked.Increment(ref UnSubscribeCount) + " channel: " + channel);
                },
            })
            {
                Console.WriteLine("PubSubServer StartedAt: " + StartedAt.ToLongTimeString());
                PubSubServer.Start();

                "Press Enter to Quit...".Print();
                Console.ReadLine();
                Console.WriteLine("PubSubServer EndedAt: " + DateTime.UtcNow.ToLongTimeString());
                Console.WriteLine("PubSubServer TimeTaken: " + (DateTime.UtcNow - StartedAt).TotalSeconds + "s");
            }
        }

        private static void OnInterval(object sender, ElapsedEventArgs e)
        {
            Task.Factory.StartNew(PublishMessage);
        }

        private static void PublishMessage()
        {
            try
            {
                var message = "MSG: #" + Interlocked.Increment(ref MessagesSent);
                Console.WriteLine("PublishMessage(): " + message);
                using (var redis = Manager.GetClient())
                {
                    redis.PublishMessage(Channel, message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR PublishMessage: " + ex);
            }
        }
         
    }
}