using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests.UseCases
{
    public class Todo
    {
        [AutoIncrement]
        public long Id { get; set; }
        public string Content { get; set; }
        public int Order { get; set; }
        public bool Done { get; set; }
    }

    [TestFixture]
    public class DynamoDbTodoExample : DynamoTestBase
    {
        private readonly IAmazonDynamoDB awsDb;
        private readonly IPocoDynamo db;

        public DynamoDbTodoExample()
        {
            awsDb = CreateDynamoDbClient();
            db = new PocoDynamo(awsDb);
            db.RegisterTable<Todo>();
            db.InitSchema();
        }

        [Test]
        public void Create_Table_with_PocoDynamo()
        {
            db.DeleteTable<Todo>();
            db.InitSchema();
            db.GetTableNames().PrintDump();
        }

        [Test]
        public void Create_Table_with_AWSSDK()
        {
            db.DeleteTable<Todo>();
            var request = new CreateTableRequest
            {
                TableName = "Todo",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("Id", KeyType.HASH),
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition("Id", ScalarAttributeType.N),
                },
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 10,
                    WriteCapacityUnits = 5,
                }
            };
            awsDb.CreateTable(request);

            while (true)
            {
                try
                {
                    var descResponse = awsDb.DescribeTable(new DescribeTableRequest("Todo"));
                    if (descResponse.Table.TableStatus == DynamoStatus.Active)
                        break;

                    Thread.Sleep(TimeSpan.FromSeconds(2));
                }
                catch (ResourceNotFoundException)
                {
                    // DescribeTable is eventually consistent. So you might get resource not found.
                }
            }

            var listResponse = awsDb.ListTables(new ListTablesRequest());
            var tableNames = listResponse.TableNames;
            tableNames.PrintDump();
        }

        [Test]
        public void Post_100_Todos_with_PocoDynamo()
        {
            db.Sequences.Reset<Todo>();
            db.DeleteTable<Todo>();
            db.CreateTable<Todo>();

            db.RegisterTable<Todo>();
            db.InitSchema();

            var todos = 100.Times(i => new Todo { Content = "TODO " + i, Order = i });

            db.PutItems(todos);

            db.ScanAll<Todo>()
                .OrderBy(x => x.Id)
                .PrintDump();
        }

        [Test]
        public void Post_100_Todos_with_AWSSDK()
        {
            db.Sequences.Reset<Todo>();
            db.DeleteTable<Todo>();
            db.CreateTable<Todo>();

            var incrRequest = new UpdateItemRequest
            {
                TableName = "Seq",
                Key = new Dictionary<string, AttributeValue> {
                    {"Id", new AttributeValue { S = "Todo" } }
                },
                AttributeUpdates = new Dictionary<string, AttributeValueUpdate> {
                    {
                        "Counter",
                        new AttributeValueUpdate {
                            Action = AttributeAction.ADD,
                            Value = new AttributeValue { N = 100.ToString() }
                        }
                    }
                },
                ReturnValues = ReturnValue.ALL_NEW,
            };

            var response = awsDb.UpdateItem(incrRequest);
            var nextSequences = Convert.ToInt64(response.Attributes["Counter"].N);

            for (int i = 0; i < 100; i++)
            {
                var putRequest = new PutItemRequest("Todo",
                    new Dictionary<string, AttributeValue> {
                        { "Id", new AttributeValue { N = (nextSequences - 100 + i).ToString() } },
                        { "Content", new AttributeValue("TODO " + i) },
                        { "Order", new AttributeValue { N = i.ToString() } },
                        { "Done", new AttributeValue { BOOL = false } },
                    });

                awsDb.PutItem(putRequest);
            }

            db.ScanAll<Todo>()
                .OrderBy(x => x.Id)
                .PrintDump();
        }

        [Test]
        public void Get_item_with_PocoDynamo()
        {
            db.PutItem(new Todo { Id = 1, Content = "TODO 1", Order = 1 });

            var todo = db.GetItem<Todo>(1);
            todo.PrintDump();
        }

        [Test]
        public void Get_all_items_with_PocoDynamo()
        {
            db.PutItem(new Todo { Id = 1, Content = "TODO 1", Order = 1 });

            IEnumerable<Todo> todos = db.ScanAll<Todo>();
            todos.PrintDump();

            db.GetAll<Todo>().PrintDump();
        }

        [Test]
        public void Get_item_with_AWSSDK()
        {
            db.PutItem(new Todo { Id = 1, Content = "TODO 1", Order = 1 });

            var request = new GetItemRequest
            {
                TableName = "Todo",
                Key = new Dictionary<string, AttributeValue> {
                    { "Id", new AttributeValue { N = "1"} }
                },
                ConsistentRead = true,
            };

            var response = awsDb.GetItem(request);
            var todo = new Todo
            {
                Id = Convert.ToInt64(response.Item["Id"].N),
                Content = response.Item["Content"].S,
                Order = Convert.ToInt32(response.Item["Order"].N),
                Done = response.Item["Done"].BOOL == true,
            };

            todo.PrintDump();
        }

        [Test]
        public void Get_all_items_with_AWSSDK()
        {
            db.PutItem(new Todo { Id = 1, Content = "TODO 1", Order = 1 });

            var request = new ScanRequest
            {
                TableName = "Todo",
                Limit = 1000,
            };

            var allTodos = new List<Todo>();
            ScanResponse response = null;
            do
            {
                if (response != null)
                    request.ExclusiveStartKey = response.LastEvaluatedKey;

                response = awsDb.Scan(request);

                foreach (var item in response.Items)
                {
                    var todo = new Todo
                    {
                        Id = Convert.ToInt64(item["Id"].N),
                        Content = item["Content"].S,
                        Order = Convert.ToInt32(item["Order"].N),
                        Done = item["Done"].BOOL == true,
                    };
                    allTodos.Add(todo);
                }

            } while (response.LastEvaluatedKey != null && response.LastEvaluatedKey.Count > 0);

            allTodos.PrintDump();
        }

        [Test]
        public void Delete_item_with_PocoDynamo()
        {
            db.PutItem(new Todo { Id = 1, Content = "TODO 1", Order = 1 });

            db.DeleteItem<Todo>(1);
            Assert.That(db.GetItem<Todo>(1), Is.Null);
        }

        [Test]
        public void Delete_item_with_AWSSDK()
        {
            db.PutItem(new Todo { Id = 1, Content = "TODO 1", Order = 1 });

            var request = new DeleteItemRequest
            {
                TableName = "Todo",
                Key = new Dictionary<string, AttributeValue> {
                    { "Id", new AttributeValue { N = "1"} }
                },
            };

            awsDb.DeleteItem(request);

            Assert.That(db.GetItem<Todo>(1), Is.Null);
        }

        [Test]
        public void Can_Query_Content()
        {
            db.Sequences.Reset<Todo>();
            var todos = 20.Times(i => new Todo { Content = "TODO " + i, Order = i });
            db.PutItems(todos);

            var response = db.FromQuery<Todo>()
                    .KeyCondition(x => x.Id == 1)
                    .Filter(x => x.Content.StartsWith("TODO"))
                .Exec().ToList();

            Assert.That(response, Is.Not.Null);

            response = db.FromQuery<Todo>(x => x.Id == 1)
                .Filter(x => x.Content.StartsWith("TODO"))
                .Exec()
                .ToList();

            Assert.That(response, Is.Not.Null);

            var q = db.FromQuery<Todo>()
                .KeyCondition(x => x.Id == 1)
                .Filter(x => x.Done);

            var isDone = q
                .Exec()
                .FirstOrDefault();

            Assert.That(isDone, Is.Null);

            var q2 = db.FromQuery<Todo>(x => x.Id == 1);
            q2.Filter(x => x.Done);
            var todo1Done = db.Query(q2).FirstOrDefault();

            Assert.That(todo1Done, Is.Null);

            db.PutItem(new Todo { Id = 1, Content = "Updated", Done = true });

            isDone = q
                .Exec()
                .FirstOrDefault();

            Assert.That(isDone, Is.Not.Null);
        }
    }
}