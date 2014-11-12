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

    public class ReplyAllService : Service
    {
        public object Any(HelloAll request)
        {
            return new HelloAllResponse { Result = "Hello, {0}!".Fmt(request.Name) };
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
        public void Can_send_multi_reply_request()
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
    }
}