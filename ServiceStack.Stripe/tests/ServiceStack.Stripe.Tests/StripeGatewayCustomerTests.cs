using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Stripe;
using ServiceStack.Stripe.Types;
using ServiceStack.Text;

namespace Stripe.Tests
{
    /*
     * Customers 
     * https://stripe.com/docs/api/curl#customers
     */
    [TestFixture]
    public class StripeGatewayCustomerTests : TestsBase
    {
        [Test]
        public void Can_Create_Customer()
        {
            var customer = CreateCustomer();

            customer.PrintDump();

            Assert.That(customer.Id, Is.Not.Null);
            Assert.That(customer.Email, Is.EqualTo("test@email.com"));
            Assert.That(customer.Sources.TotalCount, Is.EqualTo(1));
            Assert.That(customer.Sources.Data[0].Name, Is.EqualTo("Test Card"));
            Assert.That(customer.Sources.Data[0].ExpMonth, Is.EqualTo(1));
            Assert.That(customer.Sources.Data[0].ExpYear, Is.EqualTo(2030));

            Assert.That(customer.Shipping.Name, Is.EqualTo("Ship To"));
            Assert.That(customer.Shipping.Phone, Is.EqualTo("555-5555-5555"));
            Assert.That(customer.Shipping.Address.Line1, Is.EqualTo("1 Address Road"));
        }

        [Test]
        public void Can_Create_Customer_with_Card_Token()
        {
            var cardToken = gateway.Post(new CreateStripeToken {
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

            var customer = gateway.Post(new CreateStripeCustomerWithToken
            {
                AccountBalance = 10000,
                Card = cardToken.Id,
                Description = "Description",
                Email = "test@email.com",
            });

            customer.PrintDump();

            Assert.That(customer.Id, Is.Not.Null);
            Assert.That(customer.Email, Is.EqualTo("test@email.com"));
            Assert.That(customer.Sources.TotalCount, Is.EqualTo(1));
            Assert.That(customer.Sources.Data[0].Name, Is.EqualTo("Test Card"));
            Assert.That(customer.Sources.Data[0].ExpMonth, Is.EqualTo(1));
            Assert.That(customer.Sources.Data[0].ExpYear, Is.EqualTo(2030));
        }

        [Test]
        public void Can_Create_Customer_with_conflicting_JsConfig()
        {            
            JsConfig.TextCase = TextCase.CamelCase;

            var customer = CreateCustomer();

            customer.PrintDump();

            Assert.That(customer.Id, Is.Not.Null);
            Assert.That(customer.Email, Is.EqualTo("test@email.com"));
            Assert.That(customer.Sources.TotalCount, Is.EqualTo(1));
            Assert.That(customer.Sources.Data[0].Name, Is.EqualTo("Test Card"));
            Assert.That(customer.Sources.Data[0].ExpMonth, Is.EqualTo(1));
            Assert.That(customer.Sources.Data[0].ExpYear, Is.EqualTo(2030));
        }

        [Test]
        public void Can_Get_Customer()
        {
            var customer = CreateCustomer();

            customer.PrintDump();

            var newCustomer = gateway.Get(new GetStripeCustomer { Id = customer.Id });

            newCustomer.PrintDump();

            Assert.That(newCustomer.Id, Is.EqualTo(customer.Id));
            Assert.That(newCustomer.Email, Is.EqualTo("test@email.com"));
            Assert.That(newCustomer.Sources.TotalCount, Is.EqualTo(1));
            Assert.That(newCustomer.Sources.Data[0].Name, Is.EqualTo("Test Card"));
        }

        [Test]
        public void Can_Update_Customer()
        {
            var customer = CreateCustomer();

            var updatedCustomer = gateway.Post(new UpdateStripeCustomer
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
            Assert.That(updatedCustomer.Sources.TotalCount, Is.EqualTo(1));
            Assert.That(updatedCustomer.Sources.Data[0].Name, Is.EqualTo("Updated Test Card"));
        }

        [Test]
        public void Can_Delete_Customer()
        {
            var customer = CreateCustomer();

            var deletedRef = gateway.Delete(new DeleteStripeCustomer { Id = customer.Id });

            deletedRef.PrintDump();

            Assert.That(deletedRef.Id, Is.EqualTo(customer.Id));
            Assert.That(deletedRef.Deleted);
        }

        [Test]
        public void Can_List_all_Customers()
        {
            var customer = CreateCustomer();

            var customers = gateway.Get(new GetStripeCustomers());

            customers.PrintDump();

            Assert.That(customers.Data.Count, Is.GreaterThan(0));
            Assert.That(customers.Data[0].Id, Is.Not.Null);
        }

        [Test]
        public void Can_Create_and_UpdateCustomer_with_Metadata()
        {
            var request = CreateStripeCustomerRequest();
            request.BusinessVatId = "VatId";
            request.Metadata = new Dictionary<string, string> {
                {"order_id", "1234"},
                {"ref_id", "456"},
            };

            var customer = gateway.Post(request);

            Assert.That(customer.BusinessVatId, Is.EqualTo(request.BusinessVatId));
            Assert.That(customer.Metadata["order_id"], Is.EqualTo(request.Metadata["order_id"]));
            Assert.That(customer.Metadata["ref_id"], Is.EqualTo(request.Metadata["ref_id"]));
            Assert.That(customer.Currency.ToUpper(), Is.EqualTo(Currencies.UnitedStatesDollar));
        }
    }
}