using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests
{
    public class PocoDynamoUpdateTestsAsync : DynamoTestBase
    {
        private static Customer CreateCustomer()
        {
            var customer = new Customer
            {
                Name = "Foo",
                Age = 27,
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
            return customer;
        }


        [Test]
        public async Task Can_UpdateItemNonDefaults_Partial_Customer()
        {
            var db = CreatePocoDynamo()
                .RegisterTable<Customer>();

            await db.DeleteTableAsync<Customer>();
            await db.InitSchemaAsync();

            var customer = CreateCustomer();

            await db.PutItemAsync(customer);

            var row = await db.UpdateItemNonDefaultsAsync(new Customer { Id = customer.Id, Age = 42 });
            row.PrintDump();

            var updatedCustomer = await db.GetItemAsync<Customer>(customer.Id);

            Assert.That(updatedCustomer.Age, Is.EqualTo(42));
            Assert.That(updatedCustomer.Name, Is.EqualTo(customer.Name));
            Assert.That(updatedCustomer.PrimaryAddress, Is.EqualTo(customer.PrimaryAddress));
            Assert.That(updatedCustomer.Orders, Is.EquivalentTo(customer.Orders));
        }

        [Test]
        public async Task Can_partial_UpdateItem_with_Dictionary()
        {
            var db = CreatePocoDynamo()
                .RegisterTable<Customer>();

            await db.DeleteTableAsync<Customer>();
            await db.InitSchemaAsync();

            var customer = CreateCustomer();

            await db.PutItemAsync(customer);

            await db.UpdateItemAsync<Customer>(new DynamoUpdateItem
            {
                Hash = customer.Id,
                Put = new Dictionary<string, object>
                {
                    { "Nationality", "Australian" },
                },
                Add = new Dictionary<string, object>
                {
                    { "Age", -1 }
                },
                Delete = new[] { "Name", "Orders" },
            });

            var updatedCustomer = await db.GetItemAsync<Customer>(customer.Id);

            Assert.That(updatedCustomer.Age, Is.EqualTo(customer.Age - 1));
            Assert.That(updatedCustomer.Name, Is.Null);
            Assert.That(updatedCustomer.Nationality, Is.EqualTo("Australian"));
            Assert.That(updatedCustomer.PrimaryAddress, Is.EqualTo(customer.PrimaryAddress));
            Assert.That(updatedCustomer.Orders, Is.Null);
        }

        [Test]
        public async Task TypedApi_does_populate_DynamoUpdateItem()
        {
            var db = CreatePocoDynamo()
                .RegisterTable<Customer>();

            await db.DeleteTableAsync<Customer>();
            await db.InitSchemaAsync();

            var customer = CreateCustomer();

            await db.PutItemAsync(customer);

            var decrBy = -1;
            await db.UpdateItemAsync(customer.Id, 
                put: () => new Customer {
                    Nationality = "Australian"
                },
                add: () => new Customer {
                    Age = decrBy
                },
                delete: x => new { x.Name, x.Orders });


            var updatedCustomer = await db.GetItemAsync<Customer>(customer.Id);

            Assert.That(updatedCustomer.Age, Is.EqualTo(customer.Age - 1));
            Assert.That(updatedCustomer.Name, Is.Null);
            Assert.That(updatedCustomer.Nationality, Is.EqualTo("Australian"));
            Assert.That(updatedCustomer.PrimaryAddress, Is.EqualTo(customer.PrimaryAddress));
            Assert.That(updatedCustomer.Orders, Is.Null);
        }

        [Test]
        public async Task Can_SET_ADD_REMOVE_with_conditional_expression_using_UpdateExpression()
        {
            var db = CreatePocoDynamo()
                .RegisterTable<Customer>();

            await db.DeleteTableAsync<Customer>();
            await db.InitSchemaAsync();

            var customer = CreateCustomer();
            customer.Age = 27;
            await db.PutItemAsync(customer);

            var decrBy = -1;
            var q = db.UpdateExpression<Customer>(customer.Id)
                .Set(() => new Customer { Nationality = "Australian" })
                .Add(() => new Customer { Age = decrBy })
                .Remove(x => new { x.Name, x.Orders })
                .Condition(x => x.Age == 27);

            var succeeded = await db.UpdateItemAsync(q);
            Assert.That(succeeded);

            var updatedCustomer = await db.GetItemAsync<Customer>(customer.Id);
            Assert.That(updatedCustomer.Age, Is.EqualTo(customer.Age - 1));
            Assert.That(updatedCustomer.Name, Is.Null);
            Assert.That(updatedCustomer.Nationality, Is.EqualTo("Australian"));
            Assert.That(updatedCustomer.PrimaryAddress, Is.EqualTo(customer.PrimaryAddress));
            Assert.That(updatedCustomer.Orders, Is.Null);
        }

        [Test]
        public async Task Can_update_SET_with_conditional_expression()
        {
            var db = CreatePocoDynamo()
                .RegisterTable<Customer>();

            await db.DeleteTableAsync<Customer>();
            await db.InitSchemaAsync();

            var customer = CreateCustomer();
            customer.Age = 27;

            await db.PutItemAsync(customer);

            var q = db.UpdateExpression<Customer>(customer.Id)
                .Set(() => new Customer { Age = 30 })
                .Condition(x => x.Age == 29);

            var succeeded = await db.UpdateItemAsync(q);
            Assert.That(!succeeded);

            var latest = await db.GetItemAsync<Customer>(customer.Id);
            Assert.That(latest.Age, Is.EqualTo(27));

            q = db.UpdateExpression<Customer>(customer.Id)
                .Set(() => new Customer { Age = 30 })
                .Condition(x => x.Age == 27);

            succeeded = await db.UpdateItemAsync(q);
            Assert.That(succeeded);

            latest = await db.GetItemAsync<Customer>(customer.Id);
            Assert.That(latest.Age, Is.EqualTo(30));
        }

        [Test]
        public async Task Can_ADD_with_UpdateExpression()
        {
            var db = CreatePocoDynamo()
                .RegisterTable<Customer>();

            await db.DeleteTableAsync<Customer>();
            await db.InitSchemaAsync();

            var customer = CreateCustomer();
            customer.Age = 27;
            await db.PutItemAsync(customer);

            var decrBy = -1;

            var q = db.UpdateExpression<Customer>(customer.Id)
                .Add(() => new Customer { Age = decrBy });

            var succeeded = await db.UpdateItemAsync(q);
            Assert.That(succeeded);

            var latest = await db.GetItemAsync<Customer>(customer.Id);
            Assert.That(latest.Age, Is.EqualTo(26));
        }

        [Test]
        public async Task Can_REMOVE_with_UpdateExpression()
        {
            var db = CreatePocoDynamo()
                .RegisterTable<Customer>();

            await db.DeleteTableAsync<Customer>();
            await db.InitSchemaAsync();

            var customer = CreateCustomer();
            await db.PutItemAsync(customer);

            var q = db.UpdateExpression<Customer>(customer.Id)
                .Remove(x => new { x.Name, x.Orders });

            var succeeded = await db.UpdateItemAsync(q);
            Assert.That(succeeded);

            var updatedCustomer = await db.GetItemAsync<Customer>(customer.Id);
            Assert.That(updatedCustomer.Name, Is.Null);
            Assert.That(updatedCustomer.Orders, Is.Null);
        }
    }
}