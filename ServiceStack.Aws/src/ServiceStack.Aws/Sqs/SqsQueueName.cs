using System;
using System.Collections.Concurrent;

namespace ServiceStack.Aws.Sqs
{
    public static class SqsQueueNames
    {
        private static readonly ConcurrentDictionary<string, SqsQueueName> queueNameMap = new ConcurrentDictionary<string, SqsQueueName>();

        public static SqsQueueName GetSqsQueueName(string originalQueueName)
        {
            if (queueNameMap.TryGetValue(originalQueueName, out var sqn))
                return sqn;

            sqn = new SqsQueueName(originalQueueName);

            return queueNameMap.TryAdd(originalQueueName, sqn)
                ? sqn
                : queueNameMap[originalQueueName];
        }
    }

    public class SqsQueueName : IEquatable<SqsQueueName>
    {
        public SqsQueueName(string originalQueueName, string awsQueueAccountId = null)
        {
            QueueName = originalQueueName;
            AwsQueueName = originalQueueName.ToValidQueueName();
            AwsQueueAccountId = awsQueueAccountId;
        }

        public string QueueName { get; }
        public string AwsQueueName { get; private set; }
        public string AwsQueueAccountId { get; set; }

        public bool Equals(SqsQueueName other)
        {
            return other != null &&
                   QueueName.Equals(other.QueueName, StringComparison.OrdinalIgnoreCase);
        }
        
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            return obj is SqsQueueName asQueueName && Equals(asQueueName);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return QueueName;
        }
    }
}