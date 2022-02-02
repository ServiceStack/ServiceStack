using System;
using System.Diagnostics;
using System.Threading;
using Funq;
using NUnit.Framework;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class SleepTest : IReturn<SleepTestResponse>
    {
        public string Name { get; set; }
        public int WaitingSecs { get; set; }
    }

    public class SleepTestResponse
    {
        public string Message { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class TestConcurrencyService : Service
    {
        public object Any(SleepTest request)
        {
            var sw = Stopwatch.StartNew();

            Thread.Sleep(TimeSpan.FromSeconds(request.WaitingSecs));

            return new SleepTestResponse
            {
                Message = $"{request.Name} took {sw.Elapsed.TotalSeconds} secs",
            };
        }
    }

    [Ignore("Comment out to run load test")]
    public class ConcurrencyTest
    {
        private static ILog log;
        private readonly ServiceStackHost appHost;

        public ConcurrencyTest()
        {
            LogManager.LogFactory = new ConsoleLogFactory();
            log = LogManager.GetLogger(typeof(ConcurrencyTest));

            appHost = new AppHost()
                .Init()
                .Start(Config.AbsoluteBaseUri);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        public class AppHost : AppHostHttpListenerPoolBase
        {
            public AppHost() : base("Server", 500, typeof(TestConcurrencyService).Assembly) {}

            public override void Configure(Container container)
            {
            }
        }

        [Test]
        public void Does_handle_concurrent_requests()
        {
            var rand = new Random();
            var client = new JsonHttpClient(Config.AbsoluteBaseUri);
            client.GetHttpClient().Timeout = TimeSpan.FromMinutes(5);
            long responsesReceived = 0;
            long totalSecondsWaited = 0;
            var sw = Stopwatch.StartNew();
            const int ConcurrentRequests = 50;

            ConcurrentRequests.Times(i =>
            {
                Interlocked.Increment(ref responsesReceived);

                ThreadPool.QueueUserWorkItem(async _ =>
                {
                    var request = new SleepTest
                    {
                        Name = $"Request {i+1}",
                        WaitingSecs = rand.Next(30, 60),
                    };
                    Interlocked.Add(ref totalSecondsWaited, request.WaitingSecs);

                    log.Info($"[{DateTime.Now.TimeOfDay}] Sending {request.Name} to sleep for {request.WaitingSecs} seconds...");
                    
                    try
                    {
                        var response = await client.GetAsync(request);

                        log.Info($"[{DateTime.Now.TimeOfDay}] Received {request.Name}: {response.Message}");
                    }
                    catch (Exception ex)
                    {
                        log.Error($"[{DateTime.Now.TimeOfDay}] Error Response: {ex.UnwrapIfSingleException().Message}", ex);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref responsesReceived);
                    }
                });
            });

            while (Interlocked.Read(ref responsesReceived) > 0)
            {
                Thread.Sleep(10);
            }

            log.Info($"Took {sw.Elapsed.TotalSeconds} to execute {ConcurrentRequests} Concurrent Requests waiting a total of {totalSecondsWaited} seconds.");
        }
    }
}
