using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using NUnit.Framework;
using ServiceStack.Aws.Sqs;

namespace ServiceStack.Aws.Tests.Sqs;

[TestFixture]
public class SqsMqBufferNonBufferedTests
{
    // Mimics AWSSDK.SQS v4, which leaves Messages null when the receive returns nothing
    private class StubSqsClient : AmazonSQSClient
    {
        public StubSqsClient()
            : base(new AnonymousAWSCredentials(), RegionEndpoint.USEast1) { }

        public List<Message> Messages { get; set; }

        public ReceiveMessageRequest LastRequest { get; private set; }

        public override Task<ReceiveMessageResponse> ReceiveMessageAsync(ReceiveMessageRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Receive(request));

#if NETFRAMEWORK
        public override ReceiveMessageResponse ReceiveMessage(ReceiveMessageRequest request) =>
            Receive(request);
#endif

        private ReceiveMessageResponse Receive(ReceiveMessageRequest request)
        {
            LastRequest = request;
            return new ReceiveMessageResponse { Messages = Messages };
        }
    }

    private StubSqsClient client;
    private SqsMqBufferNonBuffered buffer;

    [SetUp]
    public void SetUp()
    {
        client = new StubSqsClient();
        buffer = new SqsMqBufferNonBuffered(new SqsQueueDefinition { QueueUrl = "https://sqs.local/queue" },
            new SqsConnectionFactory(() => client));
    }

    [TearDown]
    public void TearDown() => buffer.Dispose();

    [Test]
    public void Receive_returns_null_when_response_Messages_is_null()
    {
        client.Messages = null;

        Assert.That(buffer.Receive(new ReceiveMessageRequest()), Is.Null);
    }

    [Test]
    public void Receive_returns_null_when_response_Messages_is_empty()
    {
        client.Messages = new List<Message>();

        Assert.That(buffer.Receive(new ReceiveMessageRequest()), Is.Null);
    }

    [Test]
    public void Receive_returns_the_received_message()
    {
        var message = new Message { MessageId = "1", Body = "body" };
        client.Messages = new List<Message> { message };

        Assert.That(buffer.Receive(new ReceiveMessageRequest()), Is.SameAs(message));
    }

    [Test]
    public void Receive_forces_MaxNumberOfMessages_to_1()
    {
        client.Messages = new List<Message>();

        buffer.Receive(new ReceiveMessageRequest { MaxNumberOfMessages = 10 });

        Assert.That(client.LastRequest.MaxNumberOfMessages, Is.EqualTo(1));
    }
}
