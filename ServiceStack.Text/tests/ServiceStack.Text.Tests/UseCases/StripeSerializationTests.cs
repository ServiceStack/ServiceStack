using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Stripe;
using ServiceStack.Stripe.Types;

namespace ServiceStack.Text.Tests.UseCases
{
    [TestFixture]
    public class StripeSerializationTests
    {
        public StripeSerializationTests()
        {
            JsConfig.DateHandler = DateHandler.UnixTime;
            JsConfig.PropertyConvention = PropertyConvention.Lenient;
            JsConfig.TextCase = TextCase.SnakeCase;
            QueryStringSerializer.ComplexTypeStrategy = QueryStringStrategy.FormUrlEncoded;
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            JsConfig.Reset();
        }

        [Test]
        public void Serializes_Customer()
        {
            var dto = new CreateStripeCustomer
            {
                Card = new StripeCard
                {
                    Name = "Name",
                    Number = "4242424242424242",
                    Cvc = "123",
                    ExpMonth = 1,
                    ExpYear = 2,
                    AddressLine1 = "1 Address",
                    AddressLine2 = "2 Address",
                    AddressCountry = "Country",
                    AddressState = "State",
                    AddressZip = "12345",
                },
                Coupon = "Coupon",
                Plan = "Plan",
                AccountBalance = 10,
                Description = "Description",
                Email = "Email",
                Quantity = 1,
                TrialEnd = new DateTime(2014, 1, 1),
                Metadata = new Dictionary<string, string> { { "order_id", "1234" } },
            };

            var qs = QueryStringSerializer.SerializeToString(dto);
            qs.Print();
        }

        [Test]
        public void Serializes_Customer_Metadata()
        {
            var dto = new CreateStripeCustomer
            {
                AccountBalance = 100,
                Metadata = new Dictionary<string, string>
                {
                    { "order_id", "1234" },
                    { "ref_id", "456" },
                },
            };

            var qs = QueryStringSerializer.SerializeToString(dto);
            qs.Print();
            Assert.That(qs, Is.EqualTo("account_balance=100&metadata[order_id]=1234&metadata[ref_id]=456"));
        }

        [Test]
        public void QueryString_Params_uses_DataMember_alias()
        {
            var dto = new CancelStripeSubscription { CustomerId = "cid", AtPeriodEnd = true };

            Assert.That(dto.ToGetUrl(), Is.EqualTo("/customers/cid/subscription?at_period_end=True"));

            var dto2 = new GetUpcomingStripeInvoice { Customer = "cid" };

            Assert.That(dto2.ToGetUrl(), Is.EqualTo("/invoices/upcoming?customer=cid"));
        }

        [Test]
        public void Can_convert_Stripe_Invoice()
        {
            var dto = StripeJsonData.Invoice.FromJson<StripeInvoice>();
            dto.PrintDump();
        }

        [Test]
        public void Can_convert_Stripe_Customer()
        {
            var dto = StripeJsonData.Customer.FromJson<StripeCustomer>();
            dto.PrintDump();
        }

        [Test]
        public void Can_convert_Stripe_Coupon()
        {
            var dto = StripeJsonData.Coupon.FromJson<StripeCoupon>();
            dto.PrintDump();
        }

        [Test]
        public void Can_convert_Stripe_Card()
        {
            var dto = StripeJsonData.Card.FromJson<StripeCard>();
            dto.PrintDump();
        }

        [Test]
        public void Can_convert_Stripe_Charge()
        {
            var dto = StripeJsonData.Charge.FromJson<StripeCharge>();
            dto.PrintDump();
        }

        [Test]
        public void Can_serialize_ComplexTypes()
        {
            var dto = new CreateStripeAccount
            {
                Country = "Country",
                Email = "the@email.com",
                LegalEntity = new StripeLegalEntity
                {
                    Dob = new StripeDate
                    {
                        Day = 1,
                        Month = 1,
                        Year = 1970,
                    }
                },
                TosAcceptance = new StripeTosAcceptance
                {
                    Date = DateTime.UtcNow,
                    Ip = "127.0.0.1",
                    UserAgent = "USER AGENT",
                }
            };

            var qs = QueryStringSerializer.SerializeToString(dto);
            qs.Print();

            Assert.That(qs, Does.Contain(
                @"&legal_entity[dob][year]=1970&legal_entity[dob][month]=1&legal_entity[dob][day]=1"));
        }

        public class StripeCreateSubscription
        {
            public string customer { get; set; }

            public Dictionary<string, string> metadata { get; set; }

            public string plan { get; set; }
        }

        [Test]
        public void QueryStringSerializer_emits_empty_string_without_quotes()
        {
            var qs = QueryStringSerializer.SerializeToString(new StripeCreateSubscription
            {
                metadata = new Dictionary<string, string>
                {
                    { "foo", string.Empty },
                    { "bar", "qux" }
                }
            });

            Assert.That(qs, Is.EqualTo("metadata[foo]=&metadata[bar]=qux"));
        }
    }
}