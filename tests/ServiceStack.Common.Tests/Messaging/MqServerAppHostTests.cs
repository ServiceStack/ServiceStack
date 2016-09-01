#if !NETCORE_SUPPORT
using System;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.FluentValidation;
using ServiceStack.Messaging;
using ServiceStack.Messaging.Redis;
using ServiceStack.RabbitMq;
using ServiceStack.Redis;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace ServiceStack.Common.Tests.Messaging
{
    [TestFixture]
    public class RedisMqServerAppHostTests : MqServerAppHostTests
    {
        public RedisMqServerAppHostTests()
        {
            using (var redis = ((RedisMqServer)CreateMqServer()).ClientsManager.GetClient())
                redis.FlushAll();
        }

        public override IMessageService CreateMqServer(int retryCount = 1)
        {
            return new RedisMqServer(new PooledRedisClientManager()) { RetryCount = retryCount };
        }
    }

    [TestFixture]
    public class RabbitMqServerAppHostTests : MqServerAppHostTests
    {
        public RabbitMqServerAppHostTests()
        {
            using (var conn = ((RabbitMqServer)CreateMqServer()).ConnectionFactory.CreateConnection())
            using (var channel = conn.CreateModel())
            {
                channel.PurgeQueue<AnyTestMq>();
                channel.PurgeQueue<AnyTestMqAsync>();
                channel.PurgeQueue<AnyTestMqResponse>();
                channel.PurgeQueue<PostTestMq>();
                channel.PurgeQueue<PostTestMqResponse>();
                channel.PurgeQueue<ValidateTestMq>();
                channel.PurgeQueue<ValidateTestMqResponse>();
                channel.PurgeQueue<ThrowGenericError>();
            }
        }

        public override IMessageService CreateMqServer(int retryCount = 1)
        {
            return new RabbitMqServer { RetryCount = 1 };
        }
    }

    [TestFixture]
    public class MemoryMqServerAppHostTests : MqServerAppHostTests
    {
        public override IMessageService CreateMqServer(int retryCount = 1)
        {
            return new InMemoryTransientMessageService { RetryCount = retryCount };
        }
    }


    public class AnyTestMq
    {
        public int Id { get; set; }
    }

    public class AnyTestMqAsync
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

    public class ValidateTestMq
    {
        public int Id { get; set; }
    }

    public class ValidateTestMqResponse
    {
        public int CorrelationId { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    public class ThrowGenericError
    {
        public int Id { get; set; }
    }

    public class ValidateTestMqValidator : AbstractValidator<ValidateTestMq>
    {
        public ValidateTestMqValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("PositiveIntegersOnly");
        }
    }

    public class TestMqService : IService
    {
        public object Any(AnyTestMq request)
        {
            return new AnyTestMqResponse { CorrelationId = request.Id };
        }

        public async Task<object> Any(AnyTestMqAsync request)
        {
            return await Task.Factory.StartNew(() =>
                new AnyTestMqResponse { CorrelationId = request.Id });
        }

        public object Post(PostTestMq request)
        {
            return new PostTestMqResponse { CorrelationId = request.Id };
        }

        public object Post(ValidateTestMq request)
        {
            return new ValidateTestMqResponse { CorrelationId = request.Id };
        }

        public object Post(ThrowGenericError request)
        {
            throw new ArgumentException("request");
        }
    }

    public class MqTestsAppHost : AppHostHttpListenerBase
    {
        private readonly Func<IMessageService> createMqServerFn;

        public MqTestsAppHost(Func<IMessageService> createMqServerFn)
            : base("Service Name", typeof(AnyTestMq).Assembly)
        {
            this.createMqServerFn = createMqServerFn;
        }

        public override void Configure(Container container)
        {
            Plugins.Add(new ValidationFeature());
            container.RegisterValidators(typeof(ValidateTestMqValidator).Assembly);

            container.Register(c => createMqServerFn());

            var mqServer = container.Resolve<IMessageService>();
            mqServer.RegisterHandler<AnyTestMq>(ExecuteMessage);
            mqServer.RegisterHandler<AnyTestMqAsync>(ExecuteMessage);
            mqServer.RegisterHandler<PostTestMq>(ExecuteMessage);
            mqServer.RegisterHandler<ValidateTestMq>(ExecuteMessage);
            mqServer.RegisterHandler<ThrowGenericError>(ExecuteMessage);

            mqServer.Start();
        }
    }

    [TestFixture]
    public abstract class MqServerAppHostTests
    {
        protected const string ListeningOn = "http://*:1337/";
        public const string Host = "http://localhost:1337";
        private const string BaseUri = Host + "/";

        protected ServiceStackHost appHost;

        public abstract IMessageService CreateMqServer(int retryCount = 1);

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new MqTestsAppHost(() => CreateMqServer())
                .Init()
                .Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public virtual void TestFixtureTearDown()
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
        public void Can_Publish_to_AnyTestMqAsync_Service()
        {
            using (var mqFactory = appHost.TryResolve<IMessageFactory>())
            {
                var request = new AnyTestMqAsync { Id = 1 };

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

                    var errorMsg = mqClient.Get<ValidateTestMq>(QueueNames<ValidateTestMq>.Dlq, null);
                    mqClient.Ack(errorMsg);

                    Assert.That(errorMsg.Error.ErrorCode, Is.EqualTo("PositiveIntegersOnly"));

                    request = new ValidateTestMq { Id = 10 };
                    mqProducer.Publish(request);
                    var responseMsg = mqClient.Get<ValidateTestMqResponse>(QueueNames<ValidateTestMqResponse>.In, null);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().CorrelationId, Is.EqualTo(request.Id));
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

                    Assert.That(msg.Error.ErrorCode, Is.EqualTo("ArgumentException"));
                }
            }
        }

        [Test]
        public void Does_execute_ReplyTo_validation_filters()
        {
            using (var mqFactory = appHost.TryResolve<IMessageFactory>())
            {
                var request = new ValidateTestMq { Id = -10 };

                using (var mqProducer = mqFactory.CreateMessageProducer())
                using (var mqClient = mqFactory.CreateMessageQueueClient())
                {
                    var requestMsg = new Message<ValidateTestMq>(request)
                    {
                        ReplyTo = "mq:{0}.replyto".Fmt(request.GetType().Name)
                    };
                    mqProducer.Publish(requestMsg);

                    var errorMsg = mqClient.Get<ValidateTestMqResponse>(requestMsg.ReplyTo, null);
                    mqClient.Ack(errorMsg);

                    Assert.That(errorMsg.GetBody().ResponseStatus.ErrorCode, Is.EqualTo("PositiveIntegersOnly"));

                    request = new ValidateTestMq { Id = 10 };
                    requestMsg = new Message<ValidateTestMq>(request)
                    {
                        ReplyTo = "mq:{0}.replyto".Fmt(request.GetType().Name)
                    };
                    mqProducer.Publish(requestMsg);
                    var responseMsg = mqClient.Get<ValidateTestMqResponse>(requestMsg.ReplyTo, null);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().CorrelationId, Is.EqualTo(request.Id));
                }
            }
        }

        [Test]
        public void Does_handle_ReplyTo_generic_errors()
        {
            using (var mqFactory = appHost.TryResolve<IMessageFactory>())
            {
                var request = new ThrowGenericError { Id = 1 };

                using (var mqProducer = mqFactory.CreateMessageProducer())
                using (var mqClient = mqFactory.CreateMessageQueueClient())
                {
                    var requestMsg = new Message<ThrowGenericError>(request)
                    {
                        ReplyTo = "mq:{0}.replyto".Fmt(request.GetType().Name)
                    };
                    mqProducer.Publish(requestMsg);

                    var msg = mqClient.Get<ErrorResponse>(requestMsg.ReplyTo, null);
                    mqClient.Ack(msg);

                    Assert.That(msg.GetBody().ResponseStatus.ErrorCode, Is.EqualTo("ArgumentException"));
                }
            }
        }
    }
}
#endif