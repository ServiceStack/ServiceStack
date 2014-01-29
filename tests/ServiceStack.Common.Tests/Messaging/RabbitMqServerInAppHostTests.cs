using System;
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
            mqServer.RegisterHandler<ThrowGenericError>(ServiceController.ExecuteMessage);

            mqServer.Start();
        }
    }

    public class RabbitMqServerInAppHostTests : MqServerInAppHostTests
    {
        protected override void TestFixtureSetUp()
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
                channel.PurgeQueue<ThrowGenericError>();
            }
        }
    }
}