using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using NUnit.Framework;
using ServiceStack.Aws.Sqs;

namespace ServiceStack.Aws.Tests.Sqs
{
    [TestFixture]
    public class SqsMqBufferNonBufferedTests
    {
        // Mimics AWSSDK.SQS v4, which leaves Messages null when the receive returns nothing
        private class StubSqsClient : AmazonSQSClient
        {
            private readonly List<Message> messages;

            public StubSqsClient(List<Message> messages)
                : base(new AnonymousAWSCredentials(), RegionEndpoint.USEast1) => this.messages = messages;

            public override Task<ReceiveMessageResponse> ReceiveMessageAsync(ReceiveMessageRequest request,
                CancellationToken cancellationToken = default) =>
                Task.FromResult(new ReceiveMessageResponse { Messages = messages });

#if NETFRAMEWORK
            public override ReceiveMessageResponse ReceiveMessage(ReceiveMessageRequest request) =>
                new ReceiveMessageResponse { Messages = messages };
#endif
        }

        private static SqsMqBufferNonBuffered CreateBuffer(List<Message> messages) =>
            new SqsMqBufferNonBuffered(new SqsQueueDefinition { QueueUrl = "https://sqs.local/queue" },
                new SqsConnectionFactory(() => new StubSqsClient(messages)));

        [Test]
        public void Receive_returns_null_when_response_Messages_is_null()
        {
            var buffer = CreateBuffer(messages: null);

            Assert.That(buffer.Receive(new ReceiveMessageRequest()), Is.Null);
        }

        [Test]
        public void Receive_returns_null_when_response_Messages_is_empty()
        {
            var buffer = CreateBuffer(new List<Message>());

            Assert.That(buffer.Receive(new ReceiveMessageRequest()), Is.Null);
        }

        [Test]
        public void Receive_returns_the_message_when_one_is_received()
        {
            var message = new Message { MessageId = "1", Body = "body" };
            var buffer = CreateBuffer(new List<Message> { message });

            Assert.That(buffer.Receive(new ReceiveMessageRequest()), Is.SameAs(message));
        }
    }
}
