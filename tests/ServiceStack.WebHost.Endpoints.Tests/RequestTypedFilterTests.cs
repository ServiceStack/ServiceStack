using Funq;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/tenant/{TenantName}/resourceType1")]
    public class ResourceType1 : IReturn<ResourceType1>
    {
        public string TenantName { get; set; }

        public string SubResourceName { get; set; }

        public string Arg1 { get; set; }
    }

    [Route("/tenant/{TenantName}/resourceType2")]
    public class ResourceType2 : IReturn<ResourceType2>
    {
        public string TenantName { get; set; }

        public string SubResourceName { get; set; }

        public string Arg1 { get; set; }
    }

    public class TypedFilterService : Service
    {
        public object Any(ResourceType1 request)
        {
            return request;
        }

        public object Any(ResourceType2 request)
        {
            return request;
        }
    }

    [TestFixture]
    public class RequestTypedFilterTests
    {
        public class TypedFilterAppHost : AppSelfHostBase
        {
            public TypedFilterAppHost() 
                : base("Typed Filters", typeof(TypedFilterService).Assembly)
            {
            }

            public override void Configure(Container container)
            {
                RegisterTypedRequestFilter<ResourceType1>((req, res, dto) =>
                {
                    var route = req.GetRoute();
                    if (route != null && route.Path == "/tenant/{TenantName}/resourceType1")
                    {
                        dto.SubResourceName = "CustomResource";
                    }
                });
            }
        }

        ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new TypedFilterAppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_modify_requestDto_with_TypedRequestFilter()
        {
            var client = new JsonServiceClient(Config.ListeningOn);
            var response = client.Get(new ResourceType1
            {
                Arg1 = "arg1",
                TenantName = "tennant"
            });

            Assert.That(response.Arg1, Is.EqualTo("arg1"));
            Assert.That(response.TenantName, Is.EqualTo("tennant"));
            Assert.That(response.SubResourceName, Is.EqualTo("CustomResource"));
        }
    }
}