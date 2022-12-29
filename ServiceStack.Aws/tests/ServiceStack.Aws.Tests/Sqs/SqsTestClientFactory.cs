using Amazon;
using Amazon.SQS;
using ServiceStack.Aws.Sqs;
using ServiceStack.Aws.Sqs.Fake;

namespace ServiceStack.Aws.Tests.Sqs
{
    public static class SqsTestClientFactory
    {
        //This applies to all tests.
        public static IAmazonSQS GetClient()
        {
            //Uncomment line below to test against Fake instance. 
            return FakeAmazonSqs.Instance;

            //To run against real AWS SQS, uncomment below and add AWS_ACCESS_KEY and AWS_SECRET_KEY Environment variables
            //return new AmazonSQSClient(AwsConfig.AwsAccessKey, AwsConfig.AwsSecretKey, RegionEndpoint.USEast1);
        }

        public static SqsConnectionFactory GetConnectionFactory()
        {
            return new SqsConnectionFactory(GetClient);
        }

    }
}