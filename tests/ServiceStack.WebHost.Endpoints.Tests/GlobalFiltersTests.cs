using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public interface ITestFilters
    {
        bool GlobalRequestFilter { get; set; }
        bool ServiceRequestAttributeFilter { get; set; }
        bool ActionRequestAttributeFilter { get; set; }
        bool Service { get; set; }
        bool ActionResponseAttributeFilter { get; set; }
        bool ServiceResponseAttributeFilter { get; set; }
        bool GlobalResponseFilter { get; set; }
    }

    public class TestFiltersSync : IReturn<TestFiltersSync>, ITestFilters
    {
        public bool GlobalRequestFilter { get; set; }
        public bool ServiceRequestAttributeFilter { get; set; }
        public bool ActionRequestAttributeFilter { get; set; }
        public bool Service { get; set; }
        public bool ActionResponseAttributeFilter { get; set; }
        public bool ServiceResponseAttributeFilter { get; set; }
        public bool GlobalResponseFilter { get; set; }
    }

    public class TestFiltersAsync : IReturn<TestFiltersAsync>, ITestFilters
    {
        public bool GlobalRequestFilter { get; set; }
        public bool ServiceRequestAttributeFilter { get; set; }
        public bool ActionRequestAttributeFilter { get; set; }
        public bool Service { get; set; }
        public bool ActionResponseAttributeFilter { get; set; }
        public bool ServiceResponseAttributeFilter { get; set; }
        public bool GlobalResponseFilter { get; set; }
    }

    public class ServiceRequestFilterAttribute : RequestFilterAttribute
    {
        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            ((ITestFilters)requestDto).ServiceRequestAttributeFilter = true;
        }
    }

    public class ActionRequestFilterAttribute : RequestFilterAttribute
    {
        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            ((ITestFilters)requestDto).ActionRequestAttributeFilter = true;
        }
    }

    public class ServiceResponseFilterAttribute : ResponseFilterAttribute
    {
        public override void Execute(IRequest req, IResponse res, object responseDto)
        {
            ((ITestFilters)responseDto).ServiceResponseAttributeFilter = true;
        }
    }

    public class ActionResponseFilterAttribute : ResponseFilterAttribute
    {
        public override void Execute(IRequest req, IResponse res, object responseDto)
        {
            ((ITestFilters)responseDto).ActionResponseAttributeFilter = true;
        }
    }

    [ServiceRequestFilter]
    [ServiceResponseFilter]
    public class GlobalFiltersService : Service
    {
        [ActionRequestFilter]
        [ActionResponseFilter]
        public object Any(TestFiltersSync request)
        {
            request.Service = true;

            return request;
        }

        [ActionRequestFilter]
        [ActionResponseFilter]
        public async Task<TestFiltersAsync> Any(TestFiltersAsync request)
        {
            return await Task.Factory.StartNew(() =>
            {
                request.Service = true;
                return request;
            });
        }
    }

    public class GlobalFiltersAppHost : AppHostHttpListenerBase
    {
        public GlobalFiltersAppHost() : base(typeof(BufferedRequestTests).Name, typeof(MyService).Assembly) { }

        public override void Configure(Container container)
        {
            this.GlobalRequestFilters.Add((req, res, dto) => ((ITestFilters)dto).GlobalRequestFilter = true);
            this.GlobalResponseFilters.Add((req, res, dto) => ((ITestFilters)dto).GlobalResponseFilter = true);
        }
    }

    [TestFixture]
    public class GlobalFiltersTests
    {
        private ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new GlobalFiltersAppHost()
                .Init()
                .Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        public bool AllFieldsTrue(ITestFilters dto)
        {
            if (!dto.GlobalRequestFilter)
                return false;
            if (!dto.ServiceRequestAttributeFilter)
                return false;
            if (!dto.ActionRequestAttributeFilter)
                return false;
            if (!dto.Service)
                return false;
            if (!dto.ActionResponseAttributeFilter)
                return false;
            if (!dto.ServiceResponseAttributeFilter)
                return false;
            if (!dto.GlobalResponseFilter)
                return false;

            return true;
        }

        [Test]
        public void Does_fire_all_filters_sync()
        {
            var client = new JsonServiceClient(Config.ServiceStackBaseUri);

            var response = client.Get(new TestFiltersSync());

            Assert.That(AllFieldsTrue(response), Is.True);
        }

        [Test]
        public async Task Does_fire_all_filters_async()
        {
            var client = new JsonServiceClient(Config.ServiceStackBaseUri);

            var response = await client.GetAsync(new TestFiltersAsync());

            Assert.That(AllFieldsTrue(response), Is.True);
        }
         
    }
}