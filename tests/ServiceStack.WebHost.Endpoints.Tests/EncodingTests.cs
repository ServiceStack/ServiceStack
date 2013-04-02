using System;
using System.Text;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    //[Route("/HelloWorld/Greeting/{FirstName}/{LastName}", "GET")]
    [Route("/HelloWorld/Greeting/{FirstName}", "GET")]
    [Restrict(EndpointAttributes.InternalNetworkAccess)]
    public class HelloWorldName : IReturn<HelloWorldGreeting>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class HelloWorldGreeting
    {
        public string Greeting { get; set; }
    }

    public class HelloWorldService : ServiceInterface.Service
    {
        public HelloWorldGreeting Get(HelloWorldName request)
        {
            var answer = new HelloWorldGreeting
            {
                Greeting = "Hello " + request.FirstName + " " + request.LastName
            };
            return answer;
        }
    }

    public class EncodingTestsAppHost : AppHostHttpListenerBase
    {
        public EncodingTestsAppHost() : base("EncodingTests", typeof(HelloWorldService).Assembly) { }
        public override void Configure(Funq.Container container) {}
    }

    [TestFixture]
    public class EncodingTests
    {
        private EncodingTestsAppHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new EncodingTestsAppHost();
            appHost.Init();
            appHost.Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
            appHost = null;
        }

        private HelloWorldGreeting PerformRequest(string firstName, string lastName)
        {
            var client = new JsonServiceClient(Config.ServiceStackBaseUri);
            var query = string.Format("/HelloWorld/Greeting/{0}?lastname={1}", firstName, lastName);
            return client.Get<HelloWorldGreeting>(query);
        }

        [Test]
        public void Can_Get_Greeting_When_Querystring_Contains_Non_ASCII_Chars()
        {
            var firstName = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("Pål"));
            var lastName = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("Smådalø"));
            Assert.That(PerformRequest(firstName, lastName).Greeting, Is.EqualTo(string.Format("Hello {0} {1}", firstName, lastName)));
        }

        [Test]
        public void Can_Get_Greeting_When_Only_Url_Contains_Non_ASCII_Chars()
        {
            var firstName = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("Pål"));
            var lastName = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("Smith"));
            Assert.That(PerformRequest(firstName, lastName).Greeting, Is.EqualTo(string.Format("Hello {0} {1}", firstName, lastName)));
        }

        [Test]
        public void Can_Get_Greeting_When_Querystring_Contains_Only_ASCII_Chars()
        {
            var firstName = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("John"));
            var lastName = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("Smith"));
            Assert.That(PerformRequest(firstName, lastName).Greeting, Is.EqualTo(string.Format("Hello {0} {1}", firstName, lastName)));
        }
    }

}