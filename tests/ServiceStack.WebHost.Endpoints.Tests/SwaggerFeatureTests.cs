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
            ParameterType = "path", DataType = SwaggerType.String, IsRequired = true)]
        public string Name { get; set; }
    }

    [ServiceHost.Api]
    [Route("/swaggerGetList/{Name}", "GET")]
    public class SwaggerGetListRequest : IReturn<List<SwaggerFeatureResponse>>
    {
        public string Name { get; set; }
    }

    [ServiceHost.Api]
    [Route("/swaggerGetArray/{Name}", "GET")]
    public class SwaggerGetArrayRequest : IReturn<SwaggerFeatureResponse[]>
    {
        public string Name { get; set; }
    }
    
    [ServiceHost.Api]
    [Route("/swaggerModels/{UrlParam}", "POST")]
    public class SwaggerModelsRequest : IReturn<SwaggerFeatureResponse>
    {
        [ApiMember(Name = "UrlParam", Description = "URL parameter",
            ParameterType = "path", DataType = SwaggerType.String, IsRequired = true)]
        public string UrlParam { get; set; }

        [ApiMember(Name = "RequestBody", Description = "The request body",
            ParameterType = "body", DataType = "SwaggerModelsRequest", IsRequired = true)]
        [System.ComponentModel.Description("Name description")]
        public string Name { get; set; }

        [System.ComponentModel.Description("NestedModel description")]
        public SwaggerNestedModel NestedModel { get; set;}

        public List<SwaggerNestedModel2> ListProperty { get; set; }

        public SwaggerNestedModel3[] ArrayProperty { get; set; }

        public byte ByteProperty { get; set; }

        public long LongProperty { get; set; }

        public float FloatProperty { get; set; }

        public double DoubleProperty { get; set; }

        public decimal DecimalProperty { get; set; }

        public DateTime DateProperty { get; set; }
    }

    public class SwaggerNestedModel
    {
        [System.ComponentModel.Description("NestedProperty description")]
        public bool NestedProperty { get; set;}
    }

    public class SwaggerNestedModel2
    {
        [System.ComponentModel.Description("NestedProperty2 description")]
        public bool NestedProperty2 { get; set;}
    }

    public class SwaggerNestedModel3
    {
        [System.ComponentModel.Description("NestedProperty3 description")]
        public bool NestedProperty3 { get; set;}
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

	public class NullableResponse
	{
		[System.ComponentModel.Description("NestedProperty2 description")]
		public bool NestedProperty2 { get; set; }

		public int? Optional { get; set; }
	}

	[ServiceHost.Api]
	[Route("/swgnull/", "GET")]
	public class NullableInRequest : IReturn<NullableResponse>
	{
		[ApiMember]
		public int? Position { get; set; }
	}
	
	public class NullableService : ServiceInterface.Service
	{
		public object Get(NullableInRequest request)
		{
			return null;
		}
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

        public object Post(SwaggerModelsRequest request)
        {
            return new SwaggerFeatureResponse { IsSuccess = true };
        }

        public object Get(SwaggerGetListRequest request)
        {
            return new List<SwaggerFeatureResponse> { new SwaggerFeatureResponse { IsSuccess = true } };
        }

        public object Get(SwaggerGetArrayRequest request)
        {
            return new[] { new SwaggerFeatureResponse { IsSuccess = true } };
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
            appHost.LoadPlugin(new CorsFeature("http://localhost:50001"));

            Debug.WriteLine(ListeningOn + "resources");
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
            Assert.That(resources.SwaggerVersion, Is.EqualTo("1.2"));
            Assert.That(resources.Apis, Is.Not.Null);

            var swagger = resources.Apis.FirstOrDefault(t => t.Path == "/resource/swagger");
            Assert.That(swagger, Is.Not.Null);
            Assert.That(swagger.Description, Is.EqualTo("Service Description"));
        }

        [Test, TestCaseSource("RestClients")]
        public void Should_use_webhosturl_as_resources_base_path_when_configured(IRestClient client)
        {
            const string webHostUrl = "https://host.example.com/_api";
            try
            {
                appHost.Config.WebHostUrl = webHostUrl;

                var resources = client.Get<ResourcesResponse>("/resources");
                resources.PrintDump();

                Assert.That(resources.BasePath, Is.EqualTo(webHostUrl));
            }
            finally
            {
                appHost.Config.WebHostUrl = null;
            }
        }

        [Test, TestCaseSource("RestClients")]
        public void Should_use_webhosturl_as_resource_base_path_when_configured(IRestClient client)
        {
            const string webHostUrl = "https://host.example.com/_api";
            try
            {
                appHost.Config.WebHostUrl = webHostUrl;

                var resource = client.Get<ResourceResponse>("/resource/swagger");
                resource.PrintDump();

                Assert.That(resource.BasePath, Is.EqualTo(webHostUrl));
            }
            finally
            {
                appHost.Config.WebHostUrl = null;
            }
        }

        [Test, TestCaseSource("RestClients")]
        public void Should_use_https_for_resources_basepath_when_usehttpslinks_config_is_true(IRestClient client)
        {
            try
            {
                appHost.Config.UseHttpsLinks = true;

                var resources = client.Get<ResourcesResponse>("/resources");
                resources.PrintDump();

                Assert.That(resources.BasePath.ToLowerInvariant(), Is.StringStarting("https"));
            }
            finally
            {
                appHost.Config.UseHttpsLinks = false;
            }
        }

        [Test, TestCaseSource("RestClients")]
        public void Should_use_https_for_resource_basepath_when_usehttpslinks_config_is_true(IRestClient client)
        {
            try
            {
                appHost.Config.UseHttpsLinks = true;

                var resource = client.Get<ResourceResponse>("/resource/swagger");
                resource.PrintDump();

                Assert.That(resource.BasePath.ToLowerInvariant(), Is.StringStarting("https"));
            }
            finally
            {
                appHost.Config.UseHttpsLinks = false;
            }
        }

        [Test, TestCaseSource("RestClients")]
        public void Should_retrieve_service_parameters(IRestClient client)
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

        [Test, TestCaseSource("RestClients")]
        public void Should_retrieve_response_class_name(IRestClient client)
        {
            var resource = client.Get<ResourceResponse>("/resource/swaggerModels");
            Assert.That(resource.Apis, Is.Not.Empty);

            var postOperation = resource.Apis.SelectMany(api => api.Operations).Single(t => t.HttpMethod == "POST");
            postOperation.PrintDump();
            Assert.That(postOperation.ResponseClass, Is.EqualTo(typeof(SwaggerFeatureResponse).Name));
        }

        [Test, TestCaseSource("RestClients")]
        public void Should_retrieve_list_response_type_info(IRestClient client)
        {
            var resource = client.Get<ResourceResponse>("/resource/swaggerGetList");
            Assert.That(resource.Apis, Is.Not.Empty);

            var operation = resource.Apis.SelectMany(api => api.Operations).Single(t => t.HttpMethod == "GET");
            operation.PrintDump();
            Assert.That(operation.ResponseClass, Is.EqualTo("List[SwaggerFeatureResponse]"));
            Assert.That(resource.Models.ContainsKey("SwaggerFeatureResponse"));
        }

        [Test, TestCaseSource("RestClients")]
        public void Should_retrieve_array_response_type_info(IRestClient client)
        {
            var resource = client.Get<ResourceResponse>("/resource/swaggerGetArray");
            Assert.That(resource.Apis, Is.Not.Empty);

            var operation = resource.Apis.SelectMany(api => api.Operations).Single(t => t.HttpMethod == "GET");
            operation.PrintDump();
            Assert.That(operation.ResponseClass, Is.EqualTo("List[SwaggerFeatureResponse]"));
            Assert.That(resource.Models.ContainsKey("SwaggerFeatureResponse"));
        }

        [Test, TestCaseSource("RestClients")]
        public void Should_retrieve_response_model(IRestClient client)
        {
            var resource = client.Get<ResourceResponse>("/resource/swaggerModels");
            Assert.That(resource.Models, Is.Not.Empty);

            Assert.That(resource.Models.ContainsKey(typeof(SwaggerFeatureResponse).Name), Is.True);
            var responseClassModel = resource.Models[typeof(SwaggerFeatureResponse).Name];
            responseClassModel.PrintDump();

            Assert.That(responseClassModel.Id, Is.EqualTo(typeof(SwaggerFeatureResponse).Name));
            Assert.That(responseClassModel.Properties, Is.Not.Empty);
            Assert.That(responseClassModel.Properties.ContainsKey("IsSuccess"), Is.True);
            Assert.That(responseClassModel.Properties["IsSuccess"].Type, Is.EqualTo(SwaggerType.Boolean));
        }

        [Test, TestCaseSource("RestClients")]
        public void Should_retrieve_request_body_model(IRestClient client)
        {
            var resource = client.Get<ResourceResponse>("/resource/swaggerModels");
            Assert.That(resource.Models, Is.Not.Empty);
            resource.Models.PrintDump();

            Assert.That(resource.Models.ContainsKey(typeof(SwaggerModelsRequest).Name), Is.True);
            var requestClassModel = resource.Models[typeof(SwaggerModelsRequest).Name];

            Assert.That(requestClassModel.Id, Is.EqualTo(typeof(SwaggerModelsRequest).Name));
            Assert.That(requestClassModel.Properties, Is.Not.Empty);

            Assert.That(requestClassModel.Properties.ContainsKey("UrlParam"), Is.False);

            Assert.That(requestClassModel.Properties.ContainsKey("Name"), Is.True);
            Assert.That(requestClassModel.Properties["Name"].Type, Is.EqualTo(SwaggerType.String));
            Assert.That(requestClassModel.Properties["Name"].Description, Is.EqualTo("Name description"));

            Assert.That(requestClassModel.Properties.ContainsKey("ByteProperty"));
            Assert.That(requestClassModel.Properties["ByteProperty"].Type, Is.EqualTo(SwaggerType.Byte));
            Assert.That(resource.Models.ContainsKey(typeof(byte).Name), Is.False);

            Assert.That(requestClassModel.Properties.ContainsKey("LongProperty"));
            Assert.That(requestClassModel.Properties["LongProperty"].Type, Is.EqualTo(SwaggerType.Long));
            Assert.That(resource.Models.ContainsKey(typeof(long).Name), Is.False);

            Assert.That(requestClassModel.Properties.ContainsKey("FloatProperty"));
            Assert.That(requestClassModel.Properties["FloatProperty"].Type, Is.EqualTo(SwaggerType.Float));
            Assert.That(resource.Models.ContainsKey(typeof(float).Name), Is.False);

            Assert.That(requestClassModel.Properties.ContainsKey("DoubleProperty"));
            Assert.That(requestClassModel.Properties["DoubleProperty"].Type, Is.EqualTo(SwaggerType.Double));
            Assert.That(resource.Models.ContainsKey(typeof(double).Name), Is.False);

            // Swagger has no concept of a "decimal" type
            Assert.That(requestClassModel.Properties.ContainsKey("DecimalProperty"));
            Assert.That(requestClassModel.Properties["DecimalProperty"].Type, Is.EqualTo(SwaggerType.Double));
            Assert.That(resource.Models.ContainsKey(typeof(decimal).Name), Is.False);

            Assert.That(requestClassModel.Properties.ContainsKey("DateProperty"));
            Assert.That(requestClassModel.Properties["DateProperty"].Type, Is.EqualTo(SwaggerType.Date));
            Assert.That(resource.Models.ContainsKey(typeof(DateTime).Name), Is.False);

            Assert.That(requestClassModel.Properties.ContainsKey("NestedModel"), Is.True);
            Assert.That(requestClassModel.Properties["NestedModel"].Type, Is.EqualTo("SwaggerNestedModel"));
            Assert.That(requestClassModel.Properties["NestedModel"].Description, Is.EqualTo("NestedModel description"));

            Assert.That(resource.Models.ContainsKey(typeof(SwaggerNestedModel).Name), Is.True);
            var nestedClassModel = resource.Models[typeof(SwaggerNestedModel).Name];

            Assert.That(nestedClassModel.Properties.ContainsKey("NestedProperty"), Is.True);
            Assert.That(nestedClassModel.Properties["NestedProperty"].Type, Is.EqualTo(SwaggerType.Boolean));
            Assert.That(nestedClassModel.Properties["NestedProperty"].Description, Is.EqualTo("NestedProperty description"));
        }

        [Test, TestCaseSource("RestClients")]
        public void Should_retrieve_list_property_model(IRestClient client)
        {
            var resource = client.Get<ResourceResponse>("/resource/swaggerModels");
            Assert.That(resource.Models.ContainsKey(typeof(SwaggerModelsRequest).Name), Is.True);
            var requestClassModel = resource.Models[typeof(SwaggerModelsRequest).Name];

            Assert.That(requestClassModel.Properties.ContainsKey("ListProperty"), Is.True);
            Assert.That(requestClassModel.Properties["ListProperty"].Type, Is.EqualTo(SwaggerType.Array));
            Assert.That(requestClassModel.Properties["ListProperty"].Items["$ref"], Is.EqualTo(typeof(SwaggerNestedModel2).Name));
            Assert.That(resource.Models.ContainsKey(typeof(SwaggerNestedModel2).Name), Is.True);
        }

        [Test, TestCaseSource("RestClients")]
        public void Should_retrieve_array_property_model(IRestClient client)
        {
            var resource = client.Get<ResourceResponse>("/resource/swaggerModels");
            Assert.That(resource.Models.ContainsKey(typeof(SwaggerModelsRequest).Name), Is.True);
            var requestClassModel = resource.Models[typeof(SwaggerModelsRequest).Name];

            Assert.That(requestClassModel.Properties.ContainsKey("ArrayProperty"), Is.True);
            Assert.That(requestClassModel.Properties["ArrayProperty"].Type, Is.EqualTo(SwaggerType.Array));
            Assert.That(requestClassModel.Properties["ArrayProperty"].Items["$ref"], Is.EqualTo(typeof(SwaggerNestedModel3).Name));
            Assert.That(resource.Models.ContainsKey(typeof(SwaggerNestedModel3).Name), Is.True);
        }

		[Test, TestCaseSource("RestClients")]
		public void Should_retrieve_valid_nullable_fields(IRestClient client)
		{
			var resource = client.Get<ResourceResponse>("/resource/swgnull");
			Assert.That(resource.Models.ContainsKey(typeof(NullableInRequest).Name), Is.True);
			var requestClassModel = resource.Models[typeof(NullableInRequest).Name];

			Assert.That(requestClassModel.Properties.ContainsKey("Position"), Is.True);
			Assert.That(requestClassModel.Properties["Position"].Type, Is.EqualTo(SwaggerType.Int));
			Assert.That(resource.Models.ContainsKey(typeof(NullableResponse).Name), Is.True);

			var responseModel = resource.Models[typeof (NullableResponse).Name];
			Assert.That(responseModel.Properties.ContainsKey("Optional"), Is.True);
			Assert.That(responseModel.Properties["Optional"].Required, Is.False);
			Assert.That(responseModel.Properties["Optional"].Type, Is.EqualTo(SwaggerType.Int));
			Assert.That(responseModel.Properties["NestedProperty2"].Required, Is.True);
		}
    }
}
