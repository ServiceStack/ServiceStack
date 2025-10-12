using System;
using System.Collections.Generic;
using Amazon.SQS.Model;
using ServiceStack.Text;

namespace ServiceStack.Aws.Sqs.Fake
{
    public class FakeSqsQueueItem
    {
        public FakeSqsQueueItem()
        {
            MessageId = Guid.NewGuid().ToString("N");
            ReceiptHandle = Guid.NewGuid().ToString("N");
            Attributes = new Dictionary<string, string>();
            MessageAttributes = new Dictionary<string, MessageAttributeValue>();
        }

        public string MessageId { get; private set; }
        public string ReceiptHandle { get; private set; }

        public string Body { get; set; }
        public FakeSqsItemStatus Status { get; set; }
        public long? InFlightUntil { get; set; }

        public Dictionary<string, string> Attributes { get; set; }

        public Dictionary<string, MessageAttributeValue> MessageAttributes { get; set; }

        public FakeSqsItemStatus GetStatus()
        {
            if (Status == FakeSqsItemStatus.InFlight &&
                DateTime.UtcNow.ToUnixTime() >= InFlightUntil)
            {
                Status = FakeSqsItemStatus.Queued;
            }
            return Status;
        }

    }
}