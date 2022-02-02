using System;
using Amazon;
using Amazon.SQS;
using ServiceStack.Aws.Support;

namespace ServiceStack.Aws.Sqs
{
    public class SqsConnectionFactory : AwsConnectionFactory<IAmazonSQS>
    {
        public SqsConnectionFactory() : base(() => new AmazonSQSClient()) { }

        public SqsConnectionFactory(string awsAccessKey, string awsSecretKey, RegionEndpoint region)
            : base(() => new AmazonSQSClient(awsAccessKey, awsSecretKey, region)) { }

        public SqsConnectionFactory(Func<IAmazonSQS> clientFactory)
            : base(clientFactory) { }
    }
}