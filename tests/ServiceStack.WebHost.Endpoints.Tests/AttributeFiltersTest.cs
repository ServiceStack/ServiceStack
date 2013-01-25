using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using ServiceStack.Common.Tests.ServiceClient.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Service;
using ServiceStack.ServiceInterface;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Utils;

namespace ServiceStack.WebHost.Endpoints.Tests
{

    //Always executed
    public class FilterTestAttribute : Attribute, IHasRequestFilter
    {
        private static ICacheClient previousCache;

        public ICacheClient Cache { get; set; }

        public int Priority { get; set; }

        public void RequestFilter(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            var dto = (AttributeFiltered)requestDto;
            dto.AttrsExecuted.Add(GetType().Name);
            dto.RequestFilterExecuted = true;

            //Check for equality to previous cache to ensure a filter attribute is no singleton
            dto.RequestFilterDependenyIsResolved = Cache != null && !Cache.Equals(previousCache);

            previousCache = Cache;
        }

        public IHasRequestFilter Copy()
        {
            return (IHasRequestFilter)this.MemberwiseClone();
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
            var dto = (AttributeFiltered)requestDto;
            dto.AttrsExecuted.Add(GetType().Name);
            dto.ContextualRequestFilterExecuted = true;
        }
    }

    [Route("/attributefiltered")]
    [FilterTest]
    [ContextualFilterTest(ApplyTo.Delete | ApplyTo.Put)]
    public class AttributeFiltered
    {
        public AttributeFiltered()
        {
            this.AttrsExecuted = new List<string>();
        }

        public bool RequestFilterExecuted { get; set; }
        public bool ContextualRequestFilterExecuted { get; set; }
        public bool RequestFilterDependenyIsResolved { get; set; }
        public List<string> AttrsExecuted { get; set; }
    }

    //Always executed
    public class ResponseFilterTestAttribute : Attribute, IHasResponseFilter
    {
        private static ICacheClient previousCache;

        public ICacheClient Cache { get; set; }

        public int Priority { get; set; }

        public void ResponseFilter(IHttpRequest req, IHttpResponse res, object response)
        {
            var dto = response.ToResponseDto() as AttributeFilteredResponse;
            dto.ResponseFilterExecuted = true;
            dto.ResponseFilterDependencyIsResolved = Cache != null && !Cache.Equals(previousCache);

            previousCache = Cache;
        }

        public IHasResponseFilter Copy()
        {
            return (IHasResponseFilter)this.MemberwiseClone();
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

    public class ThrowingFilterAttribute : RequestFilterAttribute
    {
        public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            throw new ArgumentException("exception message");
        }
    }

    [Route("/throwingattributefiltered")]
    public class ThrowingAttributeFiltered : IReturn<string>
    {
    }

    [ThrowingFilter]
    public class ThrowingAttributeFilteredService : IService
    {
        public object Any(ThrowingAttributeFiltered request)
        {
            return "OK";
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

        public bool RequestFilterDependenyIsResolved { get; set; }
        public bool ResponseFilterDependencyIsResolved { get; set; }
    }

    public class AttributeFilteredService : IService<AttributeFiltered>
    {
        public object Execute(AttributeFiltered request)
        {
            return new AttributeFilteredResponse() {
                ResponseFilterExecuted = false,
                ContextualResponseFilterExecuted = false,
                RequestFilterExecuted = request.RequestFilterExecuted,
                ContextualRequestFilterExecuted = request.ContextualRequestFilterExecuted,
                RequestFilterDependenyIsResolved = request.RequestFilterDependenyIsResolved,
                ResponseFilterDependencyIsResolved = false
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
                container.Register<ICacheClient>(c => new MemoryCacheClient()).ReusedWithin(Funq.ReuseScope.None);
                SetConfig(new EndpointHostConfig { DebugMode = true }); //show stacktraces
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
            new XmlServiceClient(ServiceClientBaseUri),
            new JsvServiceClient(ServiceClientBaseUri)
			//SOAP not supported in HttpListener
			//new Soap11ServiceClient(ServiceClientBaseUri),
			//new Soap12ServiceClient(ServiceClientBaseUri)
        };

        [Test, TestCaseSource("ServiceClients")]
        public void Request_and_Response_Filters_are_executed_using_ServiceClient(IServiceClient client)
        {
            var response = client.Send<AttributeFilteredResponse>(
                new AttributeFiltered { RequestFilterExecuted = false });
            Assert.IsTrue(response.RequestFilterExecuted);
            Assert.IsTrue(response.ResponseFilterExecuted);
            Assert.IsFalse(response.ContextualRequestFilterExecuted);
            Assert.IsFalse(response.ContextualResponseFilterExecuted);
            Assert.IsTrue(response.RequestFilterDependenyIsResolved);
            Assert.IsTrue(response.ResponseFilterDependencyIsResolved);
        }

