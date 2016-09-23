using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Core.SelfHostTests
{
    public class Hello : IReturn<HelloResponse>
    {
        public string Name { get; set; }
    }

    public class HelloResponse
    {
        public string Result { get; set; }
    }

    public class BasicHostServices : Service
    {
        public object Any(Hello request) =>
            new HelloResponse { Result = $"Hello, {request.Name}!" };
    }

    [TestFixture]
    public class BasicSelfHostTest
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base("Basic SelfHost test", typeof(BasicHostServices).GetAssembly()) { }

            public override void Configure(Container container)
            {
            }
        }

        private readonly ServiceStackHost appHost;

        public BasicSelfHostTest()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.AbsoluteBaseUri);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Can_run_basic_SelfHost()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var response = client.Get(new Hello { Name = "AppSelfHostBase" });

            Assert.That(response.Result, Is.EqualTo("Hello, AppSelfHostBase!"));
        }

    }
}
