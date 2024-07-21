using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.UseCase;
using ServiceStack.Text;
using System.Text.RegularExpressions;

namespace ServiceStack.OrmLite.Tests.Async;

[TestFixtureOrmLite]
public class LoadReferencesJoinTestsAsync(DialectContext context) : OrmLiteProvidersTestBase(context)
{
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
    public async Task Can_populate_multiple_POCOs_using_SelectMulti2()
    {
        ResetTables();
        AddCustomerWithOrders();

        var q = db.From<Customer>()
            .Join<Customer, CustomerAddress>();

        var tuples = await db.SelectMultiAsync<Customer, CustomerAddress>(q);

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
    public async Task Can_populate_multiple_POCOs_using_SelectMulti3()
    {
        ResetTables();
        AddCustomerWithOrders();

        var q = db.From<Customer>()
            .Join<Customer, CustomerAddress>()
            .Join<Customer, Order>();

        var tuples = await db.SelectMultiAsync<Customer, CustomerAddress, Order>(q);

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