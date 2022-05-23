using System;
using System.Collections.Generic;
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

                Plugins.Add(new RequestLogsFeature
                {
                    LimitToServiceRequests = false
                });

                Plugins.Add(new CorsFeature());
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

            var response = client.Get(new RequestLogsTest { Name = "foo1" });

            var json = Config.ListeningOn.CombineWith("requestlogs").GetJsonFromUrl();
            var requestLogs = json.FromJson<RequestLogsResponse>();
            var requestLog = requestLogs.Results.First();
            var request = (RequestLogsTest)requestLog.RequestDto;
            Assert.That(request.Name, Is.EqualTo("foo1"));
            Assert.That(requestLog.Referer, Is.EqualTo(Config.ListeningOn));
        }

        [Test]
        public void Does_log_autobatch_request()
        {
            var client = new JsonServiceClient(Config.ListeningOn)
            {
                RequestFilter = req => req.Referer = Config.ListeningOn
            };

            var request = new[]
            {
                new RequestLogsTest { Name = "foo1" },
                new RequestLogsTest { Name = "bar1" }
            };
            var response = client.SendAll(request);

            var json = Config.ListeningOn.CombineWith("requestlogs").GetJsonFromUrl();
            var requestLogs = json.FromJson<RequestLogsResponse>();
            var requestLog = requestLogs.Results.First();

            var loggedRequests = requestLog.RequestDto.ConvertTo<List<RequestLogsTest>>();

            Assert.That(request is IEnumerable<RequestLogsTest>);
            Assert.That(loggedRequests is IEnumerable<RequestLogsTest>);

            Assert.That(loggedRequests.First().Name, Is.EqualTo("foo1"));
            Assert.That(loggedRequests.Last().Name, Is.EqualTo("bar1"));

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
                var response = client.Get(new RequestLogsErrorTest { Message = "foo2" });
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.Message, Is.EqualTo("Error: foo2"));
                var json = Config.ListeningOn.CombineWith("requestlogs").GetJsonFromUrl();
                var requestLogs = json.FromJson<RequestLogsResponse>();
                var requestLog = requestLogs.Results.First();
                var request = (RequestLogsErrorTest)requestLog.RequestDto;
                Assert.That(request.Message, Is.EqualTo("foo2"));
                Assert.That(requestLog.Referer, Is.EqualTo(Config.ListeningOn));
            }
        }

        [Test]
        public void Does_log_Options_request()
        {
            var response = Config.ListeningOn.CombineWith("requestlogs-test")
                .AddQueryParam("name", "foo3")
                .OptionsFromUrl(requestFilter: req => req.With(c => c.Referer = Config.ListeningOn));

            var json = Config.ListeningOn.CombineWith("requestlogs").GetJsonFromUrl();
            var requestLogs = json.FromJson<RequestLogsResponse>();
            var requestLog = requestLogs.Results.First();
            Assert.That(requestLog.HttpMethod, Is.EqualTo("OPTIONS"));
            Assert.That(requestLog.Referer, Is.EqualTo(Config.ListeningOn));
        }

        [Test]
        public void Does_log_error_response()
        {
            var client = new JsonServiceClient(Config.ListeningOn)
            {
                RequestFilter = req => req.Referer = Config.ListeningOn
            };

            try
            {
                var response = client.Get(new RequestLogsErrorTest { Message = "foo2" });
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.Message, Is.EqualTo("Error: foo2"));
                var json = Config.ListeningOn.CombineWith("requestlogs").GetJsonFromUrl();
                var requestLogs = json.FromJson<RequestLogsResponse>();
                var requestLog = requestLogs.Results.First();
                var request = (RequestLogsErrorTest)requestLog.RequestDto;
                Assert.That(requestLog.ErrorResponse != null);
                Assert.That(requestLog.ErrorResponse is ErrorResponse);
                var responseStatus = requestLog.ErrorResponse.GetResponseStatus();
                Assert.That(responseStatus.Message == "Error: foo2");
            }
        }

        [Test]
        public void Does_log_autobatch_error_response()
        {
            var client = new JsonServiceClient(Config.ListeningOn)
            {
                RequestFilter = req => req.Referer = Config.ListeningOn
            };

            var request = new[]
            {
                new RequestLogsErrorTest { Message = "foo1" },
                new RequestLogsErrorTest { Message = "bar1" }
            };

            try
            {
                var response = client.SendAll(request);
            }
            catch (WebServiceException ex)
            {

                var json = Config.ListeningOn.CombineWith("requestlogs").GetJsonFromUrl();
                var requestLogs = json.FromJson<RequestLogsResponse>();
                var requestLog = requestLogs.Results.First();

                Assert.That(requestLog.ErrorResponse != null);
                Assert.That(requestLog.ErrorResponse is ErrorResponse);
                var responseStatus = requestLog.ErrorResponse.GetResponseStatus();
                Assert.That(responseStatus.Message == "Error: foo1");
            }
        }
    }
}