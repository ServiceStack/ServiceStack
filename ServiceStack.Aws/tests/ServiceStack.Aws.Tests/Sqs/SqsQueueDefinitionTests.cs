using System;
using System.Collections.Generic;
using System.Globalization;
using Amazon.SQS;
using NUnit.Framework;
using ServiceStack.Aws.Sqs;
using ServiceStack.Text;

namespace ServiceStack.Aws.Tests.Sqs
{
    [TestFixture]
    public class SqsQueueDefinitionTests
    {
        [Test]
        public void Valid_queue_names_are_not_modified()
        {
            var validQueueNames = new List<string>
            {
                "valid123queue-name",
                "valid123queue_name",
                "validname",
                "123-_"
            };

            foreach (var qn in validQueueNames)
            {
                Assert.AreEqual(qn, qn.ToValidQueueName(), $"This string failed [{qn}]");
            }
        }

        [Test]
        public void Invalid_queue_names_are_modified()
        {
            var validQueueNames = new List<string>
            {
                "invalid123queue.name",
                "invalid123queue:name",
                "invalidname:",
                "also.invalid:still",
                "invalid as well"
            };

            foreach (var qn in validQueueNames)
            {
                Assert.AreEqual(qn.Replace(".", "-").Replace(":", "-").Replace(" ", "-"), 
                    qn.ToValidQueueName(),
                    $"This string failed [{qn}]");
            }
        }

        [Test]
        public void To_qd_returns_correct_input_values()
        {
            var createdDt = DateTime.UtcNow.ToUnixTime();
            var rdPolicy = new SqsRedrivePolicy
            {
                DeadLetterTargetArn = "http://test.dlq.targetarn.com",
                MaxReceiveCount = 11
            };
            var qn = new SqsQueueName(Guid.NewGuid().ToString("N"));

            var attributes = new Dictionary<string, string>
            {
                { QueueAttributeName.VisibilityTimeout, "17" },
                { QueueAttributeName.ReceiveMessageWaitTimeSeconds, "16" },
                { QueueAttributeName.CreatedTimestamp, createdDt.ToString(CultureInfo.InvariantCulture) },
                { QueueAttributeName.ApproximateNumberOfMessages, "123456789" },
                { QueueAttributeName.QueueArn, "http://test.queue.arn.com" },
                { QueueAttributeName.RedrivePolicy, rdPolicy.ToJson() }
            };

            var qd = attributes.ToQueueDefinition(qn, "testQueueUrl", disableBuffering: true);

            Assert.IsTrue(ReferenceEquals(qd.SqsQueueName, qn), "SqsQueueName");
            Assert.AreEqual(qd.QueueUrl, "testQueueUrl", "QueueUrl");
            Assert.AreEqual(qd.QueueArn, "http://test.queue.arn.com", "QueueArn");
            Assert.AreEqual(qd.ApproximateNumberOfMessages, 123456789, "ApproxNumMessages");
            Assert.AreEqual(qd.VisibilityTimeout, 17, "VisibilityTimeout");
            Assert.AreEqual(qd.ReceiveWaitTime, 16, "ReceiveWaitTime");
            Assert.AreEqual(qd.RedrivePolicy.ToJson(), rdPolicy.ToJson(), "RedrivePolicy");
            Assert.AreEqual(qd.CreatedTimestamp, createdDt, "CreatedTimestamp");
            Assert.IsTrue(qd.DisableBuffering, "DisableBuffering");
        }

        [Test]
        public void To_qd_uses_defaults_without_input_values()
        {
            var attributes = new Dictionary<string, string>();
            var qn = new SqsQueueName(Guid.NewGuid().ToString("N"));
            var startedAtUtc = DateTime.UtcNow.ToUnixTime();

            var qd = attributes.ToQueueDefinition(qn, "testQueueUrl", disableBuffering: false);

            Assert.That(qd.CreatedTimestamp >= startedAtUtc && qd.CreatedTimestamp <= DateTime.UtcNow.ToUnixTime(), "CreatedTimestamp");
            Assert.IsTrue(ReferenceEquals(qd.SqsQueueName, qn), "SqsQueueName");
            Assert.AreEqual(qd.QueueUrl, "testQueueUrl", "QueueUrl");
            Assert.IsFalse(qd.DisableBuffering, "DisableBuffering");

            Assert.AreEqual(qd.VisibilityTimeout, SqsQueueDefinition.DefaultVisibilityTimeoutSeconds, "VisibilityTimeout");
            Assert.AreEqual(qd.ReceiveWaitTime, SqsQueueDefinition.DefaultWaitTimeSeconds, "ReceiveWaitTime");
            Assert.AreEqual(qd.ApproximateNumberOfMessages, 0, "ApproxNumMessages");
            Assert.IsNull(qd.QueueArn, "QueueArn");
            Assert.IsNull(qd.RedrivePolicy, "RedrivePolicy");
        }

    }
}