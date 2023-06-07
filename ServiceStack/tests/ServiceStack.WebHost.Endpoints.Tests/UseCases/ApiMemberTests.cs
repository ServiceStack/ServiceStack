using System.Linq;
using System.Runtime.Serialization;
using Funq;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests.UseCases;

[TestFixture]
public class ApiMemberTests
{
    private readonly ServiceStackHost appHost;
    
    class ApiMemberHello : IReturn<ApiMemberHelloResponse>
    {
        public string Name { get; set; }
    }

    class ApiMemberHelloResponse
    {
        public string Result { get; set; }
    }

    class MyServices : Service
    {
        public object Any(ApiMemberHello request)
        {
            return new ApiMemberHelloResponse() { Result = "" };
        }
    }

    class AppHost : AppSelfHostBase
    {
        public AppHost()
            : base(nameof(ApiMemberTests), typeof(MyServices))
        {
        }
        public override void Configure(Container container)
        {
            typeof(ApiMemberHello)
                .GetProperty("Name")
                .AddAttributes(
                    new ApiMemberAttribute
                    {
                        ParameterType = "path",
                        Name = "MetaName",
                        DataType = "foo",
                        Description = "The name of the person to say hello to.",
                        IsRequired = true,
                    },
                    new DataMemberAttribute());
        }
    }
    
    public ApiMemberTests()
    {
        appHost = new AppHost()
            .Init()
            .Start(Config.AbsoluteBaseUri);
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown()
    {
        appHost.Dispose();
    }

    [Test]
    public void ApiMemberDescriptionPopulated()
    {
        var client = new JsonServiceClient(Config.AbsoluteBaseUri);
        var metadata = client.Get(new MetadataApp());
        
        Assert.That(metadata, Is.Not.Null);
        Assert.That(metadata.Api, Is.Not.Null);
        Assert.That(metadata.Api.Operations, Is.Not.Null);
        Assert.That(metadata.Api.Operations.Count, Is.EqualTo(1));
        Assert.That(metadata.Api.Operations[0].Request, Is.Not.Null);
        Assert.That(metadata.Api.Operations[0].Request.Name, Is.EqualTo("ApiMemberHello"));
        var props = metadata.Api.Operations[0].Request.Properties;
        Assert.That(props, Is.Not.Null);
        Assert.That(props.Any(x => x is { Name: "Name" }));
        var nameDesc = props.Find(x => x is { Name: "Name" })?.Description;
        Assert.That(nameDesc, Is.EqualTo("The name of the person to say hello to."));
    }
}