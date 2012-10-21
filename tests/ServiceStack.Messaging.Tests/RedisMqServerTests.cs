using System.Reflection;
using Funq;
using NUnit.Framework;
using ServiceStack.Redis;
using ServiceStack.Redis.Messaging;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Messaging.Tests
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
            : base("Service Name", typeof(AnyTestMq).Assembly) {}

        public override void Configure(Container container)
        {
            container.Register<IRedisClientsManager>(c => new PooledRedisClientManager());
            container.Register<IMessageService>(c => new RedisMqServer(c.Resolve<IRedisClientsManager>()));
            container.Register<IMessageFactory>(c => c.Resolve<IMessageService>().MessageFactory);

            var mqServer =(RedisMqServer)container.Resolve<IMessageService>();
            mqServer.RegisterHandler<AnyTestMq>(ServiceController.ExecuteMessage);
            mqServer.RegisterHandler<PostTestMq>(ServiceController.ExecuteMessage);

            mqServer.Start();
        }
    }

    [TestFixture]
    public class RedisMqServerTests
    {
        AppHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new AppHost();
            appHost.Init();

            using (var redis = appHost.TryResolve<IRedisClientsManager>().GetClient())
            {
                redis.FlushAll();
            }
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
                var request = new AnyTestMq {Id = 1};
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
                var request = new PostTestMq { Id = 1 };
                mqFactory.CreateMessageProducer().Publish(request);
                var msg = mqFactory.CreateMessageQueueClient().Get(QueueNames<PostTestMqResponse>.In, null)
                    .ToMessage<PostTestMqResponse>();
                Assert.That(msg.GetBody().CorrelationId, Is.EqualTo(request.Id));
            }
        }
    }
}