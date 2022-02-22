using System;
using Amazon;
using Amazon.S3;
using ServiceStack.Configuration;

namespace ServiceStack.Aws.Tests
{
    public class AwsConfig
    {
        public static string AwsAccessKey
        {
            get
            {
                var accessKey = ConfigUtils.GetNullableAppSetting("AWS_ACCESS_KEY")
                    ?? Environment.GetEnvironmentVariable("AWS_ACCESS_KEY");

                if (string.IsNullOrEmpty(accessKey))
                    throw new ArgumentException("AWS_ACCESS_KEY must be defined in App.config or Environment Variable");

                return accessKey;
            }
        }

        public static string AwsSecretKey
        {
            get
            {
                var secretKey = ConfigUtils.GetNullableAppSetting("AWS_SECRET_KEY")
                    ?? Environment.GetEnvironmentVariable("AWS_SECRET_KEY");

                if (string.IsNullOrEmpty(secretKey))
                    throw new ArgumentException("AWS_SECRET_KEY must be defined in App.config or Environment Variable");

                return secretKey;
            }
        }

        public static AmazonS3Client CreateAmazonS3Client()
        {
            return new AmazonS3Client(
                Environment.GetEnvironmentVariable("AWS_S3_ACCESS_KEY") ?? AwsAccessKey, 
                Environment.GetEnvironmentVariable("AWS_S3_SECRET_KEY") ?? AwsSecretKey, 
                RegionEndpoint.USEast1);
        }
    }
}