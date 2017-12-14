using System.Collections.Generic;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class SomeResponse
    {
        public string Info { get; set; }
    }

    public class InternalResponse : SomeResponse
    {
    }

    public class RequestSync : IReturn<SomeResponse> { }
    public class RequestAsync : IReturn<SomeResponse> { }
    public class RequestInternal: IGet, IReturn<InternalResponse> { }

    public class FooBarService: Service
    {
        public SomeResponse Get(RequestSync request)
        {
            var resp = Gateway.Send(new RequestInternal());
            return new SomeResponse() {Info = resp.Info};
        }

        public async Task<SomeResponse> Get(RequestAsync req)
        {
            var resp = await Gateway.SendAsync(new RequestInternal());
            return new SomeResponse() {Info = resp.Info};
        }

        public Task<InternalResponse> Get(RequestInternal req) => Task.FromResult(new InternalResponse() {Info = "yay"});
    }

    public class InProcessServiceGatewayRequestResponseFiltersTests
    {
        class InProcessAppHost : AppSelfHostBase
        {
            public InProcessAppHost() : base(typeof(InProcessServiceGatewayRequestResponseFiltersTests).Name, 
                typeof(ServiceGatewayServices).Assembly) { }

            public override void Configure(Container container)
            {                
            }
        }

        private readonly ServiceStackHost _appHost;        
        private readonly List<string> _filterCallLog = new List<string>();
        private readonly JsonServiceClient _client;

        public InProcessServiceGatewayRequestResponseFiltersTests()
        {
            _appHost = new InProcessAppHost();
            _appHost.GlobalRequestFilters.Add((req ,resp, dto) => _filterCallLog.Add(req.PathInfo));
            _appHost.GlobalResponseFilters.Add((req, resp, dto) => _filterCallLog.Add(dto.GetType().Name));

            _appHost.Init()
                .Start(Config.ListeningOn);
            _client = new JsonServiceClient(Config.ListeningOn);
        }

        [TearDown]
        public void CleanAfterTest()
        {
            _filterCallLog.Clear();
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            _appHost.Dispose();
        }

        [Test]
        public void Should_Not_Call_Filters_When_Using_SyncGateway()
        {
            var result = _client.Get(new RequestSync());
            Assert.AreEqual("yay", result.Info);            
            CollectionAssert.AreEqual(new []{ "/json/reply/RequestSync", "SomeResponse" }, _filterCallLog);
        }

        [Test]
        public void Should_Not_Call_Filters_When_Using_AsyncGateway()
        {
            var result = _client.Get(new RequestAsync());
            Assert.AreEqual("yay", result.Info);
            CollectionAssert.AreEqual(new[] { "/json/reply/RequestAsync", "SomeResponse" }, _filterCallLog);
        }
    }
}