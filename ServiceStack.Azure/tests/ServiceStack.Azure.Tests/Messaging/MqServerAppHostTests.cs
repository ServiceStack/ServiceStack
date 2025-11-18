using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Funq;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using NUnit.Framework;
using ServiceStack.Azure.Messaging;
using ServiceStack.Configuration;
using ServiceStack.FluentValidation;
using ServiceStack.Messaging;
using ServiceStack.Validation;
#if NETCORE
using QueueClient = Microsoft.Azure.ServiceBus.QueueClient;
#else
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
#endif

namespace ServiceStack.Azure.Tests.Messaging
{
    [TestFixture]
    public class AzureServiceBusMqServerAppHostTests : MqServerAppHostTests
    {
        private static string ConnectionString
        {
            get
            {
                var connString = Environment.GetEnvironmentVariable("AZURE_BUS_CONNECTION_STRING");
                if (connString != null)
                    return connString;
                
                var assembly = typeof(AzureServiceBusMqServerAppHostTests).Assembly;
                var path = new Uri(assembly.CodeBase).LocalPath;
                var configFile = Path.Combine(Path.GetDirectoryName(path), "settings.config");

                return new TextFileSettings(configFile).Get("ConnectionString");
            }
        }
        public AzureServiceBusMqServerAppHostTests()
        {
#if !NETCORE            
            NamespaceManager nm = NamespaceManager.CreateFromConnectionString(ConnectionString);
            Parallel.ForEach(nm.GetQueues(), qd =>
            {
                var sbClient = QueueClient.CreateFromConnectionString(ConnectionString, qd.Path, ReceiveMode.ReceiveAndDelete);
                BrokeredMessage msg = null;
                while ((msg = sbClient.Receive(new TimeSpan(0, 0, 1))) != null)
                {
                }
            });
#endif
        }

        public override IMessageService CreateMqServer(int retryCount = 1)
        {

            return new ServiceBusMqServer(ConnectionString) { RetryCount = retryCount };
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

        public void Post(QueueMessage request)
        {
        }

    }

    public class MqTestsAppHost : AppHostHttpListenerBase
    {
        private readonly Func<IMessageService> createMqServerFn;
        private int count = 0;
        public ManualResetEvent evt = new ManualResetEvent(false);

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
            mqServer.RegisterHandler<AnyTestMq>(q =>
            {
                return ServiceController.ExecuteMessage(q);
            });
            mqServer.RegisterHandler<AnyTestMqAsync>(msg
             => ServiceController.ExecuteMessage(msg));
            mqServer.RegisterHandler<PostTestMq>(ServiceController.ExecuteMessage);
            mqServer.RegisterHandler<ValidateTestMq>(ServiceController.ExecuteMessage);
            mqServer.RegisterHandler<ThrowGenericError>(ServiceController.ExecuteMessage);


            mqServer.RegisterHandler<QueueMessage>(m =>
            {
                Interlocked.Increment(ref count);
                var result = ServiceController.ExecuteMessage(m);
                if (count == 100)
                    evt.Set();
                return result;
            });

            mqServer.Start();
        }
    }

    [TestFixture]
    public abstract class MqServerAppHostTests
    {
        protected const string ListeningOn = "http://*:2001/";
        public const string Host = "http://localhost:2001";
        private const string BaseUri = Host + "/";

        protected ServiceStackHost appHost;

        public abstract IMessageService CreateMqServer(int retryCount = 1);

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            appHost = new MqTestsAppHost(() => CreateMqServer())
                .Init()
                .Start(ListeningOn);
        }

        [OneTimeTearDown]
        public virtual void OneTimeTearDown()
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
                        ReplyTo = "{0}{1}.replyto".Fmt(QueueNames.MqPrefix, request.GetType().Name)
                    };
                    mqProducer.Publish(requestMsg);

                    var errorMsg = mqClient.Get<ValidateTestMqResponse>(requestMsg.ReplyTo, null);
                    mqClient.Ack(errorMsg);

                    Assert.That(errorMsg.GetBody().ResponseStatus.ErrorCode, Is.EqualTo("PositiveIntegersOnly"));

                    request = new ValidateTestMq { Id = 10 };
                    requestMsg = new Message<ValidateTestMq>(request)
                    {
                        ReplyTo = "{0}{1}.replyto".Fmt(QueueNames.MqPrefix, request.GetType().Name)
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
                        ReplyTo = "{0}{1}.replyto".Fmt(QueueNames.MqPrefix, request.GetType().Name)
                    };
                    mqProducer.Publish(requestMsg);

                    var msg = mqClient.Get<ErrorResponse>(requestMsg.ReplyTo, null);
                    mqClient.Ack(msg);

                    Assert.That(msg.GetBody().ResponseStatus.ErrorCode, Is.EqualTo("ArgumentException"));
                }
            }
        }

        [Test]
        public void Can_Publish_In_Parallel()
        {
            new Thread(_ =>
            {
                using (var mqFactory = appHost.TryResolve<IMessageFactory>())
                {
                    using (var mqProducer = mqFactory.CreateMessageProducer())
                    {
                        var range = Enumerable.Range(1, 100);

                        Parallel.For(0, range.Last(),
                            index =>
                            {
                                mqProducer.Publish<QueueMessage>(
                                    new QueueMessage {Id = index + 20000, BodyHtml = "test"});
                            });
                    }
                }
            }).Start();

            ((MqTestsAppHost)appHost).evt.WaitOne();
        }

        [Test, Ignore("Benchmark")]
        [Explicit]
        public void CheckPerf()
        {
            using (var mqFactory = appHost.TryResolve<IMessageFactory>())
            {
                using (var mqProducer = mqFactory.CreateMessageProducer())
                using (var mqClient = mqFactory.CreateMessageQueueClient())
                {
                    var range = Enumerable.Range(1, 1000);

                    Parallel.For(0, range.Last(),
                        index =>
                        {
                            mqProducer.Publish<QueueMessage>(
                                new QueueMessage {Id = index + 20000, BodyHtml = "test"});
                        });

                    IMessage<QueueMessage> msg;
                    while ((msg = mqClient.Get<QueueMessage>(QueueNames<QueueMessage>.In, null)) != null)
                    {
                        mqClient.Ack(msg);
                    }
                }
            }
        }

    }

    public class QueueMessage
    {
        public int Id { get; set; }

        public string BodyHtml { get; set; }
    }
}
