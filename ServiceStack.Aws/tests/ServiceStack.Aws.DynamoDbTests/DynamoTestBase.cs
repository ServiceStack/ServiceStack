using System;
using System.Collections.Generic;
using Amazon;
using Amazon.DynamoDBv2;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;
using ServiceStack.Caching;
using ServiceStack.Configuration;

namespace ServiceStack.Aws.DynamoDbTests
{
    public abstract class DynamoTestBase
    {
        //Run ./build/start-local-dynamodb.bat to start local DynamoDB instance on 8000
        public static bool UseLocalDb = true;

        public static IPocoDynamo CreatePocoDynamo()
        {
            var dynamoClient = CreateDynamoDbClient();

            var db = new PocoDynamo(dynamoClient);
            return db;
        }

        public static string DynamoDbUrl = Environment.GetEnvironmentVariable("CI_DYNAMODB") 
            ?? ConfigUtils.GetAppSetting("DynamoDbUrl", "http://localhost:8000");

        public static ICacheClient CreateCacheClient()
        {
            var cache = new DynamoDbCacheClient(CreatePocoDynamo());
            cache.InitSchema();
            return cache;
        }

        public static AmazonDynamoDBClient CreateDynamoDbClient()
        {
            var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY");
            var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_KEY");

            var useLocalDb = UseLocalDb || 
                string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey);

            var dynamoClient = useLocalDb
                ? new AmazonDynamoDBClient("keyId", "key", new AmazonDynamoDBConfig {
                    ServiceURL = DynamoDbUrl,
                })
                : new AmazonDynamoDBClient(accessKey, secretKey, RegionEndpoint.USEast1);
            return dynamoClient;
        }

        public static List<Poco> PutPocoItems(IPocoDynamo db, int count = 10)
        {
            db.RegisterTable<Poco>();
            db.InitSchema();

            var items = count.Times(i => new Poco { Id = i + 1, Title = "Name " + i + 1 });

            db.PutItems(items);
            return items;
        }

        public static List<RangeTest> PutRangeTests(IPocoDynamo db, int count = 10)
        {
            db.RegisterTable<RangeTest>();
            db.InitSchema();

            var items =
                count.Times(
                    i =>
                        new RangeTest
                        {
                            CreatedDate = DateTime.UtcNow,
                            Data = "Test Range",                            
                            Id = (i + 1).ToString()
                        });
            db.PutItems(items);
            return items;
        } 

        protected void AssertIndex(DynamoIndex index, string indexName, string hashField, string rangeField = null)
        {
            Assert.That(index.Name, Is.EqualTo(indexName));
            Assert.That(index.HashKey.Name, Is.EqualTo(hashField));

            if (rangeField == null)
                Assert.That(index.RangeKey, Is.Null);
            else
                Assert.That(index.RangeKey.Name, Is.EquivalentTo(rangeField));
        }

        protected DynamoMetadataType AssertTable(IPocoDynamo db, Type type, string hashField, string rangeField = null)
        {
            var table = db.GetTableMetadata(type);

            Assert.That(table.HashKey.Name, Is.EquivalentTo(hashField));

            if (rangeField == null)
                Assert.That(table.RangeKey, Is.Null);
            else
                Assert.That(table.RangeKey.Name, Is.EquivalentTo(rangeField));

            return table;
        }
    }
}