using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.UseCase;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Async
{
    [TestFixtureOrmLite]
    public class LoadReferencesTestsAsync : OrmLiteProvidersTestBase
    {
        public LoadReferencesTestsAsync(DialectContext context) : base(context) {}

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
        }

        [SetUp]
        public void SetUp()
        {
            db.DeleteAll<Order>();
            db.DeleteAll<CustomerAddress>();
            db.DeleteAll<Customer>();
        }

        [OneTimeTearDown]
        public new void TestFixtureTearDown()
        {
            db.Dispose();
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
        public async Task Can_Save_and_Load_References_Async()
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

            await db.SaveAsync(customer);

            Assert.That(customer.Id, Is.GreaterThan(0));
            Assert.That(customer.PrimaryAddress.CustomerId, Is.EqualTo(0));

            await db.SaveReferencesAsync(customer, customer.PrimaryAddress);
            Assert.That(customer.PrimaryAddress.CustomerId, Is.EqualTo(customer.Id));

            await db.SaveReferencesAsync(customer, customer.Orders);
            Assert.That(customer.Orders.All(x => x.CustomerId == customer.Id));

            var dbCustomer = await db.LoadSingleByIdAsync<Customer>(customer.Id);

            dbCustomer.PrintDump();

            Assert.That(dbCustomer.PrimaryAddress, Is.Not.Null);
            Assert.That(dbCustomer.Orders.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task Can_load_only_included_references_async()
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

            await db.SaveAsync(customer);
            Assert.That(customer.Id, Is.GreaterThan(0));

            await db.SaveReferencesAsync(customer, customer.PrimaryAddress);
            Assert.That(customer.PrimaryAddress.CustomerId, Is.EqualTo(customer.Id));

            await db.SaveReferencesAsync(customer, customer.Orders);
            Assert.That(customer.Orders.All(x => x.CustomerId == customer.Id));

            // LoadSelectAsync overload 1
            var dbCustomers = await db.LoadSelectAsync<Customer>(db.From<Customer>().Where(q => q.Id == customer.Id), include: new[] { "PrimaryAddress" });
            Assert.That(dbCustomers.Count, Is.EqualTo(1));
            Assert.That(dbCustomers[0].Name, Is.EqualTo("Customer 1"));
            Assert.That(dbCustomers[0].Orders, Is.Null);
            Assert.That(dbCustomers[0].PrimaryAddress, Is.Not.Null);

            // LoadSelectAsync overload 2
            dbCustomers = await db.LoadSelectAsync<Customer>(q => q.Id == customer.Id, include: new[] { "PrimaryAddress" });
            Assert.That(dbCustomers.Count, Is.EqualTo(1));
            Assert.That(dbCustomers[0].Name, Is.EqualTo("Customer 1"));
            Assert.That(dbCustomers[0].Orders, Is.Null);
            Assert.That(dbCustomers[0].PrimaryAddress, Is.Not.Null);

            // LoadSelectAsync overload 3
            dbCustomers = await db.LoadSelectAsync(db.From<Customer>().Where(x => x.Id == customer.Id), include: new[] { "PrimaryAddress" });
            Assert.That(dbCustomers.Count, Is.EqualTo(1));
            Assert.That(dbCustomers[0].Name, Is.EqualTo("Customer 1"));
            Assert.That(dbCustomers[0].Orders, Is.Null);
            Assert.That(dbCustomers[0].PrimaryAddress, Is.Not.Null);

            // LoadSingleById overload 1
            var dbCustomer = await db.LoadSingleByIdAsync<Customer>(customer.Id, include: new[] { "PrimaryAddress" });
            Assert.That(dbCustomer.Name, Is.EqualTo("Customer 1"));
            Assert.That(dbCustomer.Orders, Is.Null);
            Assert.That(dbCustomer.PrimaryAddress, Is.Not.Null);

            // LoadSingleById overload 2
            dbCustomer = await db.LoadSingleByIdAsync<Customer>(customer.Id, include: x => new { x.PrimaryAddress });
            Assert.That(dbCustomer.Name, Is.EqualTo("Customer 1"));
            Assert.That(dbCustomer.Orders, Is.Null);
            Assert.That(dbCustomer.PrimaryAddress, Is.Not.Null);

            // Invalid field name
            dbCustomers = await db.LoadSelectAsync<Customer>(q => q.Id == customer.Id, include: new[] { "InvalidOption1", "InvalidOption2" });
            Assert.That(dbCustomers.All(x => x.Orders == null));
            Assert.That(dbCustomers.All(x => x.PrimaryAddress == null));

            dbCustomer = await db.LoadSingleByIdAsync<Customer>(customer.Id, include: new[] { "InvalidOption1", "InvalidOption2" });
            Assert.That(dbCustomer.Orders, Is.Null);
            Assert.That(dbCustomer.PrimaryAddress, Is.Null);
        }

        [Test]
        public async Task Can_Save_References_Async()
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

            try
            {
                await db.SaveAsync(customer, references:true);
                Assert.That(customer.Id, Is.GreaterThan(0));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}