using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.Model;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests
{
    [TestFixture]
    public class PocoDynamoDbTests : DynamoTestBase
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var db = CreatePocoDynamo();
            db.DeleteAllTables(TimeSpan.FromMinutes(1));
        }

        [Test]
        public void Does_Create_tables()
        {
            var db = CreatePocoDynamo();
            var types = new List<Type>()
                .Add<Customer>()
                .Add<Country>()
                .Add<Node>();

            db.RegisterTables(types);
            db.InitSchema();

            var tableNames = db.GetTableNames();

            var expected = new[] {
                "Customer",
                "Country",
                "Node",
            };
            Assert.That(expected.All(x => tableNames.Contains(x)));
        }

        [Test]
        public void Can_put_and_delete_Country_raw()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Country>();
            db.InitSchema();

            db.DynamoDb.PutItem(new PutItemRequest
            {
                TableName = nameof(Country),
                Item = new Dictionary<string, AttributeValue>
                {
                    { "Id", new AttributeValue { N = "1" } },
                    { "CountryName", new AttributeValue { S = "Australia"} },
                    { "CountryCode", new AttributeValue { S = "AU"} },
                }
            });

            var response = db.DynamoDb.GetItem(new GetItemRequest
            {
                TableName = nameof(Country),
                ConsistentRead = true,
                Key = new Dictionary<string, AttributeValue> {
                    { "Id", new AttributeValue { N = "1" } }
                }
            });

            Assert.That(response.IsItemSet);
            Assert.That(response.Item["Id"].N, Is.EqualTo("1"));
            Assert.That(response.Item["CountryName"].S, Is.EqualTo("Australia"));
            Assert.That(response.Item["CountryCode"].S, Is.EqualTo("AU"));
        }

        [Test]
        public void Can_put_and_delete_Country()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Country>();
            db.InitSchema();

            var country = new Country
            {
                Id = 2,
                CountryCode = "US",
                CountryName = "United States"
            };

            db.PutItem(country);

            var dbCountry = db.GetItem<Country>(2);

            dbCountry.PrintDump();

            Assert.That(dbCountry, Is.EqualTo(country));
        }

        [Test]
        public void Can_put_and_delete_basic_Customer_raw()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Customer>();
            db.InitSchema();

            db.DynamoDb.PutItem(new PutItemRequest
            {
                TableName = nameof(Customer),
                Item = new Dictionary<string, AttributeValue>
                {
                    { "Id", new AttributeValue { N = "2" } },
                    { "Name", new AttributeValue { S = "Foo"} },
                    { "Orders", new AttributeValue { NULL = true } },
                    { "CustomerAddress", new AttributeValue { NULL = true } },
                }
            });

            var response = db.DynamoDb.GetItem(new GetItemRequest
            {
                TableName = nameof(Customer),
                ConsistentRead = true,
                Key = new Dictionary<string, AttributeValue> {
                    { "Id", new AttributeValue { N = "2" } }
                }
            });

            Assert.That(response.IsItemSet);
            Assert.That(response.Item["Id"].N, Is.EqualTo("2"));
            Assert.That(response.Item["Name"].S, Is.EqualTo("Foo"));
            Assert.That(response.Item["Orders"].NULL);
            Assert.That(response.Item["CustomerAddress"].NULL);
        }

        [Test]
        public void Can_Put_Get_and_Delete_Customer_with_Orders()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Customer>();
            db.InitSchema();

            var customer = new Customer
            {
                Id = 11,
                Name = "Foo",
                Orders = new List<Order>
                {
                    new Order
                    {
                        Id = 21,
                        CustomerId = 11,
                        LineItem = "Item 1",
                        Qty = 3,
                        Cost = 2,
                    },
                    new Order
                    {
                        Id = 22,
                        CustomerId = 11,
                        LineItem = "Item 2",
                        Qty = 4,
                        Cost = 3,
                    },
                },
                PrimaryAddress = new CustomerAddress
                {
                    Id = 31,
                    CustomerId = 11,
                    AddressLine1 = "Line 1",
                    AddressLine2 = "Line 2",
                    City = "Darwin",
                    State = "NT",
                    Country = "AU",
                }
            };

            db.PutItem(customer);

            var dbCustomer = db.GetItem<Customer>(11);

            Assert.That(dbCustomer.Equals(customer));

            db.DeleteItem<Customer>(11);

            dbCustomer = db.GetItem<Customer>(11);

            Assert.That(dbCustomer, Is.Null);
        }

        [Test]
        public void Does_auto_populate_AutoIncrement_fields()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Customer>();
            db.InitSchema();

            db.Sequences.Reset<Customer>(10);
            db.Sequences.Reset<Order>(20);
            db.Sequences.Reset<CustomerAddress>(30);

            var customer = new Customer
            {
                Name = "Foo",
            };

            db.PutItem(customer);

            Assert.That(customer.Id, Is.EqualTo(11));

            Assert.That(db.Sequences.Current<Customer>(), Is.EqualTo(11));
            Assert.That(db.Sequences.Current<Order>(), Is.EqualTo(20));
            Assert.That(db.Sequences.Current<CustomerAddress>(), Is.EqualTo(30));

            var dbCustomer = db.GetItem<Customer>(11);
            Assert.That(dbCustomer.Id, Is.EqualTo(11));

            customer = new Customer
            {
                Name = "Foo",
                Orders = new List<Order>
                {
                    new Order
                    {
                        LineItem = "Item 1",
                        Qty = 3,
                        Cost = 2,
                    },
                    new Order
                    {
                        LineItem = "Item 2",
                        Qty = 4,
                        Cost = 3,
                    },
                },
                PrimaryAddress = new CustomerAddress
                {
                    AddressLine1 = "Line 1",
                    AddressLine2 = "Line 2",
                    City = "Darwin",
                    State = "NT",
                    Country = "AU",
                }
            };

            db.PutItem(customer);

            Assert.That(customer.Id, Is.EqualTo(12));
            Assert.That(customer.Orders[0].Id, Is.EqualTo(21));
            Assert.That(customer.Orders[1].Id, Is.EqualTo(22));
            Assert.That(customer.PrimaryAddress.Id, Is.EqualTo(31));
        }

        [Test]
        public void Can_Put_Get_and_Delete_Deeply_Nested_Nodes()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Node>();
            db.InitSchema();

            var nodes = new Node(1, "/root",
                new List<Node>
                {
                    new Node(2,"/root/2", new[] {
                        new Node(4, "/root/2/4", new [] {
                            new Node(5, "/root/2/4/5", new[] {
                                new Node(6, "/root/2/4/5/6"),
                            }),
                        }),
                    }),
                    new Node(3, "/root/3")
                });

            db.PutItem(nodes);

            var dbNodes = db.GetItem<Node>(1);

            dbNodes.PrintDump();

            Assert.That(dbNodes, Is.EqualTo(nodes));
        }

        [Test]
        public void Can_put_and_get_Collection()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Collection>();
            db.InitSchema();

            var row = new Collection
            {
                Id = 1,
            }
            .InitStrings(10.Times(i => ((char)('A' + i)).ToString()).ToArray())
            .InitInts(10.Times(i => i).ToArray());

            db.PutItem(row);

            var dbRow = db.GetItem<Collection>(1);

            dbRow.PrintDump();

            Assert.That(dbRow, Is.EqualTo(row));
        }

        [Test]
        public void Does_convert_empty_string_to_null()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Customer>();
            db.InitSchema();

            db.PutItem(new Customer { Id = 1, Name = "" });

            var customer = db.GetItem<Customer>(1);

            Assert.That(customer.Name, Is.Null);
        }

        [Test]
        public void Does_return_null_if_doesnt_exist()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Customer>();
            db.InitSchema();

            var customer = db.GetItem<Customer>(999);
            Assert.That(customer, Is.Null);
        }
    }
}