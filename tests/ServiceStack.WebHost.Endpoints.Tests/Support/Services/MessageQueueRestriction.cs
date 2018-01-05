using NUnit.Framework;
using ServiceStack.Messaging;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
    [Restrict(RequestAttributes.MessageQueue)]
    public class MessageQueueRestriction : IReturn<MessageQueueRestriction>
    {
        public int Id { get; set; }
    }

    public class MessageQueueRestrictionService : Service
    {
        public object Any(MessageQueueRestriction request) => request;
    }

    [TestFixture]
    public class MessageQueueRestrictionTests
    {
        [Test]
        public void Can_access_MQ_Restriction_when_using_ExecuteMessage()
        {
            using (var appHost = new BasicAppHost(typeof(MessageQueueRestrictionService).Assembly).Init())
            {
                var request = new MessageQueueRestriction { Id = 1 };
                var response = appHost.ExecuteMessage(new Message<MessageQueueRestriction>(request));
                Assert.That(((MessageQueueRestriction)response).Id, Is.EqualTo(1));
            }
        }
    }
}