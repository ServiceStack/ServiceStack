﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.ServiceHost;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Service;
using ServiceStack.ServiceInterface;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    //Always executed
    public class FilterTestAttribute : Attribute, IHasRequestFilter
    {
        public void RequestFilter(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            var dto = requestDto as AttributeFiltered;
            dto.RequestFilterExecuted = true;
        }
    }

    //Only executed for the provided HTTP methods (GET, POST) 
    public class ContextualFilterTestAttribute : RequestFilterAttribute
    {
        public ContextualFilterTestAttribute(ApplyTo applyTo)
            : base(applyTo)
        {
        }

        public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            var dto = requestDto as AttributeFiltered;
            dto.ContextualRequestFilterExecuted = true;
        }
    }

    [RestService("/attributefiltered")]
    [FilterTest]
    [ContextualFilterTest(ApplyTo.Delete | ApplyTo.Put)]
    public class AttributeFiltered
    {
        public bool RequestFilterExecuted { get; set; }
        public bool ContextualRequestFilterExecuted { get; set; }
    }

    //Always executed
    public class ResponseFilterTestAttribute : Attribute, IHasResponseFilter
    {
        public void ResponseFilter(IHttpRequest req, IHttpResponse res, object responseDto)
        {
            var dto = responseDto as AttributeFilteredResponse;
            dto.ResponseFilterExecuted = true;
        }
    }

    //Only executed for the provided HTTP methods (GET, POST) 
    public class ContextualResponseFilterTestAttribute : ResponseFilterAttribute
    {
        public ContextualResponseFilterTestAttribute(ApplyTo applyTo)
            : base(applyTo)
        {
        }

        public override void Execute(IHttpRequest req, IHttpResponse res, object responseDto)
        {
            var dto = responseDto as AttributeFilteredResponse;
            dto.ContextualResponseFilterExecuted = true;
        }
    }

    [ResponseFilterTest]
    [ContextualResponseFilterTest(ApplyTo.Delete | ApplyTo.Put)]
    public class AttributeFilteredResponse
    {
        public bool ResponseFilterExecuted { get; set; }
        public bool ContextualResponseFilterExecuted { get; set; }

        public bool RequestFilterExecuted { get; set; }
        public bool ContextualRequestFilterExecuted { get; set; }
    }

    public class AttributeFilteredService : IService<AttributeFiltered>
    {
        public object Execute(AttributeFiltered request)
        {
            return new AttributeFilteredResponse() 
            { 
                ResponseFilterExecuted = false, 
                ContextualResponseFilterExecuted = false,
                RequestFilterExecuted = request.RequestFilterExecuted,
                ContextualRequestFilterExecuted = request.ContextualRequestFilterExecuted
            };
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
            Assert.IsFalse(response.ContextualRequestFilterExecuted);
            Assert.IsFalse(response.ContextualResponseFilterExecuted);
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
            Assert.IsFalse(response.ContextualRequestFilterExecuted);
            Assert.IsFalse(response.ContextualResponseFilterExecuted);
        }

        [Test, TestCaseSource("RestClients")]
        public void Contextual_Request_and_Response_Filters_are_executed_using_RestClient(IRestClient client)
        {
            var response = client.Delete<AttributeFilteredResponse>("attributefiltered");
            Assert.IsTrue(response.RequestFilterExecuted);
            Assert.IsTrue(response.ResponseFilterExecuted);
            Assert.IsTrue(response.ContextualRequestFilterExecuted);
            Assert.IsTrue(response.ContextualResponseFilterExecuted);
        }

        [Test, TestCaseSource("RestClients")]
        public void Multi_Contextual_Request_and_Response_Filters_are_executed_using_RestClient(IRestClient client)
        {
            var response = client.Put<AttributeFilteredResponse>("attributefiltered", new AttributeFiltered() { RequestFilterExecuted = false });
            Assert.IsTrue(response.RequestFilterExecuted);
            Assert.IsTrue(response.ResponseFilterExecuted);
            Assert.IsTrue(response.ContextualRequestFilterExecuted);
            Assert.IsTrue(response.ContextualResponseFilterExecuted);
        }
    }
}
