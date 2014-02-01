using System;
using System.Reflection;
using System.Threading;
using Funq;
using NUnit.Framework;
using ServiceStack.Messaging;
using ServiceStack.RabbitMq;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.Server.Tests.Messaging
{
    public class Hello
    {
        public string Name { get; set; }
    }

    public class HelloResponse
    {
        public string Result { get; set; }
    }

    public class HelloService : Service
    {
        public object Any(Hello request)
        {
            return new HelloResponse { Result = "Hello, {0}!".Fmt(request.Name) };
        }
    }

    public class AppHost : AppHostHttpListenerBase
    {
        public AppHost() : base("Rabbit MQ Test Host", typeof(HelloService).Assembly) { }

        public override void Configure(Container container)
        {
            container.Register<IMessageService>(c => new RabbitMqServer());

            var mqServer = container.Resolve<IMessageService>();

            mqServer.RegisterHandler<Hello>(ServiceController.ExecuteMessage);
            mqServer.Start();
        }
    }

    [TestFixture]
    public class RabbitMqServerIntroTests
    {
        [Test]
        public void Messages_with_no_responses_are_published_to_Request_outq_topic()
        {
            using (var mqServer = new RabbitMqServer())
            {
                mqServer.RegisterHandler<Hello>(m =>
                {
                    "Hello, {0}!".Print(m.GetBody().Name);
                    return null;
                });
                mqServer.Start();

                using (var mqClient = mqServer.CreateMessageQueueClient())
                {
                    mqClient.Publish(new Hello { Name = "World" });

                    IMessage<Hello> msgCopy = mqClient.Get<Hello>(QueueNames<Hello>.Out);
                    mqClient.Ack(msgCopy);
                    Assert.That(msgCopy.GetBody().Name, Is.EqualTo("World"));
                }
            }
        }

        [Test]
        public void Message_with_response_are_published_to_Response_inq()
        {
            using (var mqServer = new RabbitMqServer())
            {
                mqServer.RegisterHandler<Hello>(m =>
                    new HelloResponse { Result = "Hello, {0}!".Fmt(m.GetBody().Name) });
                mqServer.Start();

                using (var mqClient = mqServer.CreateMessageQueueClient())
                {
                    mqClient.Publish(new Hello { Name = "World" });

                    IMessage<HelloResponse> responseMsg = mqClient.Get<HelloResponse>(QueueNames<HelloResponse>.In);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().Result, Is.EqualTo("Hello, World!"));
                }
            }
        }

        [Test]
        public void Message_with_exceptions_are_retried_then_published_to_Request_dlq()
        {
            using (var mqServer = new RabbitMqServer { RetryCount = 1 })
            {
                var called = 0;
                mqServer.RegisterHandler<Hello>(m =>
                {
                    called++;
                    throw new ArgumentException("Name");
                });
                mqServer.Start();

                using (var mqClient = mqServer.CreateMessageQueueClient())
                {
                    mqClient.Publish(new Hello { Name = "World" });

                    IMessage<Hello> dlqMsg = mqClient.Get<Hello>(QueueNames<Hello>.Dlq);
                    mqClient.Ack(dlqMsg);

                    Assert.That(called, Is.EqualTo(2));
                    Assert.That(dlqMsg.GetBody().Name, Is.EqualTo("World"));
                    Assert.That(dlqMsg.Error.ErrorCode, Is.EqualTo(typeof(ArgumentException).Name));
                    Assert.That(dlqMsg.Error.Message, Is.EqualTo("Name"));
                }
            }
        }

        [Test]
        public void Message_with_ReplyTo_are_published_to_the_ReplyTo_queue()
        {
            using (var mqServer = new RabbitMqServer())
            {
                mqServer.RegisterHandler<Hello>(m =>
                    new HelloResponse { Result = "Hello, {0}!".Fmt(m.GetBody().Name) });
                mqServer.Start();

                using (var mqClient = mqServer.CreateMessageQueueClient())
                {
                    const string replyToMq = "mq:Hello.replyto";
                    mqClient.Publish(new Message<Hello>(new Hello { Name = "World" }) {
                        ReplyTo = replyToMq
                    });

                    IMessage<HelloResponse> responseMsg = mqClient.Get<HelloResponse>(replyToMq);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().Result, Is.EqualTo("Hello, World!"));
                }
            }
        }

        [Test]
        public void Does_process_messages_in_HttpListener_AppHost()
        {
            using (var appHost = new AppHost().Init())
            {
                using (var mqClient = appHost.Resolve<IMessageService>().CreateMessageQueueClient())
                {
                    mqClient.Publish(new Hello { Name = "World" });

                    IMessage<HelloResponse> responseMsg = mqClient.Get<HelloResponse>(QueueNames<HelloResponse>.In);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().Result, Is.EqualTo("Hello, World!"));
                }
            }
        }

        [Test]
        public void Does_process_messages_in_BasicAppHost()
        {
            using (var appHost = new BasicAppHost(typeof(HelloService).Assembly)
            {
                ConfigureAppHost = host =>
                {
                    host.Container.Register<IMessageService>(c => new RabbitMqServer());

                    var mqServer = host.Container.Resolve<IMessageService>();

                    mqServer.RegisterHandler<Hello>(host.ServiceController.ExecuteMessage);
                    mqServer.Start();
                }
            }.Init())
            {
                using (var mqClient = appHost.Resolve<IMessageService>().CreateMessageQueueClient())
                {
                    mqClient.Publish(new Hello { Name = "World" });

                    IMessage<HelloResponse> responseMsg = mqClient.Get<HelloResponse>(QueueNames<HelloResponse>.In);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().Result, Is.EqualTo("Hello, World!"));
                }
            }
        }

    }
}