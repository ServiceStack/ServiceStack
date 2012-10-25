using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ServiceStack.Common.Web;
using ServiceStack.Service;
using ServiceStack.ServiceHost;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceInterface;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Utils;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/onewayrequest", "DELETE")]
    public class DeleteOneWayRequest : IReturnVoid
    {
        public string Prefix { get; set; }
    }

    [Route("/onewayrequest", "POST")]
    public class PostOneWayRequest : IReturnVoid
    {
        public string Prefix { get; set; }
    }

    public class OneWayService : ServiceInterface.Service
    {
        public static string LastResult { get; set; }
        public void Delete(DeleteOneWayRequest oneWayRequest)
        {
            LastResult = oneWayRequest.Prefix + " " + Request.HttpMethod;
        }

        public void Post(PostOneWayRequest oneWayRequest)
        {
            LastResult = oneWayRequest.Prefix + " " + Request.HttpMethod;
        }
    }

    [TestFixture]
    public class OneWayServiceTests
    {
        private const string ListeningOn = "http://localhost:8023/";
        private const string ServiceClientBaseUri = "http://localhost:8023/";

        public class OneWayServiceAppHostHttpListener
            : AppHostHttpListenerBase
        {

            public OneWayServiceAppHostHttpListener()
                : base("Cors Feature Tests", typeof(OneWayService).Assembly) { }

            public override void Configure(Funq.Container container)
            {
            }
        }

        OneWayServiceAppHostHttpListener appHost;
        private IRestClient client;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new OneWayServiceAppHostHttpListener();
            appHost.Init();
            appHost.Start(ListeningOn);

            client = new JsonServiceClient(ServiceClientBaseUri);
            OneWayService.LastResult = null;
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }


        [Test]
        public void Delete()
        {
            client.Delete(new DeleteOneWayRequest() { Prefix = "Delete" });
            Assert.That(OneWayService.LastResult, Is.EqualTo("Delete DELETE"));

        }

        [Test]
        public void Send()
        {
            client.Post(new PostOneWayRequest() { Prefix = "Post" });
            Assert.That(OneWayService.LastResult, Is.EqualTo("Post POST"));
        }

    }
}
