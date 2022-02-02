using System;
using System.Data;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;
using ServiceStack.OrmLite.Dapper;
using ServiceStack.OrmLite.Tests.UseCase;
using ServiceStack.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixtureOrmLite]
    [NonParallelizable]
    public class LoadReferencesJoinTests : OrmLiteProvidersTestBase
    {
        public LoadReferencesJoinTests(DialectContext context) : base(context) {}

        private IDbConnection db;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            db = base.OpenDbConnection();
            ResetTables();
        }

        private void ResetTables()
        {
            CustomerOrdersUseCase.DropTables(db); //Has conflicting 'Order' table

            if(DialectFeatures.SchemaSupport) db.CreateSchema<ProjectTask>();
            db.DropAndCreateTable<Order>();
            db.DropAndCreateTable<Customer>();
            db.DropAndCreateTable<CustomerAddress>();
            db.DropAndCreateTable<Country>();
        }

        [SetUp]
        public void SetUp()
        {
            db.DeleteAll<Order>();
            db.DeleteAll<CustomerAddress>();
            db.DeleteAll<Customer>();
            db.DeleteAll<Country>();
        }

        [OneTimeTearDown]
        public new void TestFixtureTearDown()
        {
            db.Dispose();
        }

        private Customer AddCustomerWithOrders()
        {
            var customer = new Customer
            {
                Name = "Customer 1",
                PrimaryAddress = new CustomerAddress
                {
                    AddressLine1 = "1 Australia Street",
                    Country = "Australia"
                },
                Orders = new[]
                {
                    new Order {LineItem = "Line 1", Qty = 1, Cost = 1.99m},
                    new Order {LineItem = "Line 2", Qty = 2, Cost = 2.99m},
                }.ToList(),
            };

            db.Save(customer, references: true);

            return customer;
        }

        [Test]
        public async Task Can_execute_LoadSelectAsync_with_OrderBy()
        {
            var customers = AddCustomersWithOrders();

            var q = db.From<Customer>()
                .OrderByFields("Id");

            string[] include = null; 
            var results = await db.LoadSelectAsync(q);
            
            Assert.That(results.Count, Is.GreaterThan(1));
        }

        public class FullCustomerInfo
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string CustomerName { get; set; }
            public int CustomerAddressId { get; set; }
            public string AddressLine1 { get; set; }
            public string City { get; set; }
            public int OrderId { get; set; }
            public string LineItem { get; set; }
            public decimal Cost { get; set; }
            public decimal OrderCost { get; set; }
            public string CountryCode { get; set; }
        }

        public class MixedCustomerInfo
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string AliasedCustomerName { get; set; }
            public string aliasedcustomername { get; set; }
            public int Q_CustomerId { get; set; }
            public int Q_CustomerAddressQ_CustomerId { get; set; }
            public string CountryName { get; set; }
            public int CountryId { get; set; }
        }

        [Test]
        public void Can_do_multiple_joins_with_SqlExpression()
        {
            AddCustomerWithOrders();

            var results = db.Select<FullCustomerInfo>(db.From<Customer>()
                .Join<Customer, CustomerAddress>()
                .Join<Customer, Order>());

            var costs = results.ConvertAll(x => x.Cost);
            Assert.That(costs, Is.EquivalentTo(new[] { 1.99m, 2.99m }));

            var expr = db.From<Customer>()
                .Join<Customer, CustomerAddress>()
                .Join<Customer, Order>();

            results = db.Select<FullCustomerInfo>(expr);

            costs = results.ConvertAll(x => x.Cost);
            Assert.That(costs, Is.EquivalentTo(new[] { 1.99m, 2.99m }));
        }

        [Test]
        public void Can_do_joins_with_wheres_using_SqlExpression()
        {
            AddCustomerWithOrders();

            var results = db.Select<FullCustomerInfo>(db.From<Customer>()
                .Join<Customer, CustomerAddress>()
                .Join<Customer, Order>((c, o) => c.Id == o.CustomerId && o.Cost < 2));

            var costs = results.ConvertAll(x => x.Cost);
            Assert.That(costs, Is.EquivalentTo(new[] { 1.99m }));

            var orders = db.Select(db.From<Order>()
                .Join<Order, Customer>()
                .Join<Customer, CustomerAddress>()
                .Where(o => o.Cost < 2)
                .And<Customer>(c => c.Name == "Customer 1"));

            costs = orders.ConvertAll(x => x.Cost);
            Assert.That(costs, Is.EquivalentTo(new[] { 1.99m }));

            results = db.Select<FullCustomerInfo>(db.From<Customer>()
                .Join<Customer, CustomerAddress>()
                .Join<Customer, Order>()
                .Where<Order>(o => o.Cost < 2));

            costs = results.ConvertAll(x => x.Cost);
            Assert.That(costs, Is.EquivalentTo(new[] { 1.99m }));

            results = db.Select<FullCustomerInfo>(db.From<Customer>()
                .Join<Customer, CustomerAddress>()
                .Join<Customer, Order>()
                .Where<Order>(o => o.Cost < 2 || o.LineItem == "Line 2"));

            costs = results.ConvertAll(x => x.Cost);
            Assert.That(costs, Is.EquivalentTo(new[] { 1.99m, 2.99m }));

            var expr = db.From<Customer>()
                .Join<Customer, CustomerAddress>()
                .Join<Customer, Order>()
                .Where<Order>(o => o.Cost < 2 || o.LineItem == "Line 2");
            results = db.Select<FullCustomerInfo>(expr);

            costs = results.ConvertAll(x => x.Cost);
            Assert.That(costs, Is.EquivalentTo(new[] { 1.99m, 2.99m }));
        }

        [Test]
        public void Can_do_joins_with_complex_wheres_using_SqlExpression()
        {
            var customers = AddCustomersWithOrders();

            db.Insert(
                new Country { CountryName = "Australia", CountryCode = "AU" },
                new Country { CountryName = "USA", CountryCode = "US" });

            var results = db.Select<FullCustomerInfo>(db.From<Customer>()
                .Join<CustomerAddress>() //implicit
                .Join<Customer, Order>() //explicit
                .Where(c => c.Name == "Customer 1")
                .And<Order>(o => o.Cost < 2)
                .Or<Order>(o => o.LineItem == "Australia Flag"));

            var costs = results.ConvertAll(x => x.Cost);
            Assert.That(costs, Is.EquivalentTo(new[] { 1.99m, 1.49m, 9.99m }));
            var orderIds = results.ConvertAll(x => x.OrderId);
            var expectedOrderIds = new[] { customers[0].Orders[0].Id, customers[0].Orders[2].Id, customers[0].Orders[4].Id };
            Assert.That(orderIds, Is.EquivalentTo(expectedOrderIds));

            //Same as above using using db.From<Customer>()
            results = db.Select<FullCustomerInfo>(db.From<Customer>()
                .Join<CustomerAddress>() //implicit
                .Join<Customer, Order>() //explicit
                .Where(c => c.Name == "Customer 1")
                .And<Order>(o => o.Cost < 2)
                .Or<Order>(o => o.LineItem == "Australia Flag"));

            costs = results.ConvertAll(x => x.Cost);
            Assert.That(costs, Is.EquivalentTo(new[] { 1.99m, 1.49m, 9.99m }));

            results = db.Select<FullCustomerInfo>(db.From<Customer>()
                .Join<Customer, CustomerAddress>()
                .Join<Customer, Order>()
                .Where(c => c.Name == "Customer 2")
                .And<CustomerAddress, Order>((a, o) => a.Country == o.LineItem));

            costs = results.ConvertAll(x => x.Cost);
            Assert.That(costs, Is.EquivalentTo(new[] { 20m }));

            var countryResults = db.Select<FullCustomerInfo>(db.From<Customer>()
                .Join<CustomerAddress>()                     //implicit join with Customer
                .Join<Order>((c, o) => c.Id == o.CustomerId) //explicit join condition
                .Join<CustomerAddress, Country>((ca, c) => ca.Country == c.CountryName)
                .Where(c => c.Name == "Customer 2")          //implicit condition with Customer
                .And<CustomerAddress, Order>((a, o) => a.Country == o.LineItem));

            costs = countryResults.ConvertAll(x => x.Cost);
            Assert.That(costs, Is.EquivalentTo(new[] { 20m }));
            Assert.That(countryResults.ConvertAll(x => x.CountryCode), Is.EquivalentTo(new[] { "US" }));
        }

        private Customer[] AddCustomersWithOrders()
        {
            var customers = new[]
            {
                new Customer
                {
                    Name = "Customer 1",
                    PrimaryAddress = new CustomerAddress
                    {
                        AddressLine1 = "1 Australia Street",
                        Country = "Australia"
                    },
                    Orders = new[]
                    {
                        new Order {LineItem = "Line 1", Qty = 1, Cost = 1.99m},
                        new Order {LineItem = "Line 1", Qty = 2, Cost = 3.98m},
                        new Order {LineItem = "Line 2", Qty = 1, Cost = 1.49m},
                        new Order {LineItem = "Line 2", Qty = 2, Cost = 2.98m},
                        new Order {LineItem = "Australia Flag", Qty = 1, Cost = 9.99m},
                    }.ToList(),
                },
                new Customer
                {
                    Name = "Customer 2",
                    PrimaryAddress = new CustomerAddress
                    {
                        AddressLine1 = "2 Prospect Park",
                        Country = "USA"
                    },
                    Orders = new[]
                    {
                        new Order {LineItem = "USA", Qty = 1, Cost = 20m},
                    }.ToList(),
                },
            };

            customers.Each(c =>
                db.Save(c, references: true));

            return customers;
        }

        [Test]
        public void Can_do_LeftJoins_using_SqlExpression()
        {
            AddCustomers();

            db.Insert(
                new Country { CountryName = "Australia", CountryCode = "AU" },
                new Country { CountryName = "USA", CountryCode = "US" },
                new Country { CountryName = "Italy", CountryCode = "IT" },
                new Country { CountryName = "Spain", CountryCode = "ED" });

            //Normal Join
            var dbCustomers = db.Select(db.From<Customer>()
                .Join<CustomerAddress>()
                .Join<CustomerAddress, Country>((ca, c) => ca.Country == c.CountryName));

            Assert.That(dbCustomers.Count, Is.EqualTo(2));

            //Left Join
            dbCustomers = db.Select(db.From<Customer>()
                .Join<CustomerAddress>()
                .LeftJoin<CustomerAddress, Country>((ca, c) => ca.Country == c.CountryName));

            Assert.That(dbCustomers.Count, Is.EqualTo(3));

            //Warning: Right and Full Joins are not implemented by Sqlite3. Avoid if possible.
            var dbCountries = db.Select(db.From<Country>()
                .LeftJoin<CustomerAddress>((c, ca) => ca.Country == c.CountryName)
                .LeftJoin<CustomerAddress, Customer>());

            Assert.That(dbCountries.Count, Is.EqualTo(4));

            var dbAddresses = db.Select(db.From<CustomerAddress>()
                .LeftJoin<Country>((ca, c) => ca.Country == c.CountryName)
                .LeftJoin<CustomerAddress, Customer>());

            Assert.That(dbAddresses.Count, Is.EqualTo(3));
        }

        private void AddCustomers()
        {
            var customers = new[]
            {
                new Customer
                {
                    Name = "Customer 1",
                    PrimaryAddress = new CustomerAddress
                    {
                        AddressLine1 = "1 Australia Street",
                        Country = "Australia"
                    },
                },
                new Customer
                {
                    Name = "Customer 2",
                    PrimaryAddress = new CustomerAddress
                    {
                        AddressLine1 = "2 America Street",
                        Country = "USA"
                    },
                },
                new Customer
                {
                    Name = "Customer 3",
                    PrimaryAddress = new CustomerAddress
                    {
                        AddressLine1 = "3 Canada Street",
                        Country = "Canada"
                    },
                },
            };

            customers.Each(c =>
                db.Save(c, references: true));
        }

        [Test]
        public void Can_Join_on_matching_Alias_convention()
        {
            Country[] countries;
            AddAliasedCustomers(out countries);

            //Normal Join
            var dbCustomers = db.Select(db.From<AliasedCustomer>()
                .Join<AliasedCustomerAddress>()
                .Join<AliasedCustomerAddress, Country>((ca, c) => ca.Country == c.CountryName));

            Assert.That(dbCustomers.Count, Is.EqualTo(2));

            //Left Join
            dbCustomers = db.Select(db.From<AliasedCustomer>()
                .Join<AliasedCustomerAddress>()
                .LeftJoin<AliasedCustomerAddress, Country>((ca, c) => ca.Country == c.CountryName));

            Assert.That(dbCustomers.Count, Is.EqualTo(3));

            //Warning: Right and Full Joins are not implemented by Sqlite3. Avoid if possible.
            var dbCountries = db.Select(db.From<Country>()
                .LeftJoin<AliasedCustomerAddress>((c, ca) => ca.Country == c.CountryName)
                .LeftJoin<AliasedCustomerAddress, AliasedCustomer>());

            Assert.That(dbCountries.Count, Is.EqualTo(4));

            var dbAddresses = db.Select(db.From<AliasedCustomerAddress>()
                .LeftJoin<Country>((ca, c) => ca.Country == c.CountryName)
                .LeftJoin<AliasedCustomerAddress, AliasedCustomer>());

            Assert.That(dbAddresses.Count, Is.EqualTo(3));
        }

        private AliasedCustomer[] AddAliasedCustomers(out Country[] countries)
        {
            db.DropAndCreateTable<AliasedCustomer>();
            db.DropAndCreateTable<AliasedCustomerAddress>();
            db.DropAndCreateTable<Country>();

            var customers = new[]
            {
                new AliasedCustomer
                {
                    Name = "Customer 1",
                    PrimaryAddress = new AliasedCustomerAddress
                    {
                        AddressLine1 = "1 Australia Street",
                        Country = "Australia"
                    },
                },
                new AliasedCustomer
                {
                    Name = "Customer 2",
                    PrimaryAddress = new AliasedCustomerAddress
                    {
                        AddressLine1 = "2 America Street",
                        Country = "USA"
                    },
                },
                new AliasedCustomer
                {
                    Name = "Customer 3",
                    PrimaryAddress = new AliasedCustomerAddress
                    {
                        AddressLine1 = "3 Canada Street",
                        Country = "Canada"
                    },
                },
            };

            customers.Each(c =>
                           db.Save(c, references: true));

            countries = new[]
            {
                new Country {CountryName = "Australia", CountryCode = "AU"},
                new Country {CountryName = "USA", CountryCode = "US"},
                new Country {CountryName = "Italy", CountryCode = "IT"},
                new Country {CountryName = "Spain", CountryCode = "ED"}
            };
            db.Save(countries);

            return customers;
        }

        [Test]
        public void Does_populate_custom_columns_based_on_property_convention()
        {
            // Reset auto ids
            db.DropAndCreateTable<Order>();
            db.DropAndCreateTable<CustomerAddress>();
            db.DropAndCreateTable<Customer>();

            var customer = AddCustomerWithOrders();

            var results = db.Select<FullCustomerInfo>(db.From<Customer>()
                .Join<Customer, CustomerAddress>()
                .Join<Customer, Order>());

            var addressIds = results.ConvertAll(x => x.CustomerAddressId);
            var expectedAddressIds = new[] { customer.PrimaryAddress.Id, customer.PrimaryAddress.Id };
            Assert.That(addressIds, Is.EquivalentTo(expectedAddressIds));

            var orderIds = results.ConvertAll(x => x.OrderId);
            var expectedOrderIds = new[] { customer.Orders[0].Id, customer.Orders[1].Id };
            Assert.That(orderIds, Is.EquivalentTo(expectedOrderIds));

            var customerNames = results.ConvertAll(x => x.CustomerName);
            Assert.That(customerNames, Is.EquivalentTo(new[] { "Customer 1", "Customer 1" }));

            var orderCosts = results.ConvertAll(x => x.OrderCost);
            Assert.That(orderCosts, Is.EquivalentTo(new[] { 1.99m, 2.99m }));

            var expr = db.From<Customer>()
                .Join<Customer, CustomerAddress>()
                .Join<Customer, Order>()
                .Where<Order>(o => o.Cost > 2);

            results = db.Select<FullCustomerInfo>(expr);

            addressIds = results.ConvertAll(x => x.CustomerAddressId);
            Assert.That(addressIds, Is.EquivalentTo(new[] { customer.PrimaryAddress.Id }));

            orderIds = results.ConvertAll(x => x.OrderId);
            Assert.That(orderIds, Is.EquivalentTo(new[] { customer.Orders[1].Id }));

            customerNames = results.ConvertAll(x => x.CustomerName);
            Assert.That(customerNames, Is.EquivalentTo(new[] { "Customer 1" }));

            orderCosts = results.ConvertAll(x => x.OrderCost);
            Assert.That(orderCosts, Is.EquivalentTo(new[] { 2.99m }));
        }

        [Test]
        public void Does_populate_custom_mixed_columns()
        {
            Country[] countries;
            var customers = AddAliasedCustomers(out countries);

            //Normal Join
            var results = db.Select<MixedCustomerInfo>(db.From<AliasedCustomer>()
                .Join<AliasedCustomerAddress>()
                .Join<AliasedCustomerAddress, Country>((ca, c) => ca.Country == c.CountryName));

            var customerNames = results.Map(x => x.Name);
            Assert.That(customerNames, Is.EquivalentTo(new[] { "Customer 1", "Customer 2" }));

            customerNames = results.Map(x => x.AliasedCustomerName);
            Assert.That(customerNames, Is.EquivalentTo(new[] { "Customer 1", "Customer 2" }));

            customerNames = results.Map(x => x.aliasedcustomername);
            Assert.That(customerNames, Is.EquivalentTo(new[] { "Customer 1", "Customer 2" }));

            var customerIds = results.Map(x => x.Q_CustomerId);
            Assert.That(customerIds, Is.EquivalentTo(new[] { customers[0].Id, customers[1].Id }));

            customerIds = results.Map(x => x.Q_CustomerAddressQ_CustomerId);
            Assert.That(customerIds, Is.EquivalentTo(new[] { customers[0].Id, customers[1].Id }));

            var countryNames = results.Map(x => x.CountryName);
            Assert.That(countryNames, Is.EquivalentTo(new[] { "Australia", "USA" }));

            var countryIds = results.Map(x => x.CountryId);
            Assert.That(countryIds, Is.EquivalentTo(new[] { countries[0].Id, countries[1].Id }));
        }

        [Test]
        public void Can_LeftJoin_and_select_empty_relation()
        {
            AddCustomerWithOrders();

            var customer = new Customer
            {
                Name = "Customer 2",
                PrimaryAddress = new CustomerAddress
                {
                    AddressLine1 = "2 America Street",
                    Country = "USA"
                },
            };

            db.Save(customer, references: true);

#pragma warning disable 472
            var q = db.From<Customer>();
            q.LeftJoin<Order>()
             .Where<Order>(o => o.Id == null);
#pragma warning restore 472

            var customers = db.Select(q);

            Assert.That(customers.Count, Is.EqualTo(1));
            Assert.That(customers[0].Name, Is.EqualTo("Customer 2"));
        }

        [Test]
        public void Can_load_list_of_references()
        {
            AddCustomersWithOrders();

            var results = db.LoadSelect<Customer>();
            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.All(x => x.PrimaryAddress != null));
            Assert.That(results.All(x => x.Orders.Count > 0));

            var customer1 = results.First(x => x.Name == "Customer 1");
            Assert.That(customer1.PrimaryAddress.Country, Is.EqualTo("Australia"));
            Assert.That(customer1.Orders.Select(x => x.Cost),
                Is.EquivalentTo(new[] { 1.99m, 3.98m, 1.49m, 2.98m, 9.99m }));

            var customer2 = results.First(x => x.Name == "Customer 2");
            Assert.That(customer2.PrimaryAddress.Country, Is.EqualTo("USA"));
            Assert.That(customer2.Orders[0].LineItem, Is.EqualTo("USA"));

            results = db.LoadSelect<Customer>(q => q.Name == "Customer 1");
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].PrimaryAddress.Country, Is.EqualTo("Australia"));
            Assert.That(results[0].Orders.Select(x => x.Cost),
                Is.EquivalentTo(new[] { 1.99m, 3.98m, 1.49m, 2.98m, 9.99m }));
        }

        [Test]
        public void Can_load_list_of_references_using_subselect()
        {
            AddCustomersWithOrders();

            var customers = db.Select(db.From<Customer>()
                .Join<Order>()
                .Where<Order>(o => o.Qty == 1)
                .OrderBy(x => x.Id)
                .SelectDistinct());

            var orders = db.Select<Order>(o => o.Qty == 1);

            customers.Merge(orders);

            customers.PrintDump();

            Assert.That(customers.Count, Is.EqualTo(2));
            Assert.That(customers[0].Orders.Count, Is.EqualTo(3));
            Assert.That(customers[0].Orders.All(x => x.Qty == 1));
            Assert.That(customers[1].Orders.Count, Is.EqualTo(1));
            Assert.That(customers[1].Orders.All(x => x.Qty == 1));
        }

        [Test]
        public void Can_join_on_references_attribute()
        {
            // Drop tables in order that FK allows
            db.DropTable<TABLE_3>();
            db.DropTable<TABLE_2>();
            db.DropTable<TABLE_1>();
            db.CreateTable<TABLE_1>();
            db.CreateTable<TABLE_2>();
            db.CreateTable<TABLE_3>();

            var id1 = db.Insert(new TABLE_1 { One = "A" }, selectIdentity: true);
            var id2 = db.Insert(new TABLE_1 { One = "B" }, selectIdentity: true);

            db.Insert(new TABLE_2 { Three = "C", TableOneKey = (int)id1 });

            var q = db.From<TABLE_1>()
                      .Join<TABLE_2>();
            var results = db.Select(q);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].One, Is.EqualTo("A"));

            var row3 = new TABLE_3
            {
                Three = "3a",
                TableTwo = new TABLE_2
                {
                    Three = "3b",
                    TableOneKey = (int)id1,
                }
            };
            db.Save(row3, references: true);

            Assert.That(row3.TableTwoKey, Is.EqualTo(row3.TableTwo.Id));

            row3 = db.LoadSingleById<TABLE_3>(row3.Id);
            Assert.That(row3.TableTwoKey, Is.EqualTo(row3.TableTwo.Id));
        }

        [Test]
        public void Can_load_references_with_OrderBy()
        {
            AddCustomersWithOrders();

            var customers = db.LoadSelect(db.From<Customer>().OrderBy(x => x.Name));
            var addresses = customers.Select(x => x.PrimaryAddress).ToList();
            var orders = customers.SelectMany(x => x.Orders).ToList();

            Assert.That(customers.Count, Is.EqualTo(2));
            Assert.That(addresses.Count, Is.EqualTo(2));
            Assert.That(orders.Count, Is.EqualTo(6));
        }

        [Test]
        public void Can_load_select_with_join()
        {
            // Drop tables in order that FK allows
            db.DropTable<TABLE_3>();
            db.DropTable<TABLE_2>();
            db.DropTable<TABLE_1>();
            db.CreateTable<TABLE_1>();
            db.CreateTable<TABLE_2>();
            db.CreateTable<TABLE_3>();

            var id1 = db.Insert(new TABLE_1 { One = "A" }, selectIdentity: true);
            var id2 = db.Insert(new TABLE_1 { One = "B" }, selectIdentity: true);

            db.Insert(new TABLE_2 { Three = "C", TableOneKey = (int)id1 });

            var q = db.From<TABLE_1>()
                      .Join<TABLE_2>();
            var results = db.LoadSelect(q);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].One, Is.EqualTo("A"));

            var row3 = new TABLE_3
            {
                Three = "3a",
                TableTwo = new TABLE_2
                {
                    Three = "3b",
                    TableOneKey = (int)id1,
                }
            };
            db.Save(row3, references: true);

            Assert.That(row3.TableTwoKey, Is.EqualTo(row3.TableTwo.Id));

            row3 = db.LoadSingleById<TABLE_3>(row3.Id);
            Assert.That(row3.TableTwoKey, Is.EqualTo(row3.TableTwo.Id));
        }

        [Test]
        public void Can_load_select_with_join_and_same_name_columns()
        {
            // Drop tables in order that FK allows
            db.DropTable<ProjectTask>();
            db.DropTable<Project>();
            db.CreateTable<Project>();
            db.CreateTable<ProjectTask>();

            db.Insert(new Project { Val = "test" });
            db.Insert(new ProjectTask { Val = "testTask", ProjectId = 1 });

            var query = db.From<ProjectTask>()
                .Join<ProjectTask, Project>((pt, p) => pt.ProjectId == p.Id);

            var selectResults = db.Select(query);

            var results = db.LoadSelect(query);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Val, Is.EqualTo("testTask"));
        }

        [Test]
        [IgnoreDialect(Dialect.AnyMySql, "doesn't yet support 'LIMIT & IN/ALL/ANY/SOME subquery")]
        [IgnoreDialect(Dialect.AnySqlServer, "Only one expression can be specified in the select list when the subquery is not introduced with EXISTS.")]
        public void Can_load_references_with_OrderBy_and_Paging()
        {
            db.DropTable<ParentSelf>();
            db.DropTable<ChildSelf>();

            db.CreateTable<ChildSelf>();
            db.CreateTable<ParentSelf>();

            db.Save(new ChildSelf { Id = 1, Value = "Lolz" });
            db.Insert(new ParentSelf { Id = 1, ChildId = null });
            db.Insert(new ParentSelf { Id = 2, ChildId = 1 });

            // Select the Parent.Id == 2.  LoadSelect should populate the child, but doesn't.
            var q = db.From<ParentSelf>()
                .Take(1)
                .OrderByDescending<ParentSelf>(p => p.Id);

            var results = db.LoadSelect(q);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Child, Is.Not.Null);
            Assert.That(results[0].Child.Value, Is.EqualTo("Lolz"));

            q = db.From<ParentSelf>()
                .Skip(1)
                .OrderBy<ParentSelf>(p => p.Id);

            results = db.LoadSelect(q);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Child, Is.Not.Null);
            Assert.That(results[0].Child.Value, Is.EqualTo("Lolz"));
            results.PrintDump();
        }

        [Test]
        [IgnoreDialect(Tests.Dialect.AnyPostgreSql, "Dapper doesn't know about pgsql naming conventions")]
        public void Can_populate_multiple_POCOs_using_Dappers_QueryMultiple()
        {
            ResetTables();
            AddCustomerWithOrders();

            var q = db.From<Customer>()
                .Join<Customer, CustomerAddress>()
                .Join<Customer, Order>()
                .OrderBy<Order>(x => x.Id)
                .Select("*");

            using (var multi = db.QueryMultiple(q.ToSelectStatement()))
            {
                var tuples = multi.Read<Customer, CustomerAddress, Order, Tuple<Customer, CustomerAddress, Order>>(
                    Tuple.Create).ToList();

                var sb = new StringBuilder();
                foreach (var tuple in tuples)
                {
                    sb.AppendLine("Customer:");
                    sb.AppendLine(tuple.Item1.Dump());
                    sb.AppendLine("Customer Address:");
                    sb.AppendLine(tuple.Item2.Dump());
                    sb.AppendLine("Order:");
                    sb.AppendLine(tuple.Item3.Dump());
                }

                AssertMultiCustomerOrderResults(sb);
            }
        }

        [Test]
        public void Can_populate_multiple_POCOs_using_SelectMulti2()
        {
            ResetTables();
            AddCustomerWithOrders();

            var q = db.From<Customer>()
                .Join<Customer, CustomerAddress>();

            var tuples = db.SelectMulti<Customer, CustomerAddress>(q);

            var sb = new StringBuilder();
            foreach (var tuple in tuples)
            {
                sb.AppendLine("Customer:");
                sb.AppendLine(tuple.Item1.Dump());
                sb.AppendLine("Customer Address:");
                sb.AppendLine(tuple.Item2.Dump());
            }

            Assert.That(sb.ToString().NormalizeNewLines().Trim(), Is.EqualTo(
@"Customer:
{
	Id: 1,
	Name: Customer 1
}
Customer Address:
{
	Id: 1,
	CustomerId: 1,
	AddressLine1: 1 Australia Street,
	Country: Australia
}".NormalizeNewLines()));
        }

        [Test]
        public void Can_populate_multiple_POCOs_using_SelectMulti2_Distinct()
        {
            ResetTables();
            AddCustomerWithOrders();

            var q = db.From<Customer>()
                .Join<Customer, CustomerAddress>();

            var tuples = db.SelectMulti<Customer, CustomerAddress>(q.SelectDistinct());

            var sb = new StringBuilder();
            foreach (var tuple in tuples)
            {
                sb.AppendLine("Customer:");
                sb.AppendLine(tuple.Item1.Dump());
                sb.AppendLine("Customer Address:");
                sb.AppendLine(tuple.Item2.Dump());
            }

            var sql = db.GetLastSql();
            Assert.That(sql, Does.Contain("SELECT DISTINCT"));

            Assert.That(sb.ToString().NormalizeNewLines().Trim(), Is.EqualTo(
                @"Customer:
{
	Id: 1,
	Name: Customer 1
}
Customer Address:
{
	Id: 1,
	CustomerId: 1,
	AddressLine1: 1 Australia Street,
	Country: Australia
}".NormalizeNewLines()));
        }

        [Test]
        public void Can_populate_multiple_POCOs_using_SelectMulti3()
        {
            ResetTables();
            AddCustomerWithOrders();

            var q = db.From<Customer>()
                .Join<Customer, CustomerAddress>()
                .Join<Customer, Order>()
                .Where(x => x.Id == 1)
                .And<CustomerAddress>(x => x.Country == "Australia")
                .OrderBy<Order>(x => x.Id);

            var tuples = db.SelectMulti<Customer, CustomerAddress, Order>(q);

            var sb = new StringBuilder();
            foreach (var tuple in tuples)
            {
                sb.AppendLine("Customer:");
                sb.AppendLine(tuple.Item1.Dump());
                sb.AppendLine("Customer Address:");
                sb.AppendLine(tuple.Item2.Dump());
                sb.AppendLine("Order:");
                sb.AppendLine(tuple.Item3.Dump());
            }
            sb.ToString().Print();
            AssertMultiCustomerOrderResults(sb);
        }

        [Test]
        public void Can_custom_select_from_multiple_joined_tables()
        {
            ResetTables();
            AddCustomerWithOrders();

            var q = db.From<Customer>()
                .Join<Customer, CustomerAddress>()
                .Join<Customer, Order>()
                .Where(x => x.Id == 1)
                .And<CustomerAddress>(x => x.Country == "Australia")
                .OrderByDescending<Order>(x => x.Id)
                .Take(1)
                .Select<Customer,CustomerAddress,Order>((c,a,o) => new {
                    o.Id,
                    c.Name,
                    CustomerId = c.Id,
                    AddressId = a.Id,
                });

            var result = db.Select<(int id, string name, int customerId, int addressId)>(q)[0];
            
            Assert.That(result.id, Is.EqualTo(2));
            Assert.That(result.name, Is.EqualTo("Customer 1"));
            Assert.That(result.customerId, Is.EqualTo(1));
            Assert.That(result.addressId, Is.EqualTo(1));
        }

        private static void AssertMultiCustomerOrderResults(StringBuilder sb)
        {
            Assert.That(Regex.Replace(sb.ToString(), @"\.99[0]+",".99").NormalizeNewLines().Trim(), Is.EqualTo(
                @"Customer:
{
	Id: 1,
	Name: Customer 1
}
Customer Address:
{
	Id: 1,
	CustomerId: 1,
	AddressLine1: 1 Australia Street,
	Country: Australia
}
Order:
{
	Id: 1,
	CustomerId: 1,
	LineItem: Line 1,
	Qty: 1,
	Cost: 1.99
}
Customer:
{
	Id: 1,
	Name: Customer 1
}
Customer Address:
{
	Id: 1,
	CustomerId: 1,
	AddressLine1: 1 Australia Street,
	Country: Australia
}
Order:
{
	Id: 2,
	CustomerId: 1,
	LineItem: Line 2,
	Qty: 2,
	Cost: 2.99
}".NormalizeNewLines()));
        }
    }

    public class ParentSelf
    {
        [PrimaryKey]
        public int Id { get; set; }

        [References(typeof(ChildSelf))]
        public int? ChildId { get; set; }

        [Reference]
        public ChildSelf Child { get; set; }
    }

    public class ChildSelf
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string Value { get; set; }
    }

    [Alias("Table1")]
    public class TABLE_1 : IHasId<int>
    {
        [AutoIncrement]
        [Alias("Key")]
        public int Id { get; set; }

        [Alias("Ena")]
        public string One { get; set; }
    }

    [Alias("Table2")]
    public class TABLE_2 : IHasId<int>
    {
        [AutoIncrement]
        [Alias("Key")]
        public int Id { get; set; }

        [Alias("Tri")]
        public string Three { get; set; }

        [References(typeof(TABLE_1))]
        [Alias("Table1")]
        public int TableOneKey { get; set; }
    }

    [Alias("Table3")]
    public class TABLE_3 : IHasId<int>
    {
        [AutoIncrement]
        [Alias("Key")]
        public int Id { get; set; }

        [Alias("Tri")]
        public string Three { get; set; }

        [References(typeof(TABLE_2))]
        public int? TableTwoKey { get; set; }

        [Reference]
        public TABLE_2 TableTwo { get; set; }
    }

    [Schema("Schema")]
    [Alias("ProjectTask")]
    public class ProjectTask : IHasId<int>
    {
        [Alias("ProjectTaskId")]
        [Index(Unique = true)]
        [AutoIncrement]
        public int Id { get; set; }

        [References(typeof(Project))]
        public int ProjectId { get; set; }

        [Reference]
        public Project Project { get; set; }

        public string Val { get; set; }
    }

    [Schema("Schema")]
    [Alias("Project")]
    public class Project : IHasId<int>
    {
        [Alias("ProjectId")]
        [Index(Unique = true)]
        [AutoIncrement]
        public int Id { get; set; }

        public string Val { get; set; }

    }
}