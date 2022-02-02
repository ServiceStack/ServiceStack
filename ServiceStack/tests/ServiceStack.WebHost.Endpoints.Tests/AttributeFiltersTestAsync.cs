using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.Caching;
using ServiceStack.Common.Tests.ServiceClient.Web;
using NUnit.Framework;
using ServiceStack.Support.WebHost;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{

    //Always executed
    public class FilterTestAsyncAttribute : AttributeBase, IHasRequestFilterAsync
    {
        private static ICacheClient previousCache;

        public ICacheClient Cache { get; set; }

        public int Priority { get; set; }

        public Task RequestFilterAsync(IRequest req, IResponse res, object requestDto)
        {
            var dto = (AttributeFilteredAsync)requestDto;
            dto.AttrsExecuted.Add(GetType().Name);
            dto.RequestFilterExecuted = true;

            //Check for equality to previous cache to ensure a filter attribute is no singleton
            dto.RequestFilterDependenyIsResolved = Cache != null && !Cache.Equals(previousCache);

            previousCache = Cache;
            return TypeConstants.EmptyTask;
        }

        public IRequestFilterBase Copy() => (IRequestFilterBase)this.MemberwiseClone();
    }

    //Only executed for the provided HTTP methods (GET, POST) 
    public class ContextualFilterTestAsyncAttribute : RequestFilterAsyncAttribute
    {
        public ContextualFilterTestAsyncAttribute(ApplyTo applyTo)
            : base(applyTo) {}

        public override Task ExecuteAsync(IRequest req, IResponse res, object requestDto)
        {
            var dto = (AttributeFilteredAsync)requestDto;
            dto.AttrsExecuted.Add(GetType().Name);
            dto.ContextualRequestFilterExecuted = true;
            return TypeConstants.EmptyTask;
        }
    }

    [Route("/attributefiltered-async")]
    [FilterTestAsync]
    [ContextualFilterTestAsync(ApplyTo.Delete | ApplyTo.Put)]
    public class AttributeFilteredAsync
    {
        public AttributeFilteredAsync()
        {
            this.AttrsExecuted = new List<string>();
        }

        public bool RequestFilterExecuted { get; set; }
        public bool ContextualRequestFilterExecuted { get; set; }
        public bool InheritedRequestFilterExecuted { get; set; }
        public bool RequestFilterDependenyIsResolved { get; set; }
        public List<string> AttrsExecuted { get; set; }
    }

    //Always executed
    public class ResponseFilterTestAsyncAttribute : AttributeBase, IHasResponseFilterAsync
    {
        private static ICacheClient previousCache;

        public ICacheClient Cache { get; set; }

        public int Priority { get; set; }

        public Task ResponseFilterAsync(IRequest req, IResponse res, object response)
        {
            var dto = response.GetResponseDto() as AttributeFilteredAsyncResponse;
            dto.ResponseFilterExecuted = true;
            dto.ResponseFilterDependencyIsResolved = Cache != null && !Cache.Equals(previousCache);

            previousCache = Cache;
            return TypeConstants.EmptyTask;
        }

        public IResponseFilterBase Copy() => (IResponseFilterBase)this.MemberwiseClone();
    }

    //Only executed for the provided HTTP methods (GET, POST) 
    public class ContextualResponseFilterTestAsyncAttribute : ResponseFilterAsyncAttribute
    {
        public ContextualResponseFilterTestAsyncAttribute(ApplyTo applyTo)
            : base(applyTo)
        {
        }

        public override Task ExecuteAsync(IRequest req, IResponse res, object responseDto)
        {
            var dto = responseDto as AttributeFilteredAsyncResponse;
            dto.ContextualResponseFilterExecuted = true;
            return TypeConstants.EmptyTask;
        }
    }

    public class ThrowingFilterAsyncAttribute : RequestFilterAsyncAttribute
    {
        public override Task ExecuteAsync(IRequest req, IResponse res, object requestDto)
        {
            throw new ArgumentException("exception message");
        }
    }

    [Route("/throwingattributefiltered-async")]
    public class ThrowingAttributeFilteredAsync : IReturn<string>
    {
    }

    [ThrowingFilter]
    public class ThrowingAttributeFilteredAsyncService : IService
    {
        public object Any(ThrowingAttributeFilteredAsync request)
        {
            return "OK";
        }
    }

    public class AttributeFilteredAsyncResponse
    {
        public bool RequestFilterExecuted { get; set; }
        public bool InheritedRequestFilterExecuted { get; set; }
        public bool ContextualRequestFilterExecuted { get; set; }

        public bool ResponseFilterExecuted { get; set; }
        public bool ContextualResponseFilterExecuted { get; set; }
        public bool InheritedResponseFilterExecuted { get; set; }

        public bool RequestFilterDependenyIsResolved { get; set; }
        public bool ResponseFilterDependencyIsResolved { get; set; }

    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class PriorityAsyncAttribute : RequestFilterAsyncAttribute
    {
        public string Name { get; set; }

        public PriorityAsyncAttribute(int priority, string name)
        {
            Priority = priority;
            Name = name;
        }

        public override Task ExecuteAsync(IRequest req, IResponse res, object requestDto)
        {
            var dto = (PriorityAttributeTestAsync)requestDto;
            dto.Names.Add(Name);
            return TypeConstants.EmptyTask;
        }
    }

    public class PriorityAttributeTestAsync : IReturn<PriorityAttributeTestAsync>
    {
        public PriorityAttributeTestAsync()
        {
            this.Names = new List<string>();
        }

        public List<string> Names { get; set; }
    }

    public class PriorityAttributeAsyncService : Service
    {
        [PriorityAsync(3, "3rd")]
        [PriorityAsync(2, "2nd")]
        [PriorityAsync(1, "1st")]
        public object Any(PriorityAttributeTestAsync request)
        {
            return request;
        }
    }

    [InheritedRequestFilterAsync]
    [InheritedResponseFilterAsync]
    public class AttributeFilteredServiceAsyncBase : IService { }

    public class AttributeFilteredAsyncService : AttributeFilteredServiceAsyncBase
    {
        [ResponseFilterTestAsync]
        [ContextualResponseFilterTestAsync(ApplyTo.Delete | ApplyTo.Put)]
        public object Any(AttributeFilteredAsync request)
        {
            return new AttributeFilteredAsyncResponse
            {
                ResponseFilterExecuted = false,
                ContextualResponseFilterExecuted = false,
                RequestFilterExecuted = request.RequestFilterExecuted,
                InheritedRequestFilterExecuted = request.InheritedRequestFilterExecuted,
                ContextualRequestFilterExecuted = request.ContextualRequestFilterExecuted,
                RequestFilterDependenyIsResolved = request.RequestFilterDependenyIsResolved,
                ResponseFilterDependencyIsResolved = false
            };
        }
    }

    public class InheritedRequestFilterAsyncAttribute : RequestFilterAsyncAttribute
    {
        public override Task ExecuteAsync(IRequest req, IResponse res, object requestDto)
        {
            var dto = (AttributeFilteredAsync)requestDto;
            dto.InheritedRequestFilterExecuted = true;
            return TypeConstants.EmptyTask;
        }
    }

    public class InheritedResponseFilterAsyncAttribute : ResponseFilterAsyncAttribute
    {
        public override Task ExecuteAsync(IRequest req, IResponse res, object responseDto)
        {
            var dto = (AttributeFilteredAsyncResponse)responseDto;
            dto.InheritedResponseFilterExecuted = true;
            return TypeConstants.EmptyTask;
        }
    }

    [TestFixture]
    public class AttributeFiltersAsyncTest
    {
        private const string ListeningOn = "http://localhost:1337/";
        private const string ServiceClientBaseUri = "http://localhost:1337/";

        public class AppHost
            : AppHostHttpListenerBase
        {

            public AppHost()
                : base("Attribute Filters Tests", typeof(AttributeAttributeFilteredService).Assembly) {}

            public override void Configure(Funq.Container container)
            {
                container.Register<ICacheClient>(c => new MemoryCacheClient()).ReusedWithin(Funq.ReuseScope.None);
                SetConfig(new HostConfig { DebugMode = true }); //show stacktraces
            }
        }

        AppHost appHost;

        [OneTimeSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new AppHost();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [OneTimeTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        public IServiceClient GetServiceClient()
        {
            return new JsonServiceClient(ServiceClientBaseUri);
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
            var response = client.Send<AttributeFilteredAsyncResponse>(
                new AttributeFilteredAsync { RequestFilterExecuted = false });
            Assert.IsTrue(response.RequestFilterExecuted);
            Assert.IsTrue(response.ResponseFilterExecuted);
            Assert.IsTrue(response.InheritedRequestFilterExecuted);
            Assert.IsTrue(response.InheritedResponseFilterExecuted);
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
                client.Get(new ThrowingAttributeFilteredAsync());
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
            var response = client.Post<AttributeFilteredAsyncResponse>("attributefiltered-async", new AttributeFilteredAsync { RequestFilterExecuted = false });
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
            var response = client.Delete<AttributeFilteredAsyncResponse>("attributefiltered-async");
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
            var response = client.Put<AttributeFilteredAsyncResponse>("attributefiltered-async", new AttributeFilteredAsync { RequestFilterExecuted = false });
            Assert.IsTrue(response.RequestFilterExecuted);
            Assert.IsTrue(response.ResponseFilterExecuted);
            Assert.IsTrue(response.ContextualRequestFilterExecuted);
            Assert.IsTrue(response.ContextualResponseFilterExecuted);
            Assert.IsTrue(response.RequestFilterDependenyIsResolved);
            Assert.IsTrue(response.ResponseFilterDependencyIsResolved);
        }

        [Test]
        public void Does_order_action_attributes_by_priority()
        {
            var client = GetServiceClient();

            var response = client.Post(new PriorityAttributeTestAsync());

            response.PrintDump();

            Assert.That(response.Names, Is.EqualTo(new[] { "1st", "2nd", "3rd" }));
        }

        public class ExecutedFirstAsyncAttribute : RequestFilterAsyncAttribute
        {
            public ExecutedFirstAsyncAttribute()
            {
                Priority = int.MinValue;
            }
 
            public override Task ExecuteAsync(IRequest req, IResponse res, object requestDto)
            {
                var dto = (AttributeFilteredAsync)requestDto;
                dto.AttrsExecuted.Add(GetType().Name);
                return TypeConstants.EmptyTask;
            }
        }

        [ExecutedFirstAsync]
        [FilterTestAsync]
        [RequiredRole("test")]
        [Authenticate]
        [RequiredPermission("test")]
        public class DummyHolderAsync { }

        [Test]
        public void RequestFilters_are_prioritized()
        {
            appHost.Metadata.Add(typeof(AttributeFilteredAsyncService), typeof(DummyHolderAsync), null);

            var attributes = FilterAttributeCache.GetRequestFilterAttributes(typeof(DummyHolderAsync));
            var attrPriorities = attributes.ToList().ConvertAll(x => x.Priority);
            Assert.That(attrPriorities, Is.EquivalentTo(new[] { int.MinValue, -100, -90, -80, 0, 0 }));

            var execOrder = new IRequestFilterBase[attributes.Length];
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
            Assert.That(execOrderPriorities, Is.EquivalentTo(new[] { int.MinValue, -100, -90, -80, 0, 0 }));
        }
    }
}
