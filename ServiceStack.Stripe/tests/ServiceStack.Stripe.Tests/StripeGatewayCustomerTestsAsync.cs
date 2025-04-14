using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Stripe;
using ServiceStack.Stripe.Types;
using ServiceStack.Text;

namespace Stripe.Tests;

/*
 * Customers
 * https://stripe.com/docs/api/curl#customers
 */
[TestFixture]
public class StripeGatewayCustomerTestsAsync : TestsBase
{
    [Test]
    public async Task Can_Create_Customer()
    {
        var customer = await CreateCustomerAsync();

        customer.PrintDump();

        Assert.That(customer.Id, Is.Not.Null);
        Assert.That(customer.Email, Is.EqualTo("test@email.com"));
        Assert.That(customer.Sources.Data.Count, Is.EqualTo(1));
        Assert.That(customer.Sources.Data[0].Name, Is.EqualTo("Test Card"));
        Assert.That(customer.Sources.Data[0].ExpMonth, Is.EqualTo(1));
        Assert.That(customer.Sources.Data[0].ExpYear, Is.EqualTo(2030));
    }

    [Test]
    public async Task Can_Create_Customer_with_Card_Token()
    {
        var cardToken = await gateway.PostAsync(new CreateStripeToken {
            Card = new StripeCard
            {
                Name = "Test Card",
                Number = "4242424242424242",
                Cvc = "123",
                ExpMonth = 1,
                ExpYear = 2030,
                AddressLine1 = "1 Address Road",
                AddressLine2 = "12345",
                AddressZip = "City",
                AddressState = "NY",
                AddressCountry = "US",
            },
        });

        var customer = await gateway.PostAsync(new CreateStripeCustomerWithToken
        {
            AccountBalance = 10000,
            Card = cardToken.Id,
            Description = "Description",
            Email = "test@email.com",
        });

        customer.PrintDump();

        Assert.That(customer.Id, Is.Not.Null);
        Assert.That(customer.Email, Is.EqualTo("test@email.com"));
        Assert.That(customer.Sources.Data.Count, Is.EqualTo(1));
        Assert.That(customer.Sources.Data[0].Name, Is.EqualTo("Test Card"));
        Assert.That(customer.Sources.Data[0].ExpMonth, Is.EqualTo(1));
        Assert.That(customer.Sources.Data[0].ExpYear, Is.EqualTo(2030));
    }

    [Test]
    public async Task Can_Create_Customer_with_conflicting_JsConfig()
    {
        JsConfig.TextCase = TextCase.CamelCase;

        var customer = await CreateCustomerAsync();

        customer.PrintDump();

        Assert.That(customer.Id, Is.Not.Null);
        Assert.That(customer.Email, Is.EqualTo("test@email.com"));
        Assert.That(customer.Sources.Data.Count, Is.EqualTo(1));
        Assert.That(customer.Sources.Data[0].Name, Is.EqualTo("Test Card"));
        Assert.That(customer.Sources.Data[0].ExpMonth, Is.EqualTo(1));
        Assert.That(customer.Sources.Data[0].ExpYear, Is.EqualTo(2030));
    }

    [Test]
    public async Task Can_Get_Customer()
    {
        var customer = await CreateCustomerAsync();

        customer.PrintDump();

        var newCustomer = await gateway.GetAsync(new GetStripeCustomer { Id = customer.Id });

        newCustomer.PrintDump();

        Assert.That(newCustomer.Id, Is.EqualTo(customer.Id));
        Assert.That(newCustomer.Email, Is.EqualTo("test@email.com"));
        Assert.That(newCustomer.Sources.Data.Count, Is.EqualTo(1));
        Assert.That(newCustomer.Sources.Data[0].Name, Is.EqualTo("Test Card"));
    }

    [Test]
    public async Task Can_Update_Customer()
    {
        var customer = await CreateCustomerAsync();

        var updatedCustomer = await gateway.PostAsync(new UpdateStripeCustomer
        {
            Id = customer.Id,
            Card = new StripeCard
            {
                Id = customer.Sources.Data[0].Id,
                Name = "Updated Test Card",
                Number = "4242424242424242",
                Cvc = "123",
                ExpMonth = 1,
                ExpYear = 2030,
                AddressLine1 = "1 Address Road",
                AddressLine2 = "12345",
                AddressZip = "City",
                AddressState = "NY",
                AddressCountry = "US",
            },
            AccountBalance = 20000,
            Description = "Updated Description",
            Email = "updated@email.com",
        });

        updatedCustomer.PrintDump();

        Assert.That(updatedCustomer.Id, Is.EqualTo(customer.Id));
        Assert.That(updatedCustomer.Email, Is.EqualTo("updated@email.com"));
        Assert.That(updatedCustomer.Sources.Data.Count, Is.EqualTo(1));
        Assert.That(updatedCustomer.Sources.Data[0].Name, Is.EqualTo("Updated Test Card"));
    }

    [Test]
    public async Task Can_Delete_Customer()
    {
        var customer = await CreateCustomerAsync();

        var deletedRef = await gateway.DeleteAsync(new DeleteStripeCustomer { Id = customer.Id });

        deletedRef.PrintDump();

        Assert.That(deletedRef.Id, Is.EqualTo(customer.Id));
        Assert.That(deletedRef.Deleted);
    }

    [Test]
    public async Task Can_List_all_Customers()
    {
        var customer = await CreateCustomerAsync();

        var customers = await gateway.GetAsync(new GetStripeCustomers());

        customers.PrintDump();

        Assert.That(customers.Data.Count, Is.GreaterThan(0));
        Assert.That(customers.Data[0].Id, Is.Not.Null);
    }
}