#if !NETCORE_SUPPORT
using Amazon.DynamoDBv2;
using ServiceStack.Configuration;

namespace ServiceStack.Common.Tests
{
    public static class DynamoConfig
    {
        public static AmazonDynamoDBClient CreateDynamoDBClient()
        {
            var dynamoClient = new AmazonDynamoDBClient("keyId", "key", new AmazonDynamoDBConfig
            {
                ServiceURL = ConfigUtils.GetAppSetting("DynamoDbUrl", "http://localhost:8000"),
            });

            return dynamoClient;
        }
    }
}
#endif