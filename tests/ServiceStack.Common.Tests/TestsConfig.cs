using System;
using Amazon.DynamoDBv2;

namespace ServiceStack.Common.Tests
{
    public class TestsConfig
    {
        public static readonly string RabbitMqHost = Environment.GetEnvironmentVariable("CI_RABBITMQ") ?? "localhost";
        
        public static AmazonDynamoDBClient CreateDynamoDBClient()
        {
            var dynamoClient = new AmazonDynamoDBClient("keyId", "key", new AmazonDynamoDBConfig
            {
                ServiceURL = Environment.GetEnvironmentVariable("CI_DYNAMODB") ?? "http://localhost:8000",
            });

            return dynamoClient;
        }
    }
}