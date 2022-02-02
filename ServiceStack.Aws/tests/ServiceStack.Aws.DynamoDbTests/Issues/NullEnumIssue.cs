using System;
using System.Linq;
using Amazon.DynamoDBv2;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;

namespace ServiceStack.Aws.DynamoDbTests.Issues
{
    public enum DurationType : byte
    {
        Months = 2
    }

    public class TestNullEnum
    {
        public string Id { get; set; }
        public DurationType? DurationType { get; set; }
    }

    public class NullEnumIssue
    {
        [Test]
        public void Can_store_nullable_enums()
        {
            var awsDb = new AmazonDynamoDBClient("keyId", "key", new AmazonDynamoDBConfig {
                ServiceURL = DynamoTestBase.DynamoDbUrl,
            });
            var db = new PocoDynamo(awsDb);

            db.DeleteTable<TestNullEnum>();
            db.CreateTable<TestNullEnum>();

            db.PutItem(new TestNullEnum {
                Id = "A",
                DurationType = DurationType.Months,
            });
            
            db.PutItem(new TestNullEnum {
                Id = "B",
                DurationType = null,
            });

            var results = db.ScanAll<TestNullEnum>().ToList();

            Assert.That(results.Count, Is.EqualTo(2));
        }
    }
}