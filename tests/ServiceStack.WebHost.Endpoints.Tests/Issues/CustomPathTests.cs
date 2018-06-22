using Funq;
using NUnit.Framework;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests.Issues
{
    [TestFixture]
    public class CustomPathTests
    {
        [Test]
        public void Can_make_CustomPath_Request_without_HandlerPath()
        {
            var apiUrl = Config.ListeningOn + "api/";

            var appHost = new AppHostWithHandlerPath()
                .Init()
                .Start(apiUrl);

            JsonServiceClient serviceClient = new JsonServiceClient(apiUrl);

            var request = new Hello { Name = "ServiceStack" };
            HelloResponse response = serviceClient.Post(request);

            Assert.That(response.Result.Contains(request.Name));

            appHost.Dispose();
        }

        [Test]
        public void Can_make_CustomPath_Request_with_HandlerPath()
        {
            var apiUrl = Config.ListeningOn + "api/";

            var appHost = new AppHostWithoutHandlerPath()
                .Init()
                .Start(apiUrl);

            JsonServiceClient serviceClient = new JsonServiceClient(apiUrl);

            var request = new Hello { Name = "ServiceStack" };
            HelloResponse response = serviceClient.Post(request);

            Assert.That(response.Result.Contains(request.Name));

            appHost.Dispose();
        }
    }

    public class AppHostWithHandlerPath : AppSelfHostBase
    {
        public AppHostWithHandlerPath() 
            : base(nameof(CustomPathTests), typeof(AppHostWithHandlerPath).Assembly) { }

        public override void Configure(Container container)
        {
            SetConfig(new HostConfig
            {
                ApiVersion = "v1",
                WsdlServiceNamespace = "http://schemas.example.com/",
                DebugMode = AppSettings.Get(nameof(HostConfig.DebugMode), true)
            });
        }
    }

    public class AppHostWithoutHandlerPath : AppSelfHostBase
    {
        public AppHostWithoutHandlerPath() 
            : base(nameof(CustomPathTests), typeof(AppHostWithoutHandlerPath).Assembly) { }

        public override void Configure(Container container)
        {
            SetConfig(new HostConfig
            {
                ApiVersion = "v1",
                HandlerFactoryPath = "api", // comment this out and it works
                WsdlServiceNamespace = "http://schemas.example.com/",
                DebugMode = AppSettings.Get(nameof(HostConfig.DebugMode), true)
            });
        }
    }
    
}