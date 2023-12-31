using System;
using Amazon.DynamoDBv2;

namespace ServiceStack.Common.Tests;

public class TestsConfig
{
    public static readonly string RabbitMqHost = Environment.GetEnvironmentVariable("CI_RABBITMQ") ?? "localhost";
        
    public static readonly string SqlServerConnString = Environment.GetEnvironmentVariable("MSSQL_CONNECTION") ?? "Server=localhost;Database=test;User Id=test;Password=test;";
    public static readonly string PostgreSqlConnString = Environment.GetEnvironmentVariable("PGSQL_CONNECTION") ?? "Server=localhost;Port=5432;User Id=test;Password=test;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200";
        
    public static AmazonDynamoDBClient CreateDynamoDBClient()
    {
        var dynamoClient = new AmazonDynamoDBClient("keyId", "key", new AmazonDynamoDBConfig
        {
            ServiceURL = Environment.GetEnvironmentVariable("CI_DYNAMODB") ?? "http://localhost:8000",
        });

        return dynamoClient;
    }
}