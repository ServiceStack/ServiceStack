using System.Runtime.Serialization;

namespace ServiceStack.Aws.Sqs
{
    [DataContract]
    public class SqsRedrivePolicy
    {
        [DataMember(Name="maxReceiveCount")] //Valid Range 1-1000
        public int MaxReceiveCount { get; set; }

        [DataMember(Name = "deadLetterTargetArn")]
        public string DeadLetterTargetArn { get; set; }
    }
}
