using NUnit.Framework;
using ServiceStack.Messaging;
using ServiceStack.Server.Tests.Services;

namespace ServiceStack.Server.Tests.Messaging
{
    [TestFixture, Category("Integration")]
    public class RedisMqHostSupportTests
    {
        [Test]
        public void Does_serialize_to_correct_MQ_name()
        {
            var message = new Message<Greet>(new Greet {Name = "Test"}) {};

            var mqClient = new RedisMessageQueueClient(TestConfig.BasicClientManger);

            mqClient.Publish(message);
        }
    }
}