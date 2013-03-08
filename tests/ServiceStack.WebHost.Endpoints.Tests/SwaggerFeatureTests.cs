using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Service;
using ServiceStack.Api.Swagger;
using ServiceStack.ServiceInterface.Cors;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [ServiceHost.Api("Service Description")]
    [Route("/swagger/{Name}", "GET", Summary = @"GET Summary", Notes = "GET Notes")]
    [Route("/swagger/{Name}", "POST", Summary = @"POST Summary", Notes = "POST Notes")]
    public class SwaggerFeatureRequest
    {
        [ApiMember(Name="Name", Description = "Name Description", 
            ParameterType = "path", DataType = "string", IsRequired = true)]
        public string Name { get; set; }
    }

    [ServiceHost.Api]
    [Route("/swagger2/NameIsNotSetRequest", "GET")]
    public class NameIsNotSetRequest
    {
        [ApiMember]
        public string Name { get; set; }
    }


    [ServiceHost.Api("test")]
    [Route("/swg3/conference/count", "GET")]
    public class MultipleTestRequest : IReturn<int>
    {
        [ApiMember]
        public string Name { get; set; }
    }

    [ServiceHost.Api]
    [Route("/swg3/conference/{Name}/conferences", "POST")]
    [Route("/swgb3/conference/{Name}/conferences", "POST")]
    public class MultipleTest2Request : IReturn<object>
    {
        [ApiMember]
        public string Name { get; set; }
    }

    [ServiceHost.Api]
    [Route("/swg3/conference/{Name}/conferences", "DELETE")]
    public class MultipleTest3Request : IReturn<object>
    {
        [ApiMember]
        public string Name { get; set; }
    }

    [ServiceHost.Api]
    [Route("/swg3/conference", "GET")]
    public class MultipleTest4Request : IReturn<object>
    {
        [ApiMember]
        public string Name { get; set; }
    }



    public class SwaggerFeatureResponse
    {
        public bool IsSuccess { get; set; }
    }

    public class MultipleTestRequestService : ServiceInterface.Service
    {
        public object Get(MultipleTestRequest request)
        {
            return null;
        }

        public object Post(MultipleTest2Request request)
        {
            return null;
        }

        public object Delete(MultipleTest3Request request)
        {
            return null;
        }
    }
    public class MultipleTest2RequestService : ServiceInterface.Service
    {
        public object Get(MultipleTest4Request request)
        {
            return null;
        }
    }


    public class SwaggerFeatureService : ServiceInterface.Service
    {
        public object Get(SwaggerFeatureRequest request)
        {
            return new SwaggerFeatureResponse { IsSuccess = true };
        }

        public object Post(SwaggerFeatureRequest request)
        {
            return new SwaggerFeatureResponse { IsSuccess = true };
        }

        public object Get(NameIsNotSetRequest request)
        {
            return 0;
        }
    }


    
    [TestFixture]
    public class SwaggerFeatureServiceTests
    {
        private const string BaseUrl = "http://localhost:8024";
        private const string ListeningOn = BaseUrl + "/";

        public class SwaggerFeatureAppHostHttpListener
            : AppHostHttpListenerBase
        {

            public SwaggerFeatureAppHostHttpListener()
                : base("Swagger Feature Tests", typeof(SwaggerFeatureServiceTests).Assembly) { }

            public override void Configure(Funq.Container container)
            {
                Plugins.Add(new SwaggerFeature());

                SetConfig(new EndpointHostConfig
                {
                    DebugMode = true //Show StackTraces for easier debugging
                });
            }
        }

        SwaggerFeatureAppHostHttpListener appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new SwaggerFeatureAppHostHttpListener();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        static IRestClient[] RestClients = 
        {
            new JsonServiceClient(ListeningOn)
            //new XmlServiceClient(ServiceClientBaseUri),
        };

        [Test, Explicit]
        public void RunFor5Mins()
        {
            appHost.LoadPlugin(new CorsFeature("http://localhost:50000"));

            Debug.WriteLine("ListeningOn: " + ListeningOn);
            Thread.Sleep(TimeSpan.FromMinutes(5));
        }

        [Test, TestCaseSource("RestClients")]
        public void Should_get_default_name_from_property(IRestClient client)
        {
            var resource = client.Get<ResourceResponse>("/resource/swagger2/NameIsNotSetRequest");

            var p = resource.Apis.SelectMany(t => t.Operations).SelectMany(t => t.Parameters);
            Assert.That(p.Count(), Is.EqualTo(1));
            Assert.That(p.FirstOrDefault(t=>t.Name == "Name"), Is.Not.Null);
        }

        [Test, TestCaseSource("RestClients")]
        public void Should_group_similar_services(IRestClient client)
        {
            var resources = client.Get<ResourcesResponse>("/resources");
            resources.PrintDump();

            var swagger = resources.Apis.Where(t => t.Path.Contains("/resource/swg3"));
            Assert.That(swagger.Count(), Is.EqualTo(1));
        }

        [Test, TestCaseSource("RestClients")]
        public void Should_distinct_base_path(IRestClient client)
        {
            var resources = client.Get<ResourcesResponse>("/resources");
            resources.PrintDump();

            var swagger = resources.Apis.Where(t => t.Path.Contains("/resource/swgb3"));
            Assert.That(swagger.Count(), Is.EqualTo(1));
        }

        [Test, TestCaseSource("RestClients")]
        public void Should_list_services(IRestClient client)
        {
            var resources = client.Get<ResourcesResponse>("/resources");
            Assert.That(resources.BasePath, Is.EqualTo(BaseUrl));
            Assert.That(resources.SwaggerVersion, Is.EqualTo("1.1"));
            Assert.That(resources.Apis, Is.Not.Null);

            var swagger = resources.Apis.FirstOrDefault(t => t.Path == "/resource/swagger");
            Assert.That(swagger, Is.Not.Null);
            Assert.That(swagger.Description, Is.EqualTo("Service Description"));
        }

        [Test, TestCaseSource("RestClients")]
        public void Should_restrieve_service_parameters(IRestClient client)
        {
            var resource = client.Get<ResourceResponse>("/resource/swagger");
            Assert.That(resource.BasePath, Is.EqualTo(BaseUrl));
            Assert.That(resource.ResourcePath, Is.EqualTo("/swagger"));
            Assert.That(resource.Apis, Is.Not.Empty);

            resource.Apis.PrintDump();

            var operations = new List<MethodOperation>();
            foreach(var api in resource.Apis) operations.AddRange(api.Operations);

            var getOperation = operations.Single(t => t.HttpMethod == "GET");
            Assert.That(getOperation.Summary, Is.EqualTo("GET Summary"));
            Assert.That(getOperation.Notes, Is.EqualTo("GET Notes"));
            Assert.That(getOperation.HttpMethod, Is.EqualTo("GET"));

            Assert.That(getOperation.Parameters, Is.Not.Empty);
            var p1 = getOperation.Parameters[0];
            Assert.That(p1.Name, Is.EqualTo("Name"));
            Assert.That(p1.Description, Is.EqualTo("Name Description"));
            Assert.That(p1.DataType, Is.EqualTo("string"));
            Assert.That(p1.ParamType, Is.EqualTo("path"));
            Assert.That(p1.Required, Is.EqualTo(true));


            var postOperation = operations.Single(t => t.HttpMethod == "POST");
            Assert.That(postOperation.Summary, Is.EqualTo("POST Summary"));
            Assert.That(postOperation.Notes, Is.EqualTo("POST Notes"));
            Assert.That(postOperation.HttpMethod, Is.EqualTo("POST"));
        }
    }
}
