using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests
{
    public class PocoDynamoRelatedTablesAsync : DynamoTestBase
    {
        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            var db = CreatePocoDynamo();
            await db.DeleteAllTablesAsync(TimeSpan.FromMinutes(1));
        }

        [Test]
        public async Task Can_Get_from_Related_Order_Table_from_Customer()
        {
            var db = CreatePocoDynamo();
            var customer = await CreateCustomerAsync(db);

            var orders = new[]
            {
                new Order
                {
                    CustomerId = 11,
                    LineItem = "Item 1",
                    Qty = 3,
                    Cost = 2,
                },
                new Order
                {
                    CustomerId = 11,
                    LineItem = "Item 2",
                    Qty = 4,
                    Cost = 3,
                },
            };

            await db.PutRelatedItemsAsync(customer.Id, orders);

            Assert.That(customer.Id, Is.GreaterThan(0));
            Assert.That(customer.PrimaryAddress.Id, Is.GreaterThan(0));

            Assert.That(orders[0].Id, Is.GreaterThan(0));
            Assert.That(orders[0].CustomerId, Is.EqualTo(customer.Id));
            Assert.That(orders[1].Id, Is.GreaterThan(0));
            Assert.That(orders[1].CustomerId, Is.EqualTo(customer.Id));

            var dbOrders = db.GetRelatedItems<Order>(customer.Id).ToList();
            Assert.That(dbOrders, Is.EquivalentTo(orders));

            dbOrders = await db.QueryAsync(db.FromQuery<Order>(x => x.CustomerId == customer.Id)).ToListAsync();
            Assert.That(dbOrders, Is.EquivalentTo(orders));
        }

        [Test]
        public async Task Can_Create_LocalIndex()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<OrderWithFieldIndex>();

            var customer = await CreateCustomerAsync(db);

            var orders = 10.Times(i => new OrderWithFieldIndex
            {
                LineItem = "Item " + (i + 1),
                Qty = i + 2,
                Cost = (i + 2) * 2
            });

            await db.PutRelatedItemsAsync(customer.Id, orders);

            var q = db.FromQuery<OrderWithFieldIndex>(x => x.CustomerId == customer.Id)
                .LocalIndex(x => x.Cost > 10);

            var expensiveOrders = await db.QueryAsync(q).ToListAsync();
            Assert.That(expensiveOrders.Count, Is.EqualTo(orders.Count(x => x.Cost > 10)));
            Assert.That(expensiveOrders.All(x => x.Qty == 0));  //non-projected field

            expensiveOrders = await db.QueryAsync(q.Clone().Select(new[] { "Qty" })).ToListAsync();
            Assert.That(expensiveOrders.All(x => x.Id == 0 && x.Qty > 0));

            expensiveOrders = await db.QueryAsync(q.Clone().SelectTableFields()).ToListAsync();
            Assert.That(expensiveOrders.All(x => x.Cost > 10 && x.Id > 0 && x.CustomerId > 0 && x.Qty > 0 && x.LineItem != null));

            expensiveOrders = await db.QueryAsync(q.Clone().Select<OrderWithFieldIndex>()).ToListAsync();
            Assert.That(expensiveOrders.All(x => x.Cost > 10 && x.Id > 0 && x.CustomerId > 0 && x.Qty > 0 && x.LineItem != null));

            expensiveOrders = await db.QueryAsync(q.Clone().Select(x => new { x.Id, x.Cost })).ToListAsync();
            Assert.That(expensiveOrders.All(x => x.CustomerId == 0));
            Assert.That(expensiveOrders.All(x => x.Cost > 10 && x.Id > 0));

            Assert.ThrowsAsync<AmazonDynamoDBException>(async () =>
                await db.QueryAsync(db.FromQuery<OrderWithFieldIndex>().LocalIndex(x => x.Cost > 10)).ToListAsync());
        }

        [Test]
        public async Task Can_Create_Typed_LocalIndex()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<OrderWithLocalTypedIndex>();

            var customer = await CreateCustomerAsync(db);

            var orders = 10.Times(i => new OrderWithLocalTypedIndex
            {
                LineItem = "Item " + (i + 1),
                Qty = i + 2,
                Cost = (i + 2) * 2
            });

            await db.PutRelatedItemsAsync(customer.Id, orders);

            var expensiveOrders = await db.QueryAsync(
                db.FromQueryIndex<OrderCostIndex>(x => x.CustomerId == customer.Id && x.Cost > 10)).ToListAsync();

            expensiveOrders.PrintDump();

            Assert.That(expensiveOrders.Count, Is.EqualTo(orders.Count(x => x.CustomerId == customer.Id && x.Cost > 10)));
            Assert.That(expensiveOrders.All(x => x.CustomerId == customer.Id));
            Assert.That(expensiveOrders.All(x => x.Cost > 10 && x.Id > 0 && x.Qty > 0));

            Assert.ThrowsAsync<AmazonDynamoDBException>(async () =>
                await db.QueryAsync(db.FromQueryIndex<OrderCostIndex>(x => x.Cost > 10)).ToListAsync());
        }

        [Test]
        public async Task Can_Create_Typed_GlobalIndex()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<OrderWithGlobalTypedIndex>();

            var customer = await CreateCustomerAsync(db);

            var table = DynamoMetadata.GetTable<OrderWithGlobalTypedIndex>();
            Assert.That(table.GlobalIndexes.Count, Is.EqualTo(1));
            Assert.That(table.GlobalIndexes[0].Name, Is.EqualTo(nameof(OrderGlobalCostIndex)));
            Assert.That(table.GlobalIndexes[0].HashKey.Name, Is.EqualTo("ProductId"));
            Assert.That(table.GlobalIndexes[0].RangeKey.Name, Is.EqualTo("Cost"));

            var orders = 10.Times(i => new OrderWithGlobalTypedIndex
            {
                ProductId = 1,
                Qty = i + 2,
                Cost = (i + 2) * 2,
                LineItem = "Item " + (i + 1),
            });

            await db.PutRelatedItemsAsync(customer.Id, orders);

            var expensiveOrders = await db.QueryAsync(db.FromQueryIndex<OrderGlobalCostIndex>(x => x.ProductId == 1 && x.Cost > 10)).ToListAsync();
            Assert.That(expensiveOrders.Count, Is.EqualTo(orders.Count(x => x.ProductId == 1 && x.Cost > 10)));
            Assert.That(expensiveOrders.All(x => x.Cost > 10 && x.Id > 0 && x.Qty > 0));

            //Note: Local DynamoDb supports projecting attributes not in GlobalIndex, AWS DynamoDB Doesn't
            var dbOrders = (await db.QueryIntoAsync<OrderWithGlobalTypedIndex>(db.FromQueryIndex<OrderGlobalCostIndex>(x => x.ProductId == 1 && x.Cost > 10))).ToList();
            Assert.That(dbOrders.All(x => x.Cost > 10 && x.Id > 0 && x.Qty > 0 && x.LineItem != null));
            dbOrders = (await db.FromQueryIndex<OrderGlobalCostIndex>(x => x.ProductId == 1 && x.Cost > 10).ExecIntoAsync<OrderWithGlobalTypedIndex>()).ToList();
            Assert.That(dbOrders.All(x => x.Cost > 10 && x.Id > 0 && x.Qty > 0 && x.LineItem != null));

            expensiveOrders = await db.ScanAsync(db.FromScanIndex<OrderGlobalCostIndex>(x => x.Cost > 10)).ToListAsync();
            Assert.That(expensiveOrders.All(x => x.Cost > 10 && x.Id > 0 && x.Qty > 0));

            var indexOrders = (await db.ScanIntoAsync<OrderWithGlobalTypedIndex>(db.FromScanIndex<OrderGlobalCostIndex>(x => x.Cost > 10))).ToList();
            Assert.That(indexOrders.Count, Is.GreaterThan(0));
            Assert.That(indexOrders.All(x => x.Cost > 10 && x.Id > 0 && x.Qty > 0 && x.LineItem != null));
        }

        private static async Task<Customer> CreateCustomerAsync(IPocoDynamo db)
        {
            var types = new List<Type>()
                .Add<Customer>()
                .Add<Order>();

            db.RegisterTables(types);

            await db.InitSchemaAsync();

            var customer = new Customer
            {
                Name = "Customer #1",
                PrimaryAddress = new CustomerAddress
                {
                    AddressLine1 = "Line 1",
                    City = "Darwin",
                    State = "NT",
                    Country = "AU",
                }
            };

            await db.PutItemAsync(customer);
            return customer;
        }
    }
}