        static IRestClient[] RestClients = 
        {
            new JsonServiceClient(ServiceClientBaseUri),
            new XmlServiceClient(ServiceClientBaseUri),
            new JsvServiceClient(ServiceClientBaseUri)
        };

        [Test]
        public void Proper_exception_is_serialized_to_client()
        {
            var client = new HtmlServiceClient(ServiceClientBaseUri);
            client.SetBaseUri(ServiceClientBaseUri);

            try
            {
                client.Get(new ThrowingAttributeFiltered());
            }
            catch (WebServiceException e)
            {
                //Ensure we have stack trace present
                Assert.IsTrue(e.ResponseBody.Contains("ThrowingFilterAttribute"), "No stack trace in the response (it's probably empty)");
            }
        }

        [Test, TestCaseSource("RestClients")]
        public void Request_and_Response_Filters_are_executed_using_RestClient(IRestClient client)
        {
            var response = client.Post<AttributeFilteredResponse>("attributefiltered", new AttributeFiltered() { RequestFilterExecuted = false });
            Assert.IsTrue(response.RequestFilterExecuted);
            Assert.IsTrue(response.ResponseFilterExecuted);
            Assert.IsFalse(response.ContextualRequestFilterExecuted);
            Assert.IsFalse(response.ContextualResponseFilterExecuted);
            Assert.IsTrue(response.RequestFilterDependenyIsResolved);
            Assert.IsTrue(response.ResponseFilterDependencyIsResolved);
        }

        [Test, TestCaseSource("RestClients")]
        public void Contextual_Request_and_Response_Filters_are_executed_using_RestClient(IRestClient client)
        {
            var response = client.Delete<AttributeFilteredResponse>("attributefiltered");
            Assert.IsTrue(response.RequestFilterExecuted);
            Assert.IsTrue(response.ResponseFilterExecuted);
            Assert.IsTrue(response.ContextualRequestFilterExecuted);
            Assert.IsTrue(response.ContextualResponseFilterExecuted);
            Assert.IsTrue(response.RequestFilterDependenyIsResolved);
            Assert.IsTrue(response.ResponseFilterDependencyIsResolved);
        }

        [Test, TestCaseSource("RestClients")]
        public void Multi_Contextual_Request_and_Response_Filters_are_executed_using_RestClient(IRestClient client)
        {
            var response = client.Put<AttributeFilteredResponse>("attributefiltered", new AttributeFiltered() { RequestFilterExecuted = false });
            Assert.IsTrue(response.RequestFilterExecuted);
            Assert.IsTrue(response.ResponseFilterExecuted);
            Assert.IsTrue(response.ContextualRequestFilterExecuted);
            Assert.IsTrue(response.ContextualResponseFilterExecuted);
            Assert.IsTrue(response.RequestFilterDependenyIsResolved);
            Assert.IsTrue(response.ResponseFilterDependencyIsResolved);
        }

        public class ExecutedFirstAttribute : RequestFilterAttribute
        {
            public ExecutedFirstAttribute()
            {
                Priority = int.MinValue;
            }

            public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
            {
                var dto = (AttributeFiltered)requestDto;
                dto.AttrsExecuted.Add(GetType().Name);
            }
        }

        [ExecutedFirst]
        [FilterTest]
        [RequiredRole("test")]
        [Authenticate]
        [RequiredPermission("test")]
        public class DummyHolder { }

        [Test]
        public void RequestFilters_are_prioritized()
        {
            EndpointHost.ServiceManager = new ServiceManager(typeof(DummyHolder).Assembly);

            EndpointHost.ServiceManager.Metadata.Add(typeof(AttributeFilteredService), typeof(DummyHolder), null);

            var attributes = FilterAttributeCache.GetRequestFilterAttributes(typeof(DummyHolder));
            var attrPriorities = attributes.ToList().ConvertAll(x => x.Priority);
            Assert.That(attrPriorities, Is.EquivalentTo(new[] { int.MinValue, -100, -90, -80, 0 }));

            var execOrder = new IHasRequestFilter[attributes.Length];
            var i = 0;
            for (; i < attributes.Length && attributes[i].Priority < 0; i++)
            {
                execOrder[i] = attributes[i];
                Console.WriteLine(attributes[i].Priority);
            }

            Console.WriteLine("break;");

            for (; i < attributes.Length; i++)
            {
                execOrder[i] = attributes[i];
                Console.WriteLine(attributes[i].Priority);
            }

            var execOrderPriorities = execOrder.ToList().ConvertAll(x => x.Priority);
            Console.WriteLine(execOrderPriorities.Dump());
            Assert.That(execOrderPriorities, Is.EquivalentTo(new[] { int.MinValue, -100, -90, -80, 0 }));
        }
    }
}
