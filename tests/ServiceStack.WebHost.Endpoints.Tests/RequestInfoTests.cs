using System.Reflection;
using Funq;
using NUnit.Framework;
using ServiceStack.Host.Handlers;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class RequestInfoServices : Service {}
    
    public class RequestInfoTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(RequestInfoTests), typeof(RequestInfoServices).GetAssembly()) {}

            public override void Configure(Container container)
            {
                SetConfig(new HostConfig { DebugMode = true });
            }
        }

        private ServiceStackHost appHost;

        public RequestInfoTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Does_return_expected_request_info()
        {
            var url = Config.ListeningOn.AppendPath("metadata").AddQueryParam("debug", "requestinfo");
            var json = url.GetJsonFromUrl();
            var info = json.FromJson<RequestInfoResponse>();
            
            info.PrintDump();

            Assert.That(info.ServiceName, Is.EqualTo(nameof(RequestInfoTests)));
            Assert.That(info.HttpMethod, Is.EqualTo("GET"));
            Assert.That(info.PathInfo, Is.EqualTo("/metadata"));
            Assert.That(info.RawUrl, Is.EqualTo("/metadata?debug=requestinfo"));
            Assert.That(info.AbsoluteUri, Is.EqualTo("http://localhost:20000/metadata?debug=requestinfo"));
        }
    }
}