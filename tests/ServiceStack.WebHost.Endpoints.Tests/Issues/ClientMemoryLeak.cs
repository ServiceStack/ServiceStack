using System.Diagnostics;
using System.Reflection;
using Funq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Net;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.Issues
{
    [Explicit,Ignore("Regression Test")]
    [TestFixture]
    public class ClientMemoryLeak
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(typeof(ClientMemoryLeak).Name, typeof(LeakServices).Assembly)
            { }

            public override void Configure(Container container) { }
        }

        private readonly ServiceStackHost appHost;
        public ClientMemoryLeak()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            appHost.Dispose();
        }

        [Route("/leak/{Name}")]
        public class LeakRequest : IReturn<LeakRequest>
        {
            public string Name { get; set; }
        }

        public class LeakServices : Service
        {
            public object Any(LeakRequest request)
            {
                return request;
            }
        }

        [Test]
        public void Run_GET_dto_in_loop()
        {
            var client = new JsonServiceClient(Config.ListeningOn);

            client.Get(new LeakRequest { Name = "warmup" });

            var sw = Stopwatch.StartNew();
            var elapsedTicks = new List<double> { sw.ElapsedMilliseconds };

            for (int i = 0; i < 10001; i++)
            {
                var response = client.Get(new LeakRequest { Name = "request" + i });
                Assert.That(response.Name, Is.EqualTo("request" + i));
                elapsedTicks.Add(sw.ElapsedTicks);
            }

            for (int i = 0; i < 10001; i += 1000)
            {
                "Elapsed Time: {0} ticks for Request at: #{1}".Print(
                    elapsedTicks[i + 1] - elapsedTicks[i], i);
            }
        }

        [Test]
        public void Run_GET_url_in_loop()
        {
            var client = new JsonServiceClient(Config.ListeningOn);

            client.Get(new LeakRequest { Name = "warmup" });

            var sw = Stopwatch.StartNew();
            var elapsedTicks = new List<double> { sw.ElapsedMilliseconds };

            for (int i = 0; i < 10001; i++)
            {
                var response = client.Get<LeakRequest>("/leak/request" + i);
                Assert.That(response.Name, Is.EqualTo("request" + i));
                elapsedTicks.Add(sw.ElapsedTicks);
            }

            for (int i = 0; i < 10001; i += 1000)
            {
                "Elapsed Time: {0} ticks for Request at: #{1}".Print(
                    elapsedTicks[i + 1] - elapsedTicks[i], i);
            }
        }

        [Test]
        public void Run_GET_url_HttpWebResponse_in_loop()
        {
            var client = new JsonServiceClient(Config.ListeningOn);

            client.Get(new LeakRequest { Name = "warmup" });

            var sw = Stopwatch.StartNew();
            var elapsedTicks = new List<double> { sw.ElapsedMilliseconds };

            for (int i = 0; i < 10001; i++)
            {
                using (HttpWebResponse response = client.Get("/leak/request" + i)) {}
                elapsedTicks.Add(sw.ElapsedTicks);
            }

            for (int i = 0; i < 10001; i += 1000)
            {
                "Elapsed Time: {0} ticks for Request at: #{1}".Print(
                    elapsedTicks[i + 1] - elapsedTicks[i], i);
            }
        }
    }
}