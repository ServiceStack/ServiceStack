using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/sendjson")]
    public class SendJson : IRequiresRequestStream, IReturn<string>
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public Stream RequestStream { get; set; }
    }

    [Route("/sendtext")]
    public class SendText : IRequiresRequestStream, IReturn<string>
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string ContentType { get; set; }

        public Stream RequestStream { get; set; }
    }

    [Route("/sendraw")]
    public class SendRaw : IRequiresRequestStream, IReturn<byte[]>
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string ContentType { get; set; }

        public Stream RequestStream { get; set; }
    }

    public class SendRawService : Service
    {
        [JsonOnly]
        public async Task<object> Any(SendJson request)
        {
            base.Response.AddHeader("X-Args", $"{request.Id},{request.Name}");

            return await request.RequestStream.ReadToEndAsync();
        }

        public async Task<object> Any(SendText request)
        {
            base.Response.AddHeader("X-Args", $"{request.Id},{request.Name}");

            base.Request.ResponseContentType = request.ContentType ?? base.Request.AcceptTypes[0];
            return await request.RequestStream.ReadToEndAsync();
        }

        public async Task<object> Any(SendRaw request)
        {
            base.Response.AddHeader("X-Args", $"{request.Id},{request.Name}");

            base.Request.ResponseContentType = request.ContentType ?? base.Request.AcceptTypes[0];
            return await request.RequestStream.ReadToEndAsync();
        }
    }

    public class TestBody
    {
        public string Foo { get; set; }
    }

    public class JsonServiceClientSendBodyTests : ServiceClientSendBodyTests
    {
        public override IServiceClient CreateClient()
        {
            return new JsonServiceClient(Config.ListeningOn);
        }
    }

    public class JsonHttpClientSendBodyTests : ServiceClientSendBodyTests
    {
        public override IServiceClient CreateClient()
        {
            return new JsonHttpClient(Config.ListeningOn);
        }
    }

    public abstract class ServiceClientSendBodyTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() 
                : base(nameof(ServiceClientSendBodyTests), typeof(SendRawService).Assembly) {}

            public override void Configure(Container container)
            {
            }
        }

        private readonly ServiceStackHost appHost;

        protected ServiceClientSendBodyTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();


        public abstract IServiceClient CreateClient();

        public SendJson CreateSendJson(IServiceClient client)
        {
            if (client is ServiceClientBase scb)
            {
                scb.ResponseFilter = res => Assert.That(res.Headers["X-Args"], Is.EqualTo("1,name"));
            }
            else if (client is JsonHttpClient jhc)
            {
                jhc.ResponseFilter = res => Assert.That(res.Headers.GetValues("X-Args").FirstOrDefault(), Is.EqualTo("1,name"));
            }

            return new SendJson
            {
                Id = 1,
                Name = "name",
            };
        }

        public SendText CreateSendText(IServiceClient client)
        {
            if (client is ServiceClientBase scb)
            {
                scb.ResponseFilter = res => Assert.That(res.Headers["X-Args"], Is.EqualTo("1,name"));
            }
            else if (client is JsonHttpClient jhc)
            {
                jhc.ResponseFilter = res => Assert.That(res.Headers.GetValues("X-Args").FirstOrDefault(), Is.EqualTo("1,name"));
            }

            return new SendText
            {
                Id = 1,
                Name = "name",
                ContentType = "text/plain"
            };
        }

        [Test]
        public void Can_SendBody()
        {
            var client = CreateClient();
            var toRequest = CreateSendJson(client);

            var body = new TestBody { Foo = "Bar" };

            var json = client.PostBody(toRequest, body);
            Assert.That(json.FromJson<TestBody>().Foo, Is.EqualTo("Bar"));

            json = client.PutBody(toRequest, body.ToJson());
            Assert.That(json.FromJson<TestBody>().Foo, Is.EqualTo("Bar"));

            json = client.PatchBody(toRequest, MemoryStreamFactory.GetStream(body.ToJson().ToUtf8Bytes()));
            Assert.That(json.FromJson<TestBody>().Foo, Is.EqualTo("Bar"));
        }

        [Test]
        public async Task Can_SendBody_Async()
        {
            var client = CreateClient();
            var toRequest = CreateSendJson(client);

            var body = new TestBody { Foo = "Bar" };

            var json = await client.PostBodyAsync(toRequest, body);
            Assert.That(json.FromJson<TestBody>().Foo, Is.EqualTo("Bar"));

            json = await client.PutBodyAsync(toRequest, body.ToJson());
            Assert.That(json.FromJson<TestBody>().Foo, Is.EqualTo("Bar"));

            json = await client.PatchBodyAsync(toRequest, MemoryStreamFactory.GetStream(body.ToJson().ToUtf8Bytes()));
            Assert.That(json.FromJson<TestBody>().Foo, Is.EqualTo("Bar"));
        }

        [Test]
        public void Can_SendBody_Raw_String()
        {
            var client = CreateClient();
            var toRequest = CreateSendText(client);

            var str = client.PutBody(toRequest, "foo");
            Assert.That(str, Is.EqualTo("foo"));
        }

        [Test]
        public async Task Can_SendBody_Raw_String_Async()
        {
            var client = CreateClient();
            var toRequest = CreateSendText(client);

            var str = await client.PutBodyAsync(toRequest, "foo");
            Assert.That(str, Is.EqualTo("foo"));
        }
    }
}