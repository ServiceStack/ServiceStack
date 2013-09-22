using System;
using System.Collections;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Clients;
using ServiceStack.Clients;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class HelloWorldServiceClientTests
    {
        public static IEnumerable ServiceClients
        {
            get
            {
                return new IServiceClient[] {
					new JsonServiceClient(Config.ServiceStackBaseUri),
					new JsvServiceClient(Config.ServiceStackBaseUri),
					new XmlServiceClient(Config.ServiceStackBaseUri),
					new Soap11ServiceClient(Config.ServiceStackBaseUri),
					new Soap12ServiceClient(Config.ServiceStackBaseUri)
				};
            }
        }

        public static IEnumerable RestClients
        {
            get
            {
                return new IRestClient[] {
					new JsonServiceClient(Config.ServiceStackBaseUri),
					new JsvServiceClient(Config.ServiceStackBaseUri),
					new XmlServiceClient(Config.ServiceStackBaseUri),
				};
            }
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Sync_Call_HelloWorld_with_Sync_ServiceClients_on_PreDefined_Routes(IServiceClient client)
        {
            var response = client.Send<HelloResponse>(new Hello { Name = "World!" });

            Assert.That(response.Result, Is.EqualTo("Hello, World!"));
        }

        [Test, TestCaseSource("RestClients")]
        public void Async_Call_HelloWorld_with_ServiceClients_on_PreDefined_Routes(IServiceClient client)
        {
            HelloResponse response = null;
            client.SendAsync<HelloResponse>(new Hello { Name = "World!" },
                r => response = r, (r, e) => Assert.Fail("NetworkError"));

            Thread.Sleep(TimeSpan.FromSeconds(1));

            Assert.That(response.Result, Is.EqualTo("Hello, World!"));
        }

        [Test, TestCaseSource("RestClients")]
        public void Sync_Call_HelloWorld_with_RestClients_on_UserDefined_Routes(IRestClient client)
        {
            var response = client.Get<HelloResponse>("/hello/World!");

            Assert.That(response.Result, Is.EqualTo("Hello, World!"));
        }

        [Test, TestCaseSource("RestClients")]
        public void Async_Call_HelloWorld_with_Async_ServiceClients_on_UserDefined_Routes(IServiceClient client)
        {
            HelloResponse response = null;
            client.GetAsync<HelloResponse>("/hello/World!",
                r => response = r, (r, e) => Assert.Fail("NetworkError"));

            var i = 0;
            while (response == null && i++ < 5)
                Thread.Sleep(TimeSpan.FromSeconds(1));

            Assert.That(response.Result, Is.EqualTo("Hello, World!"));
        }
    }
}