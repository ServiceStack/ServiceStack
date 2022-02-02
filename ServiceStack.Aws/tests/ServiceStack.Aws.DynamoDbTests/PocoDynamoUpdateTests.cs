using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests
{
    public class PocoDynamoUpdateTests : DynamoTestBase
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
        public void Can_UpdateItemNonDefaults_Partial_Customer()
        {
            var db = CreatePocoDynamo()
                .RegisterTable<Customer>();

            db.DeleteTable<Customer>();
            db.InitSchema();

            var customer = CreateCustomer();

            db.PutItem(customer);

            var row = db.UpdateItemNonDefaults(new Customer { Id = customer.Id, Age = 42 });
            row.PrintDump();

            var updatedCustomer = db.GetItem<Customer>(customer.Id);

            Assert.That(updatedCustomer.Age, Is.EqualTo(42));
            Assert.That(updatedCustomer.Name, Is.EqualTo(customer.Name));
            Assert.That(updatedCustomer.PrimaryAddress, Is.EqualTo(customer.PrimaryAddress));
            Assert.That(updatedCustomer.Orders, Is.EquivalentTo(customer.Orders));
        }

        [Test]
        public void Can_partial_UpdateItem_with_Dictionary()
        {
            var db = CreatePocoDynamo()
                .RegisterTable<Customer>();

            db.DeleteTable<Customer>();
            db.InitSchema();

            var customer = CreateCustomer();

            db.PutItem(customer);

            db.UpdateItem<Customer>(new DynamoUpdateItem
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

            var updatedCustomer = db.GetItem<Customer>(customer.Id);

            Assert.That(updatedCustomer.Age, Is.EqualTo(customer.Age - 1));
            Assert.That(updatedCustomer.Name, Is.Null);
            Assert.That(updatedCustomer.Nationality, Is.EqualTo("Australian"));
            Assert.That(updatedCustomer.PrimaryAddress, Is.EqualTo(customer.PrimaryAddress));
            Assert.That(updatedCustomer.Orders, Is.Null);
        }

        [Test]
        public void TypedApi_does_populate_DynamoUpdateItem()
        {
            var db = CreatePocoDynamo()
                .RegisterTable<Customer>();

            db.DeleteTable<Customer>();
            db.InitSchema();

            var customer = CreateCustomer();

            db.PutItem(customer);

            var decrBy = -1;
            db.UpdateItem(customer.Id, 
                put: () => new Customer {
                    Nationality = "Australian"
                },
                add: () => new Customer {
                    Age = decrBy
                },
                delete: x => new { x.Name, x.Orders });


            var updatedCustomer = db.GetItem<Customer>(customer.Id);

            Assert.That(updatedCustomer.Age, Is.EqualTo(customer.Age - 1));
            Assert.That(updatedCustomer.Name, Is.Null);
            Assert.That(updatedCustomer.Nationality, Is.EqualTo("Australian"));
            Assert.That(updatedCustomer.PrimaryAddress, Is.EqualTo(customer.PrimaryAddress));
            Assert.That(updatedCustomer.Orders, Is.Null);
        }

        [Test]
        public void Can_SET_ADD_REMOVE_with_conditional_expression_using_UpdateExpression()
        {
            var db = CreatePocoDynamo()
                .RegisterTable<Customer>();

            db.DeleteTable<Customer>();
            db.InitSchema();

            var customer = CreateCustomer();
            customer.Age = 27;
            db.PutItem(customer);

            var decrBy = -1;
            var q = db.UpdateExpression<Customer>(customer.Id)
                .Set(() => new Customer { Nationality = "Australian" })
                .Add(() => new Customer { Age = decrBy })
                .Remove(x => new { x.Name, x.Orders })
                .Condition(x => x.Age == 27);

            var succeeded = db.UpdateItem(q);
            Assert.That(succeeded);

            var updatedCustomer = db.GetItem<Customer>(customer.Id);
            Assert.That(updatedCustomer.Age, Is.EqualTo(customer.Age - 1));
            Assert.That(updatedCustomer.Name, Is.Null);
            Assert.That(updatedCustomer.Nationality, Is.EqualTo("Australian"));
            Assert.That(updatedCustomer.PrimaryAddress, Is.EqualTo(customer.PrimaryAddress));
            Assert.That(updatedCustomer.Orders, Is.Null);
        }

        [Test]
        public void Can_update_SET_with_conditional_expression()
        {
            var db = CreatePocoDynamo()
                .RegisterTable<Customer>();

            db.DeleteTable<Customer>();
            db.InitSchema();

            var customer = CreateCustomer();
            customer.Age = 27;

            db.PutItem(customer);

            var q = db.UpdateExpression<Customer>(customer.Id)
                .Set(() => new Customer { Age = 30 })
                .Condition(x => x.Age == 29);

            var succeeded = db.UpdateItem(q);
            Assert.That(!succeeded);

            var latest = db.GetItem<Customer>(customer.Id);
            Assert.That(latest.Age, Is.EqualTo(27));

            q = db.UpdateExpression<Customer>(customer.Id)
                .Set(() => new Customer { Age = 30 })
                .Condition(x => x.Age == 27);

            succeeded = db.UpdateItem(q);
            Assert.That(succeeded);

            latest = db.GetItem<Customer>(customer.Id);
            Assert.That(latest.Age, Is.EqualTo(30));
        }

        [Test]
        public void Can_ADD_with_UpdateExpression()
        {
            var db = CreatePocoDynamo()
                .RegisterTable<Customer>();

            db.DeleteTable<Customer>();
            db.InitSchema();

            var customer = CreateCustomer();
            customer.Age = 27;
            db.PutItem(customer);

            var decrBy = -1;

            var q = db.UpdateExpression<Customer>(customer.Id)
                .Add(() => new Customer { Age = decrBy });

            var succeeded = db.UpdateItem(q);
            Assert.That(succeeded);

            var latest = db.GetItem<Customer>(customer.Id);
            Assert.That(latest.Age, Is.EqualTo(26));
        }

        [Test]
        public void Can_REMOVE_with_UpdateExpression()
        {
            var db = CreatePocoDynamo()
                .RegisterTable<Customer>();

            db.DeleteTable<Customer>();
            db.InitSchema();

            var customer = CreateCustomer();
            db.PutItem(customer);

            var q = db.UpdateExpression<Customer>(customer.Id)
                .Remove(x => new { x.Name, x.Orders });

            var succeeded = db.UpdateItem(q);
            Assert.That(succeeded);

            var updatedCustomer = db.GetItem<Customer>(customer.Id);
            Assert.That(updatedCustomer.Name, Is.Null);
            Assert.That(updatedCustomer.Orders, Is.Null);
        }
    }
}