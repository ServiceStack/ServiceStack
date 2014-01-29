using Funq;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Messaging;
using ServiceStack.Validation;

namespace ServiceStack.Common.Tests.Messaging
{
    public class InMemoryMqAppHost : AppHostHttpListenerBase
    {
        public InMemoryMqAppHost()
            : base("Service Name", typeof(AnyTestMq).Assembly) { }

        public override void Configure(Container container)
        {
            Plugins.Add(new ValidationFeature());
            container.RegisterValidators(typeof(ValidateTestMqValidator).Assembly);

            var appSettings = new AppSettings();
            container.Register<IMessageService>(c => new InMemoryTransientMessageService());

            var mqServer = (InMemoryTransientMessageService)container.Resolve<IMessageService>();
            mqServer.RegisterHandler<AnyTestMq>(ServiceController.ExecuteMessage);
            mqServer.RegisterHandler<PostTestMq>(ServiceController.ExecuteMessage);
            mqServer.RegisterHandler<ValidateTestMq>(ServiceController.ExecuteMessage);
            mqServer.RegisterHandler<ThrowGenericError>(ServiceController.ExecuteMessage);

            mqServer.Start();
        }
    }

    [TestFixture]
    public class InMemoryMqServerInAppHostTests : MqServerInAppHostTests
    {
        protected override void TestFixtureSetUp()
        {
            appHost = new InMemoryMqAppHost()
                .Init()
                .Start(ListeningOn);
        }
    }
}