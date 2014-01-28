using Funq;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Messaging;
using ServiceStack.Messaging.Redis;
using ServiceStack.Redis;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace ServiceStack.Common.Tests.Messaging
{
    public class RedisMqAppHost : AppHostHttpListenerBase
    {
        public RedisMqAppHost()
            : base("Service Name", typeof(AnyTestMq).Assembly) { }

        public override void Configure(Container container)
        {
            Plugins.Add(new ValidationFeature());
            container.RegisterValidators(typeof(ValidateTestMqValidator).Assembly);

            var appSettings = new AppSettings();
            container.Register<IRedisClientsManager>(c => new PooledRedisClientManager(
                new[] { appSettings.GetString("Redis.Host") ?? "localhost" }));
            container.Register<IMessageService>(c => new RedisMqServer(c.Resolve<IRedisClientsManager>()));

            var mqServer = (RedisMqServer)container.Resolve<IMessageService>();
            mqServer.RegisterHandler<AnyTestMq>(ServiceController.ExecuteMessage);
            mqServer.RegisterHandler<PostTestMq>(ServiceController.ExecuteMessage);
            mqServer.RegisterHandler<ValidateTestMq>(ServiceController.ExecuteMessage);
            mqServer.RegisterHandler<ThrowGenericError>(ServiceController.ExecuteMessage);

            mqServer.Start();
        }
    }

    [TestFixture]
    public class RedisMqServerInAppHostTests
    {
        private const string ListeningOn = "http://*:1337/";
        public const string Host = "http://localhost:1337";
        private const string BaseUri = Host + "/";

        ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new RedisMqAppHost()
                .Init()
                .Start(ListeningOn);

            using (var redis = appHost.TryResolve<IRedisClientsManager>().GetClient())
                redis.FlushAll();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_Publish_to_AnyTestMq_Service()
        {
            using (var mqFactory = appHost.TryResolve<IMessageFactory>())
            {
                var request = new AnyTestMq { Id = 1 };

                using (var mqProducer = mqFactory.CreateMessageProducer())
                    mqProducer.Publish(request);

                using (var mqClient = mqFactory.CreateMessageQueueClient())
                {
                    var msg = mqClient.Get<AnyTestMqResponse>(QueueNames<AnyTestMqResponse>.In, null);
                    mqClient.Ack(msg);
                    Assert.That(msg.GetBody().CorrelationId, Is.EqualTo(request.Id));
                }
            }
        }

        [Test]
        public void Can_Publish_to_PostTestMq_Service()
        {
            using (var mqFactory = appHost.TryResolve<IMessageFactory>())
            {
                var request = new PostTestMq { Id = 2 };

                using (var mqProducer = mqFactory.CreateMessageProducer())
                    mqProducer.Publish(request);

                using (var mqClient = mqFactory.CreateMessageQueueClient())
                {
                    var msg = mqClient.Get<PostTestMqResponse>(QueueNames<PostTestMqResponse>.In, null);
                    mqClient.Ack(msg);
                    Assert.That(msg.GetBody().CorrelationId, Is.EqualTo(request.Id));
                }
            }
        }

        [Test]
        public void SendOneWay_calls_AnyTestMq_Service_via_MQ()
        {
            var client = new JsonServiceClient(BaseUri);
            var request = new AnyTestMq { Id = 3 };

            client.SendOneWay(request);

            using (var mqFactory = appHost.TryResolve<IMessageFactory>())
            using (var mqClient = mqFactory.CreateMessageQueueClient())
            {
                var msg = mqClient.Get<AnyTestMqResponse>(QueueNames<AnyTestMqResponse>.In, null);
                mqClient.Ack(msg);
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
            using (var mqClient = mqFactory.CreateMessageQueueClient())
            {
                var msg = mqClient.Get<PostTestMqResponse>(QueueNames<PostTestMqResponse>.In, null);
                mqClient.Ack(msg);
                Assert.That(msg.GetBody().CorrelationId, Is.EqualTo(request.Id));
            }
        }

        [Test]
        public void Does_execute_validation_filters()
        {
            using (var mqFactory = appHost.TryResolve<IMessageFactory>())
            {
                var request = new ValidateTestMq { Id = -10 };

                using (var mqProducer = mqFactory.CreateMessageProducer())
                using (var mqClient = mqFactory.CreateMessageQueueClient())
                {
                    mqProducer.Publish(request);

                    var msg = mqClient.Get<ValidateTestMqResponse>(QueueNames<ValidateTestMqResponse>.Dlq, null);
                    mqClient.Ack(msg);

                    msg.GetBody().PrintDump();
                    Assert.That(msg.GetBody().ResponseStatus.ErrorCode, Is.EqualTo("PositiveIntegersOnly"));

                    request = new ValidateTestMq { Id = 10 };
                    mqProducer.Publish(request);
                    msg = mqClient.Get<ValidateTestMqResponse>(QueueNames<ValidateTestMqResponse>.In, null);
                    mqClient.Ack(msg);
                    Assert.That(msg.GetBody().CorrelationId, Is.EqualTo(request.Id));
                }
            }
        }

        [Test]
        public void Does_handle_generic_errors()
        {
            using (var mqFactory = appHost.TryResolve<IMessageFactory>())
            {
                var request = new ThrowGenericError { Id = 1 };

                using (var mqProducer = mqFactory.CreateMessageProducer())
                using (var mqClient = mqFactory.CreateMessageQueueClient())
                {
                    mqProducer.Publish(request);

                    var msg = mqClient.Get<ThrowGenericError>(QueueNames<ThrowGenericError>.Dlq, null);
                    mqClient.Ack(msg);

                    msg.PrintDump();
                    Assert.That(msg.Error.ErrorCode, Is.EqualTo("ArgumentException"));
                }
            }
        }

    }
}