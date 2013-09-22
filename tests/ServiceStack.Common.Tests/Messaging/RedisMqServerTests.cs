using Funq;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Messaging;
using ServiceStack.Redis;
using ServiceStack.Redis.Messaging;
using ServiceStack.Clients;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Common.Tests.Messaging
{
    public class AnyTestMq
    {
        public int Id { get; set; }
    }

    public class AnyTestMqResponse
    {
        public int CorrelationId { get; set; }
    }

    public class PostTestMq
    {
        public int Id { get; set; }
    }

    public class PostTestMqResponse
    {
        public int CorrelationId { get; set; }
    }

    public class TestMqService : IService
    {
        public object Any(AnyTestMq request)
        {
            return new AnyTestMqResponse { CorrelationId = request.Id };
        }

        public object Post(PostTestMq request)
        {
            return new PostTestMqResponse { CorrelationId = request.Id };
        }
    }

    public class AppHost : AppHostHttpListenerBase
    {
        public AppHost()
            : base("Service Name", typeof(AnyTestMq).Assembly) { }

        public override void Configure(Container container)
        {
            var appSettings = new AppSettings();
            container.Register<IRedisClientsManager>(c => new PooledRedisClientManager(
                new string[] { appSettings.GetString("Redis.Host") ?? "localhost" }));
            container.Register<IMessageService>(c => new RedisMqServer(c.Resolve<IRedisClientsManager>()));
            container.Register<IMessageFactory>(c => c.Resolve<IMessageService>().MessageFactory);

            var mqServer = (RedisMqServer)container.Resolve<IMessageService>();
            mqServer.RegisterHandler<AnyTestMq>(ServiceController.ExecuteMessage);
            mqServer.RegisterHandler<PostTestMq>(ServiceController.ExecuteMessage);

            mqServer.Start();
        }
    }

    [TestFixture]
    public class RedisMqServerTests
    {
        private const string ListeningOn = "http://*:1337/";
        public const string Host = "http://localhost:1337";
        private const string BaseUri = Host + "/";

        AppHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new AppHost();
            appHost.Init();
            appHost.Start(ListeningOn);

            using (var redis = appHost.TryResolve<IRedisClientsManager>().GetClient())
                redis.FlushAll();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
            appHost = null;
        }

        [Test]
        public void Can_Publish_to_AnyTestMq_Service()
        {
            using (var mqFactory = appHost.TryResolve<IMessageFactory>())
            {
                var request = new AnyTestMq { Id = 1 };
                mqFactory.CreateMessageProducer().Publish(request);
                var msg = mqFactory.CreateMessageQueueClient().Get(QueueNames<AnyTestMqResponse>.In, null)
                    .ToMessage<AnyTestMqResponse>();
                Assert.That(msg.GetBody().CorrelationId, Is.EqualTo(request.Id));
            }
        }

        [Test]
        public void Can_Publish_to_PostTestMq_Service()
        {
            using (var mqFactory = appHost.TryResolve<IMessageFactory>())
            {
                var request = new PostTestMq { Id = 2 };
                mqFactory.CreateMessageProducer().Publish(request);
                var msg = mqFactory.CreateMessageQueueClient().Get(QueueNames<PostTestMqResponse>.In, null)
                    .ToMessage<PostTestMqResponse>();
                Assert.That(msg.GetBody().CorrelationId, Is.EqualTo(request.Id));
            }
        }

        [Test]
        public void SendOneWay_calls_AnyTestMq_Service_via_MQ()
        {
            var client = new JsonServiceClient(BaseUri);
            var request = new AnyTestMq { Id = 3 };

            client.SendOneWay(request);

            using (var mqFactory = appHost.TryResolve<IMessageFactory>())
            {
                var msg = mqFactory.CreateMessageQueueClient().Get(QueueNames<AnyTestMqResponse>.In, null)
                    .ToMessage<AnyTestMqResponse>();
                Assert.That(msg.GetBody().CorrelationId, Is.EqualTo(request.Id));
            }
        }

        [Test]
        public void SendOneWay_calls_PostTestMq_Service_via_MQ()
        {
            var client = new JsonServiceClient(BaseUri);
            var request = new PostTestMq { Id = 4 };

            client.SendOneWay(request);

            using (var mqFactory = appHost.TryResolve<IMessageFactory>())
            {
                var msg = mqFactory.CreateMessageQueueClient().Get(QueueNames<PostTestMqResponse>.In, null)
                    .ToMessage<PostTestMqResponse>();
                Assert.That(msg.GetBody().CorrelationId, Is.EqualTo(request.Id));
            }
        }
    }
}