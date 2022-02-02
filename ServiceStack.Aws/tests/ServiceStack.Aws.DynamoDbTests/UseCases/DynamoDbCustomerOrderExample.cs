using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests.UseCases
{
    public class Customer
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public CustomerAddress PrimaryAddress { get; set; }
    }

    public class CustomerAddress
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Address { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
    }

    [Alias("CustomerOrder")]
    public class Order
    {
        [AutoIncrement]
        public int Id { get; set; }

        [References(typeof(Customer))]
        public int CustomerId { get; set; }

        public string Product { get; set; }
        public int Qty { get; set; }

        [Index]
        public virtual decimal Cost { get; set; }
    }

    [References(typeof(OrderCostLocalIndex))]
    [References(typeof(OrderCostGlobalIndex))]
    [References(typeof(OrderProductGlobalIndex))]
    public class OrderTypedIndex
    {
        [AutoIncrement]
        public int Id { get; set; }

        [References(typeof(Customer))]
        public int CustomerId { get; set; }

        public string Product { get; set; }
        public int Qty { get; set; }
        public virtual decimal Cost { get; set; }
    }

    public class OrderCostLocalIndex : ILocalIndex<OrderTypedIndex>
    {
        [Index]
        public decimal Cost { get; set; }
        public int CustomerId { get; set; }

        public int Id { get; set; }
        public int Qty { get; set; }
    }

    public class OrderCostGlobalIndex : IGlobalIndex<OrderTypedIndex>
    {
        [HashKey]
        public string Product { get; set; }
        [RangeKey]
        public decimal Cost { get; set; }

        public int CustomerId { get; set; }
        public int Qty { get; set; }
        public int Id { get; set; }
    }

    public class OrderProductGlobalIndex : IGlobalIndex<OrderTypedIndex>
    {
        [HashKey]
        public string Product { get; set; }

        public decimal Cost { get; set; }
        public int CustomerId { get; set; }
        public int Qty { get; set; }
        public int Id { get; set; }
    }

    public class DynamoDbCustomerOrderExample : DynamoTestBase
    {
        private readonly IPocoDynamo db;

        public DynamoDbCustomerOrderExample()
        {
            db = new PocoDynamo(CreateDynamoDbClient())
                .RegisterTable<Customer>()
                .RegisterTable<Order>();
        }

        private Customer InsertCustomerAndOrders()
        {
            db.DeleteTable<Customer>();
            db.DeleteTable<Order>();
            db.Sequences.Reset<Customer>();
            db.Sequences.Reset<Order>();
            db.Sequences.Reset<CustomerAddress>();

            db.InitSchema();

            var customer = new Customer
            {
                Name = "Customer",
                PrimaryAddress = new CustomerAddress
                {
                    Address = "1 road",
                    State = "NT",
                    Country = "Australia",
                }
            };

            db.PutItem(customer);

            var orders = 10.Times(i => new Order
            {
                Product = "Item " + (i % 2 == 0 ? "A" : "B"),
                Qty = i + 2,
                Cost = (i + 2) * 2
            });

            db.PutRelatedItems(customer.Id, orders);
            return customer;
        }

        [Test]
        public void Can_store_Customer_and_related_orders()
        {
            var customer = InsertCustomerAndOrders();

            Assert.That(customer.Id, Is.GreaterThan(0));
            Assert.That(customer.PrimaryAddress.Id, Is.GreaterThan(0));

            var dbOrders = db.GetRelatedItems<Order>(customer.Id).ToList();

            dbOrders.PrintDump();
        }

        [Test]
        public void Can_query_related_Customer_orders()
        {
            var customer = InsertCustomerAndOrders();

            var q = db.FromQuery<Order>(x => x.CustomerId == customer.Id);
            var dbOrders = db.Query(q);

            dbOrders.ToList().PrintDump();

            var expensiveOrders = q.Clone()
                .Filter(x => x.Cost > 10)
                .Exec();

            expensiveOrders.ToList().PrintDump();
        }

        [Test]
        public void Can_query_related_Customer_orders_LocalIndex()
        {
            var customer = InsertCustomerAndOrders();

            var q = db.FromQuery<Order>(x => x.CustomerId == customer.Id);

            var expensiveOrders = q.Clone()
                .LocalIndex(x => x.Cost > 10)
                .Exec();

            expensiveOrders.ToList().PrintDump();

            expensiveOrders = q.Clone()
                .LocalIndex(x => x.Cost > 10)
                .Select<Order>()
                .Exec();

            expensiveOrders.ToList().PrintDump();
        }

        [Test]
        public void Can_query_related_Customer_orders_Typed_LocalIndex()
        {
            db.DeleteTable<OrderTypedIndex>();
            db.RegisterTable<OrderTypedIndex>();
            db.InitSchema();

            var customer = InsertCustomerAndOrders();

            var indexOrders = db.GetRelatedItems<Order>(customer.Id)
                .Map(x => x.ConvertTo<OrderTypedIndex>());

            db.PutItems(indexOrders);

            var expensiveOrderIndexes = db.FromQueryIndex<OrderCostLocalIndex>(
                x => x.CustomerId == customer.Id && x.Cost > 10)
                .Exec();

            expensiveOrderIndexes.ToList().PrintDump();

            var expensiveOrders = expensiveOrderIndexes
                .Map(x => x.ConvertTo<Order>());

            expensiveOrders.ToList().PrintDump();
        }

        [Test]
        public void Can_query_related_Customer_orders_Typed_GlobalIndex()
        {
            db.DeleteTable<OrderTypedIndex>();
            db.RegisterTable<OrderTypedIndex>();
            db.InitSchema();

            var customer = InsertCustomerAndOrders();

            var indexOrders = db.GetRelatedItems<Order>(customer.Id)
                .Map(x => x.ConvertTo<OrderTypedIndex>());

            db.PutItems(indexOrders);

            var expensiveItemAIndexes = db.FromQueryIndex<OrderCostGlobalIndex>(
                x => x.Product == "Item A" && x.Cost > 10)
                .Exec();

            //expensiveItemAIndexes.ToList().PrintDump();

            var expensiveItemAOrders = expensiveItemAIndexes
                .Map(x => x.ConvertTo<Order>());

            //expensiveItemAOrders.ToList().PrintDump();

            var expensiveOrderIndexes = db
                .FromScanIndex<OrderCostGlobalIndex>(x => x.Cost > 10)
                .Exec()
                .ToList();

            expensiveOrderIndexes.PrintDump();

            var expensiveOrders = db.FromScanIndex<OrderCostGlobalIndex>(x => x.Cost > 10)
                .ExecInto<Order>();

            expensiveOrders.PrintDump();
        }

        [Test]
        public void Can_query_related_Customer_orders_Typed_GlobalIndex_without_RangeKey()
        {
            db.DeleteTable<OrderTypedIndex>();
            db.RegisterTable<OrderTypedIndex>();
            db.InitSchema();

            var customer = InsertCustomerAndOrders();

            var indexOrders = db.GetRelatedItems<Order>(customer.Id)
                .Map(x => x.ConvertTo<OrderTypedIndex>());

            db.PutItems(indexOrders);

            var expensiveItemAIndexes = db.FromQueryIndex<OrderProductGlobalIndex>(
                    x => x.Product == "Item A")
                .Filter(x => x.Cost > 10)
                .Exec();

            var expensiveItemAOrders = expensiveItemAIndexes
                .Map(x => x.ConvertTo<Order>());

            var expensiveOrderIndexes = db
                .FromScanIndex<OrderProductGlobalIndex>(x => x.Cost > 10)
                .Exec()
                .ToList();

            expensiveOrderIndexes.PrintDump();

            var expensiveOrders = db.FromScanIndex<OrderProductGlobalIndex>(x => x.Cost > 10)
                .ExecInto<Order>();

            expensiveOrders.PrintDump();
        }

        [Test]
        public void Can_Scan_by_FilterExpression()
        {
            var customer = InsertCustomerAndOrders();

            var expensiveOrders = db.FromQuery<Order>(x => x.CustomerId == 1)
                .Exec().ToList();

            expensiveOrders = db.FromQuery<Order>()
                .KeyCondition(x => x.CustomerId == 1)
                .Exec().ToList();

            expensiveOrders = db.FromScan<Order>().Filter(x => x.Cost > 10)
                .Exec().ToList();

            expensiveOrders = db.FromScan<Order>().Filter("Cost > :amount", new { amount = 10 })
                .Exec().ToList();

            expensiveOrders = db.FromScan<Order>().Filter("Cost > :amount", new Dictionary<string, object> { { "amount", 10 } })
                .Exec().ToList();
        }

        class CustomerCost
        {
            public int CustomerId { get; set; }
            public virtual decimal Cost { get; set; }
        }

        [Test]
        public void Can_select_custom_fields()
        {
            var customer = InsertCustomerAndOrders();

            var partialOrders = db.FromScan<Order>().Select(x => new { x.CustomerId, x.Cost })
                .Exec();

            partialOrders = db.FromScan<Order>().Select(x => new[] { "CustomerId", "Cost" })
                .Exec();

            var custCosts = db.FromScan<Order>().Select<CustomerCost>()
                .Exec()
                .Map(x => x.ConvertTo<CustomerCost>());

            custCosts = db.FromScan<Order>()
                .ExecInto<CustomerCost>().ToList();

            custCosts.PrintDump();

            List<int> orderIds = db.FromScan<Order>().ExecColumn(x => x.Id).ToList();

            orderIds.PrintDump();
        }

        [Test]
        public void Can_scan_advanced_expressions()
        {
            var customer = InsertCustomerAndOrders();

            var orders = db.FromScan<Order>(x => x.Product.StartsWith("Item A"))
                .Exec();

            orders = db.FromScan<Order>(x => Dynamo.BeginsWith(x.Product, "Item A")).Exec();

            orders = db.FromScan<Order>().Filter("begins_with(Product, :s)", new { s = "Item A" })
                .Exec();

            orders = db.FromScan<Order>(x => x.Product.Contains("em A")).Exec();

            orders = db.FromScan<Order>(x => Dynamo.Contains(x.Product, "em A")).Exec();

            orders = db.FromScan<Order>().Filter("contains(Product, :s)", new { s = "em A" }).Exec();

            var qtys = new[] { 5, 10 };

            orders = db.FromScan<Order>(x => qtys.Contains(x.Qty)).Exec();

            orders = db.FromScan<Order>(x => Dynamo.In(x.Qty, qtys)).Exec();

            orders = db.FromScan<Order>().Filter("Qty in(:q1,:q2)", new { q1 = 5, q2 = 10 }).Exec();

            orders = db.FromScan<Order>(x => x.Product.Length == 6).Exec();

            orders = db.FromScan<Order>(x => Dynamo.Size(x.Product) == 6).Exec();

            orders = db.FromScan<Order>().Filter("size(Product) = :n", new { n = 6 }).Exec();

            orders = db.FromScan<Order>(x => Dynamo.Between(x.Qty, 3, 5)).Exec();

            orders = db.FromScan<Order>(x => x.Qty >= 3 && x.Qty <= 5).Exec();

            orders = db.FromScan<Order>().Filter("Qty between :from and :to", new { from = 3, to = 5 }).Exec();

            orders = db.FromScan<Order>(x => 
                    Dynamo.AttributeType(x.Product, DynamoType.String) &&
                    Dynamo.AttributeType(x.Qty, DynamoType.Number))
                .Exec();

            orders = db.FromScan<Order>().Filter(
                    "attribute_type(Qty, :n) and attribute_type(Product, :s)", new { n = "N", s = "S"})
                .Exec();

            orders = db.FromScan<Order>(x => Dynamo.AttributeExists(x.Product)).Exec();

            orders = db.FromScan<Order>().Filter("attribute_exists(Product)").Exec();

            orders.PrintDump();
        }

        public class IntCollections
        {
            public int Id { get; set; }

            public int[] ArrayInts { get; set; }
            public HashSet<int> SetInts { get; set; }
            public List<int> ListInts { get; set; }
            public Dictionary<int, int> DictionaryInts { get; set; }
        }

        [Test]
        public void Can_scan_advanced_expressions_size_collections()
        {
            var db = CreatePocoDynamo();
            db.DeleteTable<IntCollections>();
            db.RegisterTable<IntCollections>();
            db.InitSchema();

            var ints = 10.Times(i => i);

            var collections = new IntCollections
            {
                Id = 1,
                ArrayInts = ints.ToArray(),
                SetInts = new HashSet<int>(ints),
                ListInts = new List<int>(ints),
                DictionaryInts = new Dictionary<int, int>()
            };

            10.Times(x => collections.DictionaryInts[x] = x);

            db.PutItem(collections);

            var results = db.FromScan<IntCollections>(x =>
                    x.ArrayInts.Length == 10 &&
                    x.SetInts.Count == 10 &&
                    x.ListInts.Count == 10 &&
                    x.DictionaryInts.Count == 10)
                .Exec();

            results.ToList().PrintDump();
        }

    }
}