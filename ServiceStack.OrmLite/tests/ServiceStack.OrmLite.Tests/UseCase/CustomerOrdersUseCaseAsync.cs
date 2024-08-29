using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.UseCase;

/// <summary>
/// Stand-alone class, No other configs, nothing but POCOs.
/// </summary>
[TestFixtureOrmLite]
public class CustomerOrdersUseCaseAsync(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public async Task Can_run_Customer_Orders_UseCase()
    {
        using IDbConnection db = await OpenDbConnectionAsync();
        //Re-Create all table schemas:
        CustomerOrdersUseCase.RecreateTables(db);

        await db.InsertAsync(new Employee { Id = 1, Name = "Employee 1" });
        await db.InsertAsync(new Employee { Id = 2, Name = "Employee 2" });
        var product1 = new Product { Id = 1, Name = "Product 1", UnitPrice = 10 };
        var product2 = new Product { Id = 2, Name = "Product 2", UnitPrice = 20 };
        await db.SaveAsync(product1, product2);

        var customer = new Customer {
            FirstName = "Orm",
            LastName = "Lite",
            Email = "ormlite@servicestack.net",
            PhoneNumbers =
            {
                { PhoneType.Home, "555-1234" },
                { PhoneType.Work, "1-800-1234" },
                { PhoneType.Mobile, "818-123-4567" },
            },
            Addresses =
            {
                { AddressType.Work, new Address { Line1 = "1 Street", Country = "US", State = "NY", City = "New York", ZipCode = "10101" } },
            },
            CreatedAt = DateTime.UtcNow,
        };

        var customerId = await db.InsertAsync(customer, selectIdentity: true); //Get Auto Inserted Id
        customer = await db.SingleAsync<Customer>(new { customer.Email }); //Query
        Assert.That(customer.Id, Is.EqualTo(customerId));

        //Direct access to System.Data.Transactions:
        using IDbTransaction trans = db.OpenTransaction(IsolationLevel.ReadCommitted);
        var order = new Order {
            CustomerId = customer.Id,
            EmployeeId = 1,
            OrderDate = DateTime.UtcNow,
            Freight = 10.50m,
            ShippingAddress = new Address { Line1 = "3 Street", Country = "US", State = "NY", City = "New York", ZipCode = "12121" },
        };
        await db.SaveAsync(order); //Inserts 1st time

        //order.Id populated on Save().

        var orderDetails = new[] {
            new OrderDetail {
                OrderId = order.Id,
                ProductId = product1.Id,
                Quantity = 2,
                UnitPrice = product1.UnitPrice,
            },
            new OrderDetail {
                OrderId = order.Id,
                ProductId = product2.Id,
                Quantity = 2,
                UnitPrice = product2.UnitPrice,
                Discount = .15m,
            }
        };

        await db.SaveAsync(orderDetails);

        order.Total = orderDetails.Sum(x => x.UnitPrice * x.Quantity * x.Discount) + order.Freight;

        await db.SaveAsync(order); //Updates 2nd Time

        trans.Commit();
    }

}