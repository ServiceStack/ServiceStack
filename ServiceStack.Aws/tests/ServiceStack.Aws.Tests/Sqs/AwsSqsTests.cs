using System.Collections.Generic;
using Amazon;
using Amazon.SQS.Model;
using NUnit.Framework;
using ServiceStack.Aws.Sqs;
using ServiceStack.Text;

namespace ServiceStack.Aws.Tests.Sqs
{
    [Explicit, Ignore("Integration Test")]
    public class AwsSqsTests
    {
        private SqsQueueManager sqsQueueManager;
        private SqsMqBufferFactory sqsMqBufferFactory;

        [OneTimeSetUp]
        public void FixtureSetup()
        {
            var sqsFactory = new SqsConnectionFactory(AwsConfig.AwsAccessKey, AwsConfig.AwsSecretKey, RegionEndpoint.USEast1);

            sqsQueueManager = new SqsQueueManager(sqsFactory) {
                DisableBuffering = true
            };

            sqsMqBufferFactory = new SqsMqBufferFactory(sqsFactory);
        }

        [OneTimeTearDown]
        public void FixtureTeardown()
        {
            if (SqsTestAssert.IsFakeClient)
                return;

            // Cleanup anything left cached that we tested with
            var queueNamesToDelete = new List<string>(sqsQueueManager.QueueNameMap.Keys);
            foreach (var queueName in queueNamesToDelete)
            {
                try
                {
                    sqsQueueManager.DeleteQueue(queueName);
                }
                catch { }
            }
        }

        [Test]
        public void Can_send_and_receive_message_with_Attributes()
        {
            var queueDef = sqsQueueManager.CreateQueue("TestMq");
            var buffer = sqsMqBufferFactory.GetOrCreate(queueDef);

            var msgBody = "Test Body";
            buffer.Send(new SendMessageRequest
            {
                QueueUrl = buffer.QueueDefinition.QueueUrl,
                MessageBody = msgBody,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    { "Custom", new MessageAttributeValue { DataType = "String", StringValue = "Header" } },
                }
            });

            var responseMsg = buffer.Receive(new ReceiveMessageRequest
            {
                QueueUrl = buffer.QueueDefinition.QueueUrl,
                MessageAttributeNames = new List<string> { "All" },
            });

            Assert.That(responseMsg.Body, Is.EqualTo(msgBody));

            Assert.That(responseMsg.MessageAttributes["Custom"].StringValue, Is.EqualTo("Header"));
        }

    }
}