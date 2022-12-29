using NUnit.Framework;
using ServiceStack.Messaging;
using ServiceStack.Messaging.Redis;
using ServiceStack.RabbitMq;
using ServiceStack.Redis;

namespace ServiceStack.Server.Tests.Messaging
{
    public abstract class MqServerQueueNameTests
    {
        public abstract IMessageService CreateMqServer(int retryCount = 1);

        [Test]
        public void Can_Send_and_Receive_messages_using_QueueNamePrefix()
        {
            QueueNames.SetQueuePrefix("site1.");

            Assert.That(QueueNames.TopicIn, Is.EqualTo("site1.mq:topic:in"));
            Assert.That(QueueNames.TopicOut, Is.EqualTo("site1.mq:topic:out"));

            using (var mqServer = CreateMqServer())
            {
                mqServer.RegisterHandler<HelloIntro>(m =>
                    new HelloIntroResponse { Result = $"Hello, {m.GetBody().Name}!" });
                mqServer.Start();

                using (var mqClient = mqServer.CreateMessageQueueClient())
                {
                    var request = new HelloIntro { Name = "World" };
                    var requestInq = MessageFactory.Create(request).ToInQueueName();
                    Assert.That(requestInq, Is.EqualTo("site1.mq:HelloIntro.inq"));

                    mqClient.Publish(request);

                    var responseInq = QueueNames<HelloIntroResponse>.In;
                    Assert.That(responseInq, Is.EqualTo("site1.mq:HelloIntroResponse.inq"));

                    IMessage<HelloIntroResponse> responseMsg = mqClient.Get<HelloIntroResponse>(responseInq);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().Result, Is.EqualTo("Hello, World!"));
                }
            }

            QueueNames.SetQueuePrefix("");
        }
    }

    class RedisMqServerQueueNameTests : MqServerQueueNameTests
    {
        public override IMessageService CreateMqServer(int retryCount = 1)
        {
            var redisManager = new BasicRedisClientManager();
            using (var redis = redisManager.GetClient())
            {
                redis.FlushAll();
            }
            return new RedisMqServer(redisManager) { RetryCount = retryCount };
        }
    }

    class RabbitMqServerQueueNameTests : MqServerQueueNameTests
    {
        public override IMessageService CreateMqServer(int retryCount = 1)
        {
            return new RabbitMqServer(connectionString: Config.RabbitMQConnString) { RetryCount = retryCount };
        }
    }
}
