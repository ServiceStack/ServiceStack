using System;
using Amazon;
using Amazon.S3;
using ServiceStack.Aws.Support;

namespace ServiceStack.Aws.FileStorage
{
    public class S3ConnectionFactory : AwsConnectionFactory<IAmazonS3>
    {
        public S3ConnectionFactory() : base(() => new AmazonS3Client()) { }

        public S3ConnectionFactory(string awsAccessKey, string awsSecretKey, RegionEndpoint region)
            : base(() => new AmazonS3Client(awsAccessKey, awsSecretKey, region)) { }

        public S3ConnectionFactory(Func<IAmazonS3> clientFactory)
            : base(clientFactory) { }
    }
}