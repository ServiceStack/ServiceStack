using System;
using System.Net;
using System.Reflection;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class InvalidRequests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() 
                : base(nameof(InvalidRequests), typeof(MyServices).Assembly) { }
            
            public override void Configure(Container container)
            {
                SetConfig(new HostConfig {
                    DebugMode = false
                });
            }
        }

        private readonly ServiceStackHost appHost;
        public InvalidRequests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Invalid_Request_does_not_return_StackTrace_when_not_DebugMode()
        {
            try
            {
                var response = Config.ListeningOn.CombineWith("*|?")
                    .GetJsonFromUrl();

                Assert.Fail("Should throw");
            }
            catch (Exception ex)
            {
                Assert.That(ex.GetStatus(), Is.EqualTo(HttpStatusCode.NotFound));

                if (ex is WebException webEx)
                {
                    var errorBody = webEx.GetResponseBody();
                    Assert.That(errorBody.ToLower(), Does.Not.Contain("stacktrace"));
                }
            }
        }
    }
}