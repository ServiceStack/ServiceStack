using System.Linq;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class ReplyAllAppHost : AppSelfHostBase
    {
        public ReplyAllAppHost()
            : base(typeof(ReplyAllTests).Name, typeof(ReplyAllService).Assembly) { }

        public override void Configure(Container container)
        {
        }
    }

    public class HelloAll : IReturn<HelloAllResponse>
    {
        public string Name { get; set; }
    }

    public class HelloAllResponse
    {
        public string Result { get; set; }
    }

    public class HelloAllCustom : IReturn<HelloAllCustomResponse>
    {
        public string Name { get; set; }
    }

    public class HelloAllCustomResponse
    {
        public string Result { get; set; }
    }

    public class ReplyAllService : Service
    {
        public object Any(HelloAll request)
        {
            return new HelloAllResponse { Result = "Hello, {0}!".Fmt(request.Name) };
        }

        public object Any(HelloAllCustom request)
        {
            return new HelloAllCustomResponse { Result = "Hello, {0}!".Fmt(request.Name) };
        }

        public object Any(HelloAllCustom[] requests)
        {
            return requests.Map(x => new HelloAllCustomResponse
            {
                Result = "Custom, {0}!".Fmt(x.Name)
            });
        }
    }

    [TestFixture]
    public class ReplyAllTests
    {
        private ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new ReplyAllAppHost()
                .Init()
                .Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_send_single_HelloAll_request()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var request = new HelloAll { Name = "Foo" };
            var response = client.Send(request);
            Assert.That(response.Result, Is.EqualTo("Hello, Foo!"));
        }

        [Test]
        public void Can_send_multi_reply_HelloAll_requests()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var requests = new[]
            {
                new HelloAll { Name = "Foo" },
                new HelloAll { Name = "Bar" },
                new HelloAll { Name = "Baz" },
            };

            var responses = client.SendAll(requests);
            responses.PrintDump();

            var results = responses.Map(x => x.Result);

            Assert.That(results, Is.EquivalentTo(new[] {
                "Hello, Foo!", "Hello, Bar!", "Hello, Baz!"
            }));
        }

        [Test]
        public void Can_send_single_HelloAllCustom_request()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var request = new HelloAllCustom { Name = "Foo" };
            var response = client.Send(request);
            Assert.That(response.Result, Is.EqualTo("Hello, Foo!"));
        }

        [Test]
        public void Can_send_multi_reply_HelloAllCustom_requests()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var requests = new[]
            {
                new HelloAllCustom { Name = "Foo" },
                new HelloAllCustom { Name = "Bar" },
                new HelloAllCustom { Name = "Baz" },
            };

            var responses = client.SendAll(requests);
            responses.PrintDump();

            var results = responses.Map(x => x.Result);

            Assert.That(results, Is.EquivalentTo(new[] {
                "Custom, Foo!", "Custom, Bar!", "Custom, Baz!"
            }));
        }
    }
}