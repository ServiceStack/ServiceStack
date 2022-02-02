using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests.UseCase;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests
{
    /// <summary>
    /// Example of adding reference types to a POCO that:
    ///   - Doesn't persist as complex type blob in OrmLite.
    ///   - Doesn't impact other queries using the POCO.
    ///   - Can save and load references independently from itself.
    ///   - Loaded references are serialized in Text serializers.
    /// </summary>
    public class Customer
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }

        [Reference]
        public CustomerAddress PrimaryAddress { get; set; }

        [Reference]
        public List<Order> Orders { get; set; }
    }

    public class CustomerAddress
    {
        [AutoIncrement]
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
    }

    public class Order
    {
        [AutoIncrement]
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string LineItem { get; set; }
        public int Qty { get; set; }
        public decimal Cost { get; set; }
    }

    public class Country
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string CountryName { get; set; }
        public string CountryCode { get; set; }
    }

    /// <summary>
    /// Test POCOs using table aliases and an alias on the foreign key reference
    /// </summary>
    [Alias("Q_Customer")]
    public class AliasedCustomer
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }

        [Reference]
        public AliasedCustomerAddress PrimaryAddress { get; set; }
    }

    [Alias("Q_CustomerAddress")]
    public class AliasedCustomerAddress
    {
        [AutoIncrement]
        public int Id { get; set; }
        [Alias("Q_CustomerId")]
        public int AliasedCustId { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
    }

    /// <summary>
    /// Test POCOs using table aliases and old form foreign key reference which was aliased name
    /// </summary>
    [Alias("QO_Customer")]
    public class OldAliasedCustomer
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }

        [Reference]
        public OldAliasedCustomerAddress PrimaryAddress { get; set; }
    }

    [Alias("QO_CustomerAddress")]
    public class OldAliasedCustomerAddress
    {
        [AutoIncrement]
        public int Id { get; set; }
        public int QO_CustomerId { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
    }

    [Alias("FooCustomer")]
    public class MismatchAliasCustomer
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }

        [Reference]
        public MismatchAliasAddress PrimaryAddress { get; set; }
    }

    [Alias("BarCustomerAddress")]
    public class MismatchAliasAddress
    {
        [AutoIncrement]
        public int Id { get; set; }
        [Alias("BarCustomerId")]
        public int MismatchAliasCustomerId { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
    }

    public class SelfCustomer
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }

        public int? SelfCustomerAddressId { get; set; }

        [Reference]
        public SelfCustomerAddress PrimaryAddress { get; set; }
    }

    public class SelfCustomerAddress
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
    }

    public class MultiSelfCustomer
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }

        [References(typeof(SelfCustomerAddress))]
        public int? HomeAddressId { get; set; }

        [References(typeof(SelfCustomerAddress))]
        public int? WorkAddressId { get; set; }

        [Reference]
        public SelfCustomerAddress HomeAddress { get; set; }

        [Reference]
        public SelfCustomerAddress WorkAddress { get; set; }
    }


    [TestFixtureOrmLite]
    public class LoadReferencesTests : OrmLiteProvidersTestBase
    {
        public LoadReferencesTests(DialectContext context) : base(context) {}

        private IDbConnection db;

        [OneTimeSetUp]
        public new void TestFixtureSetUp()
        {
            db = base.OpenDbConnection();
            CustomerOrdersUseCase.DropTables(db); //Has conflicting 'Order' table

            db.DropAndCreateTable<Order>();
            db.DropAndCreateTable<Customer>();
            db.DropAndCreateTable<CustomerAddress>();
            db.DropAndCreateTable<Country>();
            db.DropAndCreateTable<AliasedCustomer>();
            db.DropAndCreateTable<AliasedCustomerAddress>();
            db.DropAndCreateTable<OldAliasedCustomer>();
            db.DropAndCreateTable<OldAliasedCustomerAddress>();
            db.DropAndCreateTable<MismatchAliasCustomer>();
            db.DropAndCreateTable<MismatchAliasAddress>();

            db.DropTable<SelfCustomer>();
            db.DropTable<MultiSelfCustomer>();
            db.DropTable<SelfCustomerAddress>();

            db.CreateTable<SelfCustomerAddress>();
            db.CreateTable<MultiSelfCustomer>();
            db.CreateTable<SelfCustomer>();
        }

        [SetUp]
        public void SetUp()
        {
            db.DeleteAll<Order>();
            db.DeleteAll<CustomerAddress>();
            db.DeleteAll<Customer>();
            db.DeleteAll<MultiSelfCustomer>();
            db.DeleteAll<SelfCustomerAddress>();
            db.DeleteAll<SelfCustomer>();
        }

        [OneTimeTearDown]
        public new void TestFixtureTearDown()
        {
            db.Dispose();
        }

        [Test]
        public void Does_not_include_complex_reference_type_in_sql()
        {
            db.Select<Customer>();
            Assert.That(db.GetLastSql().NormalizeSql(),
                Is.EqualTo("select id, name from customer"));
        }

        [Test]
        public void Can_Save_and_Load_References()
        {
            var customer = new Customer
            {
                Name = "Customer 1",
                PrimaryAddress = new CustomerAddress
                {
                    AddressLine1 = "1 Humpty Street",
                    City = "Humpty Doo",
                    State = "Northern Territory",
                    Country = "Australia"
                },
                Orders = new[] {
                    new Order { LineItem = "Line 1", Qty = 1, Cost = 1.99m },
                    new Order { LineItem = "Line 2", Qty = 2, Cost = 2.99m },
                }.ToList(),
            };

            db.Save(customer);

            Assert.That(customer.Id, Is.GreaterThan(0));
            Assert.That(customer.PrimaryAddress.CustomerId, Is.EqualTo(0));

            db.SaveReferences(customer, customer.PrimaryAddress);
            Assert.That(customer.PrimaryAddress.CustomerId, Is.EqualTo(customer.Id));

            db.SaveReferences(customer, customer.Orders);
            Assert.That(customer.Orders.All(x => x.CustomerId == customer.Id));

            var dbCustomer = db.LoadSingleById<Customer>(customer.Id);

            dbCustomer.PrintDump();

            Assert.That(dbCustomer.PrimaryAddress, Is.Not.Null);
            Assert.That(dbCustomer.Orders.Count, Is.EqualTo(2));
        }

        [Test]
        public void Can_Save_and_Load_Aliased_References()
        {
            var customer = new AliasedCustomer
            {
                Name = "Customer 1",
                PrimaryAddress = new AliasedCustomerAddress
                {
                    AddressLine1 = "1 Humpty Street",
                    City = "Humpty Doo",
                    State = "Northern Territory",
                    Country = "Australia"
                },
            };

            db.Save(customer);

            Assert.That(customer.Id, Is.GreaterThan(0));
            Assert.That(customer.PrimaryAddress.AliasedCustId, Is.EqualTo(0));

            db.SaveReferences(customer, customer.PrimaryAddress);
            Assert.That(customer.PrimaryAddress.AliasedCustId, Is.EqualTo(customer.Id));

            var dbCustomer = db.LoadSingleById<AliasedCustomer>(customer.Id);

            dbCustomer.PrintDump();

            Assert.That(dbCustomer.PrimaryAddress, Is.Not.Null);
        }

        [Test]
        public void Can_Save_and_Load_Old_Aliased_References()
        {
            var customer = new OldAliasedCustomer
            {
                Name = "Customer 1",
                PrimaryAddress = new OldAliasedCustomerAddress
                {
                    AddressLine1 = "1 Humpty Street",
                    City = "Humpty Doo",
                    State = "Northern Territory",
                    Country = "Australia"
                },
            };

            db.Save(customer);

            Assert.That(customer.Id, Is.GreaterThan(0));
            Assert.That(customer.PrimaryAddress.QO_CustomerId, Is.EqualTo(0));

            db.SaveReferences(customer, customer.PrimaryAddress);
            Assert.That(customer.PrimaryAddress.QO_CustomerId, Is.EqualTo(customer.Id));

            var dbCustomer = db.LoadSingleById<OldAliasedCustomer>(customer.Id);

            dbCustomer.PrintDump();

            Assert.That(dbCustomer.PrimaryAddress, Is.Not.Null);
        }

        [Test]
        public void Can_Save_and_Load_MismatchedAlias_References_using_code_conventions()
        {
            var customer = new MismatchAliasCustomer
            {
                Name = "Customer 1",
                PrimaryAddress = new MismatchAliasAddress
                {
                    AddressLine1 = "1 Humpty Street",
                    City = "Humpty Doo",
                    State = "Northern Territory",
                    Country = "Australia"
                },
            };

            db.Save(customer);

            Assert.That(customer.Id, Is.GreaterThan(0));
            Assert.That(customer.PrimaryAddress.MismatchAliasCustomerId, Is.EqualTo(0));

            db.SaveReferences(customer, customer.PrimaryAddress);
            Assert.That(customer.PrimaryAddress.MismatchAliasCustomerId, Is.EqualTo(customer.Id));

            var dbCustomer = db.LoadSingleById<MismatchAliasCustomer>(customer.Id);

            dbCustomer.PrintDump();

            Assert.That(dbCustomer.PrimaryAddress, Is.Not.Null);
        }

        private Customer AddCustomerWithOrders()
        {
            var customer = GetCustomerWithOrders();

            db.Save(customer, references: true);

            return customer;
        }

        public static Customer GetCustomerWithOrders(string id = "1")
        {
            var customer = new Customer
            {
                Name = "Customer " + id,
                PrimaryAddress = new CustomerAddress
                {
                    AddressLine1 = id + " Humpty Street",
                    City = "Humpty Doo",
                    State = "Northern Territory",
                    Country = "Australia"
                },
                Orders = new[]
                    {
                        new Order {LineItem = "Line 1", Qty = 1, Cost = 1.99m},
                        new Order {LineItem = "Line 2", Qty = 2, Cost = 2.99m},
                    }.ToList(),
            };
            return customer;
        }

        [Test]
        public void Can_SaveAllReferences_then_Load_them()
        {
            var customer = AddCustomerWithOrders();

            Assert.That(customer.Id, Is.GreaterThan(0));
            Assert.That(customer.PrimaryAddress.CustomerId, Is.EqualTo(customer.Id));
            Assert.That(customer.Orders.All(x => x.CustomerId == customer.Id));

            var dbCustomer = db.LoadSingleById<Customer>(customer.Id);

            dbCustomer.PrintDump();

            Assert.That(dbCustomer.PrimaryAddress, Is.Not.Null);
            Assert.That(dbCustomer.Orders.Count, Is.EqualTo(2));
        }

        [Test]
        public void Can_save_and_load_with_null_references()
        {
            var customer = new Customer
            {
                Name = "Customer 1",
                PrimaryAddress = null,
                Orders = null,
            };

            db.Save(customer, references: true);

            Assert.That(customer.Id, Is.GreaterThan(0));

            var dbCustomer = db.LoadSingleById<Customer>(customer.Id);
            Assert.That(dbCustomer.Name, Is.EqualTo("Customer 1"));

            var dbCustomers = db.LoadSelect<Customer>(q => q.Id == customer.Id);
            Assert.That(dbCustomers.Count, Is.EqualTo(1));
            Assert.That(dbCustomers[0].Name, Is.EqualTo("Customer 1"));
        }

        [Test]
        public void Can_save_and_load_self_references_with_null_references()
        {
            var customer = new SelfCustomer
            {
                Name = "Customer 1",
                PrimaryAddress = null,
            };

            db.Save(customer, references: true);

            Assert.That(customer.Id, Is.GreaterThan(0));

            var dbCustomer = db.LoadSingleById<SelfCustomer>(customer.Id);
            Assert.That(dbCustomer.Name, Is.EqualTo("Customer 1"));

            var dbCustomers = db.LoadSelect<SelfCustomer>(q => q.Id == customer.Id);
            Assert.That(dbCustomers.Count, Is.EqualTo(1));
            Assert.That(dbCustomers[0].Name, Is.EqualTo("Customer 1"));
        }

        [Test]
        public void Can_FirstMatchingField_in_JOIN_tables()
        {
            var q = db.From<Customer>()
                      .Join<CustomerAddress>();

            Assert.That(q.FirstMatchingField("Id"), Is.Not.Null);
            Assert.That(q.FirstMatchingField("AddressLine1"), Is.Not.Null);
            Assert.That(q.FirstMatchingField("CustomerId").Item1.Name, Is.EqualTo("CustomerAddress"));
            Assert.That(q.FirstMatchingField("CustomerAddressCity"), Is.Not.Null);
            Assert.That(q.FirstMatchingField("Unknown"), Is.Null);
        }

        [Test]
        public void Can_FirstMatchingField_in_JOIN_tables_with_Aliases()
        {
            var q = db.From<AliasedCustomer>()
                      .Join<AliasedCustomerAddress>();

            Assert.That(q.FirstMatchingField("Id"), Is.Not.Null);
            Assert.That(q.FirstMatchingField("AddressLine1"), Is.Not.Null);
            Assert.That(q.FirstMatchingField("Q_CustomerId").Item1.Name, Is.EqualTo("AliasedCustomerAddress"));
            Assert.That(q.FirstMatchingField("AliasedCustId").Item1.Name, Is.EqualTo("AliasedCustomerAddress"));
            Assert.That(q.FirstMatchingField("Q_CustomerAddressCity"), Is.Not.Null);
            Assert.That(q.FirstMatchingField("Unknown"), Is.Null);
        }

        [Test]
        public void Can_Save_and_Load_Self_References()
        {
            var customer = new SelfCustomer
            {
                Name = "Customer 1",
                PrimaryAddress = new SelfCustomerAddress
                {
                    AddressLine1 = "1 Humpty Street",
                    City = "Humpty Doo",
                    State = "Northern Territory",
                    Country = "Australia"
                },
            };

            db.Save(new SelfCustomer { Name = "Dummy Incrementer" });

            db.Save(customer);

            Assert.That(customer.Id, Is.GreaterThan(0));
            Assert.That(customer.SelfCustomerAddressId, Is.Null);

            db.SaveReferences(customer, customer.PrimaryAddress);
            Assert.That(customer.SelfCustomerAddressId, Is.EqualTo(customer.PrimaryAddress.Id));

            var dbCustomer = db.LoadSingleById<SelfCustomer>(customer.Id);
            Assert.That(dbCustomer.PrimaryAddress, Is.Not.Null);

            customer = new SelfCustomer
            {
                Name = "Customer 2",
                PrimaryAddress = new SelfCustomerAddress
                {
                    AddressLine1 = "2 Humpty Street",
                    City = "Humpty Doo",
                    State = "Northern Territory",
                    Country = "Australia"
                },
            };

            db.Save(customer, references: true);
            Assert.That(customer.SelfCustomerAddressId, Is.EqualTo(customer.PrimaryAddress.Id));

            dbCustomer = db.LoadSingleById<SelfCustomer>(customer.Id);
            Assert.That(dbCustomer.PrimaryAddress, Is.Not.Null);
        }

        [Test]
        public void Can_load_list_of_self_references()
        {
            var customers = new[]
            {
                new SelfCustomer
                {
                    Name = "Customer 1",
                    PrimaryAddress = new SelfCustomerAddress
                    {
                        AddressLine1 = "1 Australia Street",
                        Country = "Australia"
                    },
                },
                new SelfCustomer
                {
                    Name = "Customer 2",
                    PrimaryAddress = new SelfCustomerAddress
                    {
                        AddressLine1 = "2 Prospect Park",
                        Country = "USA"
                    },
                },
            };

            db.Save(new SelfCustomer { Name = "Dummy Incrementer" });

            customers.Each(x =>
                db.Save(x, references: true));

            var results = db.LoadSelect<SelfCustomer>(q => q.SelfCustomerAddressId != null);
            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.All(x => x.PrimaryAddress != null));

            var customer1 = results.First(x => x.Name == "Customer 1");
            Assert.That(customer1.PrimaryAddress.Country, Is.EqualTo("Australia"));

            var customer2 = results.First(x => x.Name == "Customer 2");
            Assert.That(customer2.PrimaryAddress.Country, Is.EqualTo("USA"));

            results = db.LoadSelect<SelfCustomer>(q => q.Name == "Customer 1");
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].PrimaryAddress.Country, Is.EqualTo("Australia"));
        }

        [Test]
        public void Can_support_multiple_self_references()
        {
            var customers = new[]
            {
                new MultiSelfCustomer
                {
                    Name = "Customer 1",
                    HomeAddress = new SelfCustomerAddress
                    {
                        AddressLine1 = "1 Home Street",
                        Country = "Australia"
                    },
                    WorkAddress = new SelfCustomerAddress
                    {
                        AddressLine1 = "1 Work Street",
                        Country = "Australia"
                    },
                },
                new MultiSelfCustomer
                {
                    Name = "Customer 2",
                    HomeAddress = new SelfCustomerAddress
                    {
                        AddressLine1 = "2 Home Park",
                        Country = "USA"
                    },
                    WorkAddress = new SelfCustomerAddress
                    {
                        AddressLine1 = "2 Work Park",
                        Country = "UK"
                    },
                },
            };

            customers.Each(x =>
                db.Save(x, references: true));

            var results = db.LoadSelect<MultiSelfCustomer>(q =>
                q.HomeAddressId != null &&
                q.WorkAddressId != null);

            results.PrintDump();

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].HomeAddress.AddressLine1, Does.Contain("Home"));
            Assert.That(results[0].WorkAddress.AddressLine1, Does.Contain("Work"));
            Assert.That(results[1].HomeAddress.AddressLine1, Does.Contain("Home"));
            Assert.That(results[1].WorkAddress.AddressLine1, Does.Contain("Work"));

            var ukAddress = db.Single<SelfCustomerAddress>(q => q.Country == "UK");
            ukAddress.PrintDump();
            Assert.That(ukAddress.AddressLine1, Is.EqualTo("2 Work Park"));
        }

        [Test]
        public void Can_load_only_included_references()
        {
            var customer = new Customer
            {
                Name = "Customer 1",
                PrimaryAddress = new CustomerAddress
                {
                    AddressLine1 = "1 Humpty Street",
                    City = "Humpty Doo",
                    State = "Northern Territory",
                    Country = "Australia"
                },
                Orders = new[] {
                    new Order { LineItem = "Line 1", Qty = 1, Cost = 1.99m },
                    new Order { LineItem = "Line 2", Qty = 2, Cost = 2.99m },
                }.ToList(),
            };

            db.Save(customer, references: true);
            Assert.That(customer.Id, Is.GreaterThan(0));

            var dbCustomers = db.LoadSelect<Customer>(x => x.Id == customer.Id, include: x => x.PrimaryAddress);
            Assert.That(dbCustomers.Count, Is.EqualTo(1));
            Assert.That(dbCustomers[0].Name, Is.EqualTo("Customer 1"));
            Assert.That(dbCustomers[0].Orders, Is.Null);
            Assert.That(dbCustomers[0].PrimaryAddress, Is.Not.Null);

            dbCustomers = db.LoadSelect<Customer>(q => q.Id == customer.Id, include: new[] { "primaryaddress" });
            Assert.That(dbCustomers.Count, Is.EqualTo(1));
            Assert.That(dbCustomers[0].Name, Is.EqualTo("Customer 1"));
            Assert.That(dbCustomers[0].Orders, Is.Null);
            Assert.That(dbCustomers[0].PrimaryAddress, Is.Not.Null);

            // Test LoadSingleById
            var dbCustomer = db.LoadSingleById<Customer>(customer.Id, include: new[] { "PrimaryAddress" });
            Assert.That(dbCustomer.Name, Is.EqualTo("Customer 1"));
            Assert.That(dbCustomer.Orders, Is.Null);
            Assert.That(dbCustomer.PrimaryAddress, Is.Not.Null);

            dbCustomer = db.LoadSingleById<Customer>(customer.Id, include: new[] { "primaryaddress" });
            Assert.That(dbCustomer.Name, Is.EqualTo("Customer 1"));
            Assert.That(dbCustomer.Orders, Is.Null);
            Assert.That(dbCustomer.PrimaryAddress, Is.Not.Null);

            dbCustomer = db.LoadSingleById<Customer>(customer.Id, include: x => new { x.PrimaryAddress });
            Assert.That(dbCustomer.Name, Is.EqualTo("Customer 1"));
            Assert.That(dbCustomer.Orders, Is.Null);
            Assert.That(dbCustomer.PrimaryAddress, Is.Not.Null);

            dbCustomers = db.LoadSelect<Customer>(q => q.Id == customer.Id, include: new string[0]);
            Assert.That(dbCustomers.All(x => x.Orders == null));
            Assert.That(dbCustomers.All(x => x.PrimaryAddress == null));

            dbCustomer = db.LoadSingleById<Customer>(customer.Id, include: new string[0]);
            Assert.That(dbCustomer.Name, Is.EqualTo("Customer 1"));
            Assert.That(dbCustomer.Orders, Is.Null);
            Assert.That(dbCustomer.PrimaryAddress, Is.Null);

            // Invalid field name
            dbCustomers = db.LoadSelect<Customer>(q => q.Id == customer.Id, include: new[] { "InvalidOption1", "InvalidOption2" });
            Assert.That(dbCustomers.All(x => x.Orders == null));
            Assert.That(dbCustomers.All(x => x.PrimaryAddress == null));

            dbCustomer = db.LoadSingleById<Customer>(customer.Id, include: new[] { "InvalidOption1", "InvalidOption2" });
            Assert.That(dbCustomer.Orders, Is.Null);
            Assert.That(dbCustomer.PrimaryAddress, Is.Null);
        }

        [Test]
        public void Can_load_included_references_via_sql_in_expression()
        {
            var customer = new Customer
            {
                Name = "Customer 1",
                PrimaryAddress = new CustomerAddress
                {
                    AddressLine1 = "1 Humpty Street",
                    City = "Humpty Doo",
                    State = "Northern Territory",
                    Country = "Australia"
                },
                Orders = new[] {
                    new Order { LineItem = "Line 1", Qty = 1, Cost = 1.99m },
                    new Order { LineItem = "Line 2", Qty = 2, Cost = 2.99m },
                }.ToList(),
            };

            db.Save(customer, references: true);

            var customersSubFilter = db.From<Customer>().Select(c => c.Id);
            var orderQuery = db.From<Order>().Where(q => Sql.In(q.CustomerId, customersSubFilter));
            var dbOrders = db.Select(orderQuery);
            Assert.That(dbOrders.Count, Is.EqualTo(2));

            // Negative case
            customersSubFilter = db.From<Customer>().Select(c => c.Id).Where(c => c.Id == -1);
            orderQuery = db.From<Order>().Where(q => Sql.In(q.CustomerId, customersSubFilter));

            dbOrders = db.Select(orderQuery);
            Assert.That(dbOrders.Count, Is.EqualTo(0));

            //Test merge subselect params
            orderQuery = db.From<Order>().Where(q => q.CustomerId >= 1 && Sql.In(q.CustomerId, customersSubFilter));

            Assert.That(orderQuery.Params.Count, Is.EqualTo(2));
            Assert.That(orderQuery.Params[0].Value, Is.EqualTo(1));
            Assert.That(orderQuery.Params[0].ParameterName, Does.EndWith("0"));
            Assert.That(orderQuery.Params[1].Value, Is.EqualTo(-1));
            Assert.That(orderQuery.Params[1].ParameterName, Does.EndWith("1"));

            dbOrders = db.Select(orderQuery);
            Assert.That(dbOrders.Count, Is.EqualTo(0));
        }
    }

}