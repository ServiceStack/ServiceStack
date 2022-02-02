using System;
using System.Threading;
using Amazon.SQS;
using NUnit.Framework;
using ServiceStack.Aws.Sqs.Fake;
using ServiceStack.Text;

namespace ServiceStack.Aws.Tests.Sqs
{
    public static class SqsTestAssert
    {
        // SQS is non-deterministic in some operations (for example receiving a certain number of items in batch recive request, there
        // is no guarantee it will return that number of items depending on sizes of queues, nodes available, etc.)  So, for testing,
        // we can use the Fake SQS helper to get deterministic results, but when testing on a real SQS insatnce nothing is 
        // guaranteed there, so we have to either not run the test, or assert it a bit differently

        // Reading on this:
        //  http://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-batch-api.html
        //  http://docs.aws.amazon.com/sdkfornet/latest/apidocs/items/MSQS_SQSReceiveMessage_ReceiveMessageRequestNET4_5.html

        private static bool? isFakeClient;
        public static bool IsFakeClient
        {
            get
            {
                if (isFakeClient.HasValue)
                    return isFakeClient.Value;

                var fakeClient = SqsTestClientFactory.GetClient() as FakeAmazonSqs;

                isFakeClient = fakeClient != null;
                return isFakeClient.Value;
            }
        }

        public static void FakeEqualRealGreater(int fakeExpected, int realMinimum, int compareTo)
        {
            if (IsFakeClient)
            {
                Assert.AreEqual(fakeExpected, compareTo);
            }
            else
            {
                Assert.Greater(compareTo, realMinimum);
            }
        }

        public static void Throws<T>(TestDelegate code, string realMsgContains = null)
            where T : Exception
        {
            if (IsFakeClient)
            {
                Assert.Throws<T>(code);
            }
            else
            {
                // With real, it sometimes throws a strongly-typed exception, other times a generic exception with a pattern
                try
                {
                    code();
                }
                catch(T)
                {
                    return;
                }
                catch (AmazonSQSException sqsex)
                {
                    if (!string.IsNullOrEmpty(realMsgContains))
                    {
                        Assert.IsTrue(sqsex.Message.ToLowerInvariant().Contains(realMsgContains.ToLowerInvariant()));
                    }

                    return;
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Real SQS client - expected to throw typed or SQS exception, but it threw a different one. Exception type: [{ex.GetType()}], message [{ex.Message}]");
                }

                Assert.Fail("Real SQS client - expected to throw exception, but it did not throw one.");
            }

        }

        public static void WaitUntilTrueOrTimeout(Func<bool> doneWhenTrue, int timeoutSeconds = 10)
        {
            var timeoutAt = DateTime.UtcNow.AddSeconds(timeoutSeconds);

            "Waiting for max of {0}s for processing".Print(timeoutSeconds);

            while (DateTime.UtcNow <= timeoutAt)
            {
                if (doneWhenTrue())
                {
                    break;
                }

                Thread.Sleep(300);
            }
        }

    }
}