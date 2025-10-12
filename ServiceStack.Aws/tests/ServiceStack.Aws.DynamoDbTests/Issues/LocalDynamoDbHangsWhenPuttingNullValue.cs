using System;
using System.Collections.Generic;
using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using NUnit.Framework;

namespace ServiceStack.Aws.DynamoDbTests.Issues
{
    [TestFixture,Explicit]
    public class LocalDynamoDbHangsWhenPuttingNullValue
    {
        [Test]
        public void Can_put_item_with_null_value()
        {
            var db = new AmazonDynamoDBClient("keyId", "key", new AmazonDynamoDBConfig
            {
                ServiceURL = DynamoTestBase.DynamoDbUrl,
            });

            try
            {
                db.DeleteTable(new DeleteTableRequest
                {
                    TableName = "Test",
                });

                Thread.Sleep(1000);
            }
            catch (Exception) { /*ignore*/ }

            db.CreateTable(new CreateTableRequest
            {
                TableName = "Test",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement
                    {
                        AttributeName = "Id",
                        KeyType = "HASH"
                    }
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition
                    {
                        AttributeName = "Id",
                        AttributeType = "N",
                    }
                },
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 10,
                    WriteCapacityUnits = 5,
                }
            });

            Thread.Sleep(1000);

            db.PutItem(new PutItemRequest
            {
                TableName = "Test",
                Item = new Dictionary<string, AttributeValue>
                {
                    { "Id", new AttributeValue { N = "1" } },
                    { "Name", new AttributeValue { S = "Foo"} },
                    { "Empty", new AttributeValue { NULL = true } },
                }
            });

            var response = db.GetItem(new GetItemRequest
            {
                TableName = "Test",
                ConsistentRead = true,
                Key = new Dictionary<string, AttributeValue> {
                    { "Id", new AttributeValue { N = "1" } }
                }
            });

            Assert.That(response.IsItemSet);
            Assert.That(response.Item["Id"].N, Is.EqualTo("1"));
            Assert.That(response.Item["Name"].S, Is.EqualTo("Foo"));
            Assert.That(response.Item["Empty"].NULL == true);
        }
    }
}