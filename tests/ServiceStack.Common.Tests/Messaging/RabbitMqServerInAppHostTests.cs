using Funq;
using NUnit.Framework;
using RabbitMQ.Client;
using ServiceStack.Configuration;
using ServiceStack.Messaging;
using ServiceStack.RabbitMq;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace ServiceStack.Common.Tests.Messaging
{
    public class RabbitMqAppHost : AppHostHttpListenerBase
    {
        public RabbitMqAppHost()
            : base("Service Name", typeof(AnyTestMq).Assembly) { }

        public override void Configure(Container container)
        {
            Plugins.Add(new ValidationFeature());
            container.RegisterValidators(typeof(ValidateTestMqValidator).Assembly);

            var appSettings = new AppSettings();
            container.Register<IMessageService>(c =>
                new RabbitMqServer(appSettings.GetString("RabbitMq.Host") ?? "localhost"));
            container.Register(c =>
                ((RabbitMqServer) c.Resolve<IMessageService>()).ConnectionFactory);

            var mqServer = (RabbitMqServer)container.Resolve<IMessageService>();
            mqServer.RegisterHandler<AnyTestMq>(ServiceController.ExecuteMessage);
            mqServer.RegisterHandler<PostTestMq>(ServiceController.ExecuteMessage);
            mqServer.RegisterHandler<ValidateTestMq>(ServiceController.ExecuteMessage);

            mqServer.Start();
        }
    }

    [TestFixture]
    public class RabbitMqServerInAppHostTests
    {
        private const string ListeningOn = "http://*:1337/";
        public const string Host = "http://localhost:1337";
        private const string BaseUri = Host + "/";

        ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new RabbitMqAppHost()
                .Init()
                .Start(ListeningOn);

            using (var conn = appHost.TryResolve<ConnectionFactory>().CreateConnection())
            using (var channel = conn.CreateModel())
            {
                channel.PurgeQueue<AnyTestMq>();
                channel.PurgeQueue<AnyTestMqResponse>();
                channel.PurgeQueue<PostTestMq>();
                channel.PurgeQueue<PostTestMqResponse>();
                channel.PurgeQueue<ValidateTestMq>();
                channel.PurgeQueue<ValidateTestMqResponse>();
            }
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

    }
}