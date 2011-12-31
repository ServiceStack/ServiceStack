using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.ServiceHost;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Service;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class RequestFilterAttribute : Attribute, IHasRequestFilter
    {
        public void RequestFilter(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            var dto = requestDto as AttributeFiltered;
            dto.RequestFilterExecuted = true;
        }
    }

    [RestService("/attributefiltered")]
    [RequestFilter]
    public class AttributeFiltered
    {
        public bool RequestFilterExecuted { get; set; }
    }

    public class ResponseFilterAttribute : Attribute, IHasResponseFilter
    {
        public void ResponseFilter(IHttpRequest req, IHttpResponse res, object responseDto)
        {
            var dto = responseDto as AttributeFilteredResponse;
            dto.ResponseFilterExecuted = true;
        }
    }

    [ResponseFilter]
    public class AttributeFilteredResponse
    {
        public bool ResponseFilterExecuted { get; set; }
        public bool RequestFilterExecuted { get; set; }
    }

    public class AttributeFilteredService : IService<AttributeFiltered>
    {
        public object Execute(AttributeFiltered request)
        {
            return new AttributeFilteredResponse() { ResponseFilterExecuted = false, RequestFilterExecuted = request.RequestFilterExecuted };
        }
    }

    [TestFixture]
    public class AttributeFiltersTest
    {
        private const string ListeningOn = "http://localhost:82/";
        private const string ServiceClientBaseUri = "http://localhost:82/";

        public class AttributeFiltersAppHostHttpListener
            : AppHostHttpListenerBase
        {

            public AttributeFiltersAppHostHttpListener()
                : base("Attribute Filters Tests", typeof(AttributeFilteredService).Assembly) { }

            public override void Configure(Funq.Container container)
            {
            }
        }

        AttributeFiltersAppHostHttpListener appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new AttributeFiltersAppHostHttpListener();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        static IServiceClient[] ServiceClients = 
        {
            new JsonServiceClient(ServiceClientBaseUri),
            new Soap11ServiceClient(ServiceClientBaseUri),
            new Soap12ServiceClient(ServiceClientBaseUri)
        };

        [Test, TestCaseSource("ServiceClients")]
        public void Request_and_Response_Filters_are_executed_using_ServiceClient(IServiceClient client)
        {
            var response = client.Send<AttributeFilteredResponse>(new AttributeFiltered() { RequestFilterExecuted = false });
            Assert.IsTrue(response.RequestFilterExecuted);
            Assert.IsTrue(response.ResponseFilterExecuted);
        }

        static IRestClient[] RestClients = 
        {
            new JsonServiceClient(ServiceClientBaseUri),
            new XmlServiceClient(ServiceClientBaseUri),
            new JsvServiceClient(ServiceClientBaseUri)
        };

        [Test, TestCaseSource("RestClients")]
        public void Request_and_Response_Filters_are_executed_using_RestClient(IRestClient client)
        {
            var response = client.Post<AttributeFilteredResponse>("attributefiltered", new AttributeFiltered() { RequestFilterExecuted = false });
            Assert.IsTrue(response.RequestFilterExecuted);
            Assert.IsTrue(response.ResponseFilterExecuted);
        }
    }
}
