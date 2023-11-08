using System.Linq;
using NUnit.Framework;
using ServiceStack.Stripe;
using ServiceStack.Stripe.Types;
using ServiceStack.Text;

namespace Stripe.Tests
{
    [TestFixture]
    public class StripeGatewayAccountTests : TestsBase
    {
        public void DeleteAllCustomersWithEmail(string email)
        {
            StripeCollection<StripeCustomer> existingCustomers = null;
            do
            {
                existingCustomers = gateway.Send(new GetStripeCustomers
                {
                    Email = email,
                    Limit = 100
                });

                foreach (var customer in existingCustomers.Data)
                {
                    DeleteCustomer(customer);
                }
            } while (existingCustomers.Data.Count >= 100);
        }

        private void DeleteCustomer(StripeCustomer account)
        {
            var stripeId = account.Id;
            try
            {
                $"Deleting Customer: {stripeId} - {account.Email} ".Print();
                gateway.Send(new DeleteStripeCustomer
                {
                    Id = stripeId
                });
            }
            catch (System.Exception ex)
            {
                $"Error trying to delete {stripeId}: {ex.Message}".Print();
            }
        }

        [Test]
        public void Can_Create_Account()
        {
            DeleteAllCustomersWithEmail("test@email.com");

            var response = gateway.Post(new CreateStripeAccount
            {
                Country = "US",
                Email = "test@email.com",
                LegalEntity = new StripeLegalEntity
                {
                    Address = new StripeAddress
                    {
                        Line1 = "1 Highway Rd",
                        City = "Brooklyn",
                        State = "NY",
                        Country = "US",
                        PostalCode = "90210",
                    },
                    Dob = new StripeDate(1980, 1, 1),
                    BusinessName = "Business Name",
                    FirstName = "First",
                    LastName = "Last",
                },
                RequestedCapabilities = new[] {
                    StripeCapability.card_payments,
                    StripeCapability.transfers,
                }, 
            });

            response.PrintDump();

            Assert.That(response.Keys["secret"], Is.Not.Null);
            Assert.That(response.Keys["publishable"], Is.Not.Null);
            Assert.That(response.BusinessName, Is.EqualTo("Business Name"));
            Assert.That(response.Country, Is.EqualTo("US"));
            Assert.That(response.DefaultCurrency.ToUpper(), Is.EqualTo(Currencies.UnitedStatesDollar));
            Assert.That(response.Email, Is.EqualTo("test@email.com"));

            Assert.That(response.LegalEntity.BusinessName, Is.EqualTo("Business Name"));
            Assert.That(response.LegalEntity.FirstName, Is.EqualTo("First"));
            Assert.That(response.LegalEntity.LastName, Is.EqualTo("Last"));

            var address = response.LegalEntity.Address;
            Assert.That(address.Line1, Is.EqualTo("1 Highway Rd"));
            Assert.That(address.City, Is.EqualTo("Brooklyn"));
            Assert.That(address.State, Is.EqualTo("NY"));
            Assert.That(address.Country, Is.EqualTo("US"));
            Assert.That(address.PostalCode, Is.EqualTo("90210"));

            var dob = response.LegalEntity.Dob;
            Assert.That(dob.Year, Is.EqualTo(1980));
            Assert.That(dob.Month, Is.EqualTo(1));
            Assert.That(dob.Day, Is.EqualTo(1));

            var verification = response.Verification;
            Assert.That(verification.DisabledReason, Is.EqualTo("fields_needed"));
            Assert.That(verification.DueBy, Is.Null);
            Assert.That(verification.FieldsNeeded, Is.Not.Null);
            Assert.That(verification.FieldsNeeded.Length, Is.EqualTo(9));
        }
    }
}