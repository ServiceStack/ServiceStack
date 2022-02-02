using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests;
using ServiceStack.OrmLite.Tests.UseCase;
using ServiceStack.Text;
using Customer = ServiceStack.OrmLite.Tests.Customer;
using Order = ServiceStack.OrmLite.Tests.Order;

namespace ServiceStack.OrmLite.PostgreSQL.Tests
{
    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]
    class NormalizeTests : OrmLiteProvidersTestBase
    {
        public NormalizeTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_create_and_populate_tables_without_quotes()
        {
            using (var db = OpenDbConnection())
            {
                ((PostgreSqlDialectProvider) DialectProvider).Normalize = true;

                CustomerOrdersUseCase.DropTables(db); //Has conflicting 'Order' table
                db.DropTable<Order>();
                db.DropTable<CustomerAddress>();
                db.DropTable<Customer>();

                db.CreateTable<Customer>();
                db.CreateTable<CustomerAddress>();
                db.CreateTable<Order>();

                db.GetLastSql().Print();

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
                db.GetLastSql().Print();

                var dbCustomer = db.SingleById<Customer>(customer.Id);
                Assert.That(dbCustomer.Name, Is.EqualTo(customer.Name));
                dbCustomer = db.SqlList<Customer>("select * from Customer where Id = @Id", new { customer.Id })[0];
                Assert.That(dbCustomer.Name, Is.EqualTo(customer.Name));

                var address = db.Single<CustomerAddress>(x => x.CustomerId == customer.Id && x.Id == customer.PrimaryAddress.Id);
                Assert.That(address.Country, Is.EqualTo("Australia"));

                var orders = db.Select<Order>(x => x.CustomerId == customer.Id);
                var totalQty = orders.Sum(x => x.Qty);
                Assert.That(totalQty, Is.EqualTo(3));

                //PostgreSqlDialectProvider.Instance.Normalize = false;
            }
        }
    }
}
