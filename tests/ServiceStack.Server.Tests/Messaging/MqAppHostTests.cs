using System;
using System.Threading;
using Funq;
using NUnit.Framework;
using ServiceStack.Messaging;
using ServiceStack.RabbitMq;
using ServiceStack.Server.Tests.Properties;

namespace ServiceStack.Server.Tests.Messaging
{
    public class MqAppHost : AppSelfHostBase
    {
        public MqAppHost()
            : base(typeof(MqAppHost).Name, typeof(MqAppHostServices).Assembly) {}

        public override void Configure(Container container)
        {
            var mqServer = new RabbitMqServer();

            mqServer.RegisterHandler<MqCustomException>(
                ServiceController.ExecuteMessage,
                HandleMqCustomException);

            container.Register<IMessageService>(c => mqServer);
            mqServer.Start();
        }

        public CustomException LastCustomException;

        public void HandleMqCustomException(IMessageHandler mqHandler, IMessage<MqCustomException> message, Exception ex)
        {
            LastCustomException = ex.InnerException as CustomException;

            bool requeue = !(ex is UnRetryableMessagingException)
                && message.RetryAttempts < 1;

            if (requeue)
            {
                message.RetryAttempts++;
            }

            message.Error = ex.ToResponseStatus();
            mqHandler.MqClient.Nak(message, requeue: requeue, exception: ex);
        }
    }

    public class CustomException : Exception
    {
        public CustomException() {}
        public CustomException(string message) : base(message) {}
        public CustomException(string message, Exception innerException) : base(message, innerException) {}
    }

    public class MqCustomException
    {
        public string Message { get; set; }
    }

    public class MqAppHostServices : Service
    {
        public static int TimesCalled = 0;

        public object Any(MqCustomException request)
        {
            TimesCalled++;
            throw new CustomException("ERROR: " + request.Message);
        }
    }

    public class MqAppHostTests
    {
        private readonly MqAppHost appHost;

        public MqAppHostTests()
        {
            this.appHost = new MqAppHost();
            appHost
                .Init()
                .Start(Config.ListeningOn);
    }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_handle_custom_exception()
        {
            MqAppHostServices.TimesCalled = 0;

            using (var mqClient = appHost.TryResolve<IMessageService>().CreateMessageQueueClient())
            {
                mqClient.Publish(new MqCustomException { Message = "foo" });

                Thread.Sleep(1000);

                Assert.That(MqAppHostServices.TimesCalled, Is.EqualTo(2));
                Assert.That(appHost.LastCustomException.Message, Is.EqualTo("ERROR: foo"));
            }
        }
    }
}