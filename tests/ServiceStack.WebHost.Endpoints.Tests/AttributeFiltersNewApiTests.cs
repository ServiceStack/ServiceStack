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
    public class FilterTestNewApiAttribute : Attribute, IHasRequestFilter
    {
        private static ICacheClient previousCache;

        public ICacheClient Cache { get; set; }

        public int Priority { get; set; }

        public void RequestFilter(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            var dto = (AttributeFilteredNewApi)requestDto;
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
    public class ContextualFilterTestNewApiAttribute : RequestFilterAttribute
    {
        public ContextualFilterTestNewApiAttribute(ApplyTo applyTo)
            : base(applyTo)
        {
        }

        public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            var dto = (AttributeFilteredNewApi)requestDto;
            dto.AttrsExecuted.Add(GetType().Name);
            dto.ContextualRequestFilterExecuted = true;
        }
    }

    //Always executed, before global filters. (applied on DTO class)
    public class PrioritizedDtoFilterTestNewApiAttribute : RequestFilterAttribute
    {
        public PrioritizedDtoFilterTestNewApiAttribute()
        {
            Priority = -20;
        }

        public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            var dto = (AttributeFilteredNewApi)requestDto;
            dto.AttrsExecuted.Add(GetType().Name);
            dto.PrioritizedDtoRequestFilterExecuted = true;
        }
    }

    //Always executed, before global filters. (applied on service class)
    public class PrioritizedServiceClassFilterTestNewApiAttribute : RequestFilterAttribute
    {
        public PrioritizedServiceClassFilterTestNewApiAttribute()
        {
            Priority = -10;
        }

        public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            var dto = (AttributeFilteredNewApi)requestDto;
            dto.AttrsExecuted.Add(GetType().Name);
        }
    }

    //Always executed, before global filters. (applied on service method)
    public class PrioritizedServiceMethodFilterTestNewApiAttribute : RequestFilterAttribute
    {
        public PrioritizedServiceMethodFilterTestNewApiAttribute()
        {
            Priority = -5;
        }

        public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            var dto = (AttributeFilteredNewApi)requestDto;
            dto.AttrsExecuted.Add(GetType().Name);
        }
    }

    [Route("/attributefilterednewapi")]
    [FilterTestNewApi]
    [ContextualFilterTestNewApi(ApplyTo.Delete | ApplyTo.Put)]
    [PrioritizedDtoFilterTestNewApi]
    public class AttributeFilteredNewApi : IReturn<AttributeFilteredNewApiResponse>
    {
        public AttributeFilteredNewApi()
        {
            this.AttrsExecuted = new List<string>();
        }

        public bool RequestFilterExecuted { get; set; }
        public bool ContextualRequestFilterExecuted { get; set; }
        public bool PrioritizedDtoRequestFilterExecuted { get; set; }
        public bool RequestFilterDependenyIsResolved { get; set; }
        public List<string> AttrsExecuted { get; set; }
    }

    //Always executed
    public class ResponseFilterTestNewApiAttribute : Attribute, IHasResponseFilter
    {
        private static ICacheClient previousCache;

        public ICacheClient Cache { get; set; }

        public int Priority { get; set; }

        public void ResponseFilter(IHttpRequest req, IHttpResponse res, object response)
        {
            var dto = response.ToResponseDto() as AttributeFilteredNewApiResponse;
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
    public class ContextualResponseFilterTestNewApiAttribute : ResponseFilterAttribute
    {
        public ContextualResponseFilterTestNewApiAttribute(ApplyTo applyTo)
            : base(applyTo)
        {
        }

        public override void Execute(IHttpRequest req, IHttpResponse res, object responseDto)
        {
            var dto = responseDto as AttributeFilteredNewApiResponse;
            dto.ContextualResponseFilterExecuted = true;
        }
    }

    public class ThrowingFilterNewApiAttribute : RequestFilterAttribute
    {
        public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            throw new ArgumentException("exception message");
        }
    }

    [Route("/throwingattributefilterednewapi")]
    public class ThrowingAttributeFilteredNewApi : IReturn<string>
    {
    }

    [ThrowingFilterNewApi]
    public class ThrowingAttributeFilteredNewApiService : ServiceInterface.Service, IAny<ThrowingAttributeFilteredNewApi>
    {
        public object Any(ThrowingAttributeFilteredNewApi request)
        {
            return "OK";
        }
    }

    [ResponseFilterTestNewApi]
    [ContextualResponseFilterTestNewApi(ApplyTo.Delete | ApplyTo.Put)]
    public class AttributeFilteredNewApiResponse
    {
        public bool ResponseFilterExecuted { get; set; }
        public bool ContextualResponseFilterExecuted { get; set; }

        public bool RequestFilterExecuted { get; set; }
        public bool ContextualRequestFilterExecuted { get; set; }

        public bool RequestFilterDependenyIsResolved { get; set; }
        public bool ResponseFilterDependencyIsResolved { get; set; }

        public List<string> AttrsExecuted { get; set; }
    }

    [PrioritizedServiceClassFilterTestNewApi]
    public class AttributeFilteredNewApiService : ServiceInterface.Service, IAny<AttributeFilteredNewApi>
    {
        [PrioritizedServiceMethodFilterTestNewApi]
        public object Any(AttributeFilteredNewApi request)
        {
            return new AttributeFilteredNewApiResponse()
            {
                ResponseFilterExecuted = false,
                ContextualResponseFilterExecuted = false,
                RequestFilterExecuted = request.RequestFilterExecuted,
                ContextualRequestFilterExecuted = request.ContextualRequestFilterExecuted,
                RequestFilterDependenyIsResolved = request.RequestFilterDependenyIsResolved,
                ResponseFilterDependencyIsResolved = false,
                AttrsExecuted = request.AttrsExecuted
            };
        }
    }

    [TestFixture]
    public class AttributeFiltersNewApiTest
    {
        private const string GlobalTestFilterId = "GlobalTestFilter";

        private const string ListeningOn = "http://localhost:82/";
        private const string ServiceClientBaseUri = "http://localhost:82/";

        public class AttributeFiltersNewApiAppHostHttpListener
            : AppHostHttpListenerBase
        {

            public AttributeFiltersNewApiAppHostHttpListener()
                : base("Attribute Filters New API Tests", typeof(AttributeFilteredNewApiService).Assembly) { }

            public override void Configure(Funq.Container container)
            {
                RequestFilters.Add(GlobalTestFilter);
                container.Register<ICacheClient>(c => new MemoryCacheClient()).ReusedWithin(Funq.ReuseScope.None);
                SetConfig(new EndpointHostConfig { DebugMode = true }); //show stacktraces
            }

            public static void GlobalTestFilter(IHttpRequest req, IHttpResponse res, object requestDto)
            {
                var testDto = requestDto as AttributeFilteredNewApi;
                if (testDto != null)
                    testDto.AttrsExecuted.Add(GlobalTestFilterId);
            }
        }

        AttributeFiltersNewApiAppHostHttpListener appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new AttributeFiltersNewApiAppHostHttpListener();
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
            var response = client.Send(
                new AttributeFilteredNewApi() { RequestFilterExecuted = false });
            Assert.IsTrue(response.RequestFilterExecuted);
            Assert.IsTrue(response.ResponseFilterExecuted);
            Assert.IsFalse(response.ContextualRequestFilterExecuted);
            Assert.IsFalse(response.ContextualResponseFilterExecuted);
            Assert.IsTrue(response.RequestFilterDependenyIsResolved);
            Assert.IsTrue(response.ResponseFilterDependencyIsResolved);
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Prioritized_Request_Filters_are_executed_in_correct_order_using_ServiceClient(IServiceClient client)
        {
            var response = client.Send(new AttributeFilteredNewApi() { RequestFilterExecuted = false });
            Assert.That(response.AttrsExecuted[0], Is.EqualTo(typeof(PrioritizedDtoFilterTestNewApiAttribute).Name));
            Assert.That(response.AttrsExecuted[1], Is.EqualTo(typeof(PrioritizedServiceClassFilterTestNewApiAttribute).Name));
            Assert.That(response.AttrsExecuted[1], Is.EqualTo(typeof(PrioritizedServiceMethodFilterTestNewApiAttribute).Name));
            Assert.That(response.AttrsExecuted[2], Is.EqualTo(GlobalTestFilterId));
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
                client.Get(new ThrowingAttributeFilteredNewApi());
            }
            catch (WebServiceException e)
            {
                //Ensure we have stack trace present
                Assert.IsTrue(e.ResponseBody.Contains("ThrowingFilterNewApiAttribute"), "No stack trace in the response (it's probably empty)");
            }
        }

        [Test, TestCaseSource("RestClients")]
        public void Request_and_Response_Filters_are_executed_using_RestClient(IRestClient client)
        {
            var response = client.Post<AttributeFilteredNewApiResponse>("attributefilterednewapi", new AttributeFilteredNewApi() { RequestFilterExecuted = false });
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
            var response = client.Delete<AttributeFilteredNewApiResponse>("attributefilterednewapi");
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
            var response = client.Put<AttributeFilteredNewApiResponse>("attributefilterednewapi", new AttributeFilteredNewApi() { RequestFilterExecuted = false });
            Assert.IsTrue(response.RequestFilterExecuted);
            Assert.IsTrue(response.ResponseFilterExecuted);
            Assert.IsTrue(response.ContextualRequestFilterExecuted);
            Assert.IsTrue(response.ContextualResponseFilterExecuted);
            Assert.IsTrue(response.RequestFilterDependenyIsResolved);
            Assert.IsTrue(response.ResponseFilterDependencyIsResolved);
        }

        [Test, TestCaseSource("RestClients")]
        public void Prioritized_Request_Filters_are_executed_in_correct_order_using_RestClient(IRestClient client)
        {
            var response = client.Put<AttributeFilteredNewApiResponse>("attributefilterednewapi", new AttributeFilteredNewApi() { RequestFilterExecuted = false });
            Assert.That(response.AttrsExecuted[0], Is.EqualTo(typeof(PrioritizedDtoFilterTestNewApiAttribute).Name));
            Assert.That(response.AttrsExecuted[1], Is.EqualTo(typeof(PrioritizedServiceClassFilterTestNewApiAttribute).Name));
            Assert.That(response.AttrsExecuted[1], Is.EqualTo(typeof(PrioritizedServiceMethodFilterTestNewApiAttribute).Name));
            Assert.That(response.AttrsExecuted[2], Is.EqualTo(GlobalTestFilterId));
        }

        public class ExecutedFirstNewApiAttribute : RequestFilterAttribute
        {
            public ExecutedFirstNewApiAttribute()
            {
                Priority = int.MinValue;
            }

            public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
            {
                var dto = (AttributeFilteredNewApi)requestDto;
                dto.AttrsExecuted.Add(GetType().Name);
            }
        }

        [ExecutedFirstNewApiAttribute]
        [FilterTestNewApi]
        [RequiredRole("test")]
        [Authenticate]
        [RequiredPermission("test")]
        public class DummyHolderNewApi { }

        [Test]
        public void RequestFilters_are_prioritized()
        {
            EndpointHost.ServiceManager = new ServiceManager(typeof(DummyHolderNewApi).Assembly);

            EndpointHost.ServiceManager.Metadata.Add(typeof(AttributeFilteredNewApiService), typeof(DummyHolderNewApi), null);

            var attributes = FilterAttributeCache.GetRequestFilterAttributes(typeof(DummyHolderNewApi));
            var attrPriorities = attributes.ToList().ConvertAll(x => x.Priority);
            Assert.That(attrPriorities, Is.EquivalentTo(new[] { int.MinValue, -100, -90, -80, -10, 0 }));

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
            Assert.That(execOrderPriorities, Is.EquivalentTo(new[] { int.MinValue, -100, -90, -80, -10, 0 }));
        }
    }
}
