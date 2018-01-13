using System;
using System.Linq;
using Funq;
using NUnit.Framework;
using ServiceStack.Admin;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/requestlogs-test")]
    public class RequestLogsTest : IReturn<RequestLogsTest>
    {
        public string Name { get; set; }
    }

    [Route("/requestlogs-error-test")]
    public class RequestLogsErrorTest : IReturn<RequestLogsErrorTest>
    {
        public string Message { get; set; }
    }

    class MyServices : Service
    {
        public object Any(RequestLogsTest request) => request;

        public object Any(RequestLogsErrorTest request) =>
            throw new Exception("Error: " + request.Message);
    }

    public class RequestLogsFeatureTests
    {
        private readonly ServiceStackHost appHost;

        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(RequestLogsFeatureTests), typeof(MyServices).Assembly) { }

            public override void Configure(Container container)
            {
                SetConfig(new HostConfig
                {
                    DebugMode = true
                });

                Plugins.Add(new RequestLogsFeature());
            }
        }

        public RequestLogsFeatureTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Does_log_Service_request()
        {
            var client = new JsonServiceClient(Config.ListeningOn)
            {
                RequestFilter = req => req.Referer = Config.ListeningOn
            };

            var response = client.Get(new RequestLogsTest { Name = "foo" });

            var json = Config.ListeningOn.CombineWith("requestlogs").GetJsonFromUrl();
            var requestLogs = json.FromJson<RequestLogsResponse>();
            var requestLog = requestLogs.Results.First();
            var request = (RequestLogsTest)requestLog.RequestDto;
            Assert.That(request.Name, Is.EqualTo("foo"));
            Assert.That(requestLog.Referer, Is.EqualTo(Config.ListeningOn));
        }

        [Test]
        public void Does_log_Error_request()
        {
            var client = new JsonServiceClient(Config.ListeningOn)
            {
                RequestFilter = req => req.Referer = Config.ListeningOn
            };

            try
            {
                var response = client.Get(new RequestLogsErrorTest { Message = "foo" });
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.Message, Is.EqualTo("Error: foo"));
                var json = Config.ListeningOn.CombineWith("requestlogs").GetJsonFromUrl();
                var requestLogs = json.FromJson<RequestLogsResponse>();
                var requestLog = requestLogs.Results.First();
                var request = (RequestLogsErrorTest)requestLog.RequestDto;
                Assert.That(request.Message, Is.EqualTo("foo"));
                Assert.That(requestLog.Referer, Is.EqualTo(Config.ListeningOn));
            }
        }
    }
}