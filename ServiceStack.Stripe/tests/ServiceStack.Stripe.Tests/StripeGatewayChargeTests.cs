using System;
using System.Net;
using NUnit.Framework;
using ServiceStack.Stripe;
using ServiceStack.Stripe.Types;
using ServiceStack.Text;
using GetStripeCharge = ServiceStack.Stripe.GetStripeCharge;
using UpdateStripeCharge = ServiceStack.Stripe.UpdateStripeCharge;

namespace Stripe.Tests
{
    /*
     * Charges 
     * https://stripe.com/docs/api/curl#charges
     */
    [TestFixture]
    public class StripeGatewayChargeTests : TestsBase
    {
        [Test]
        public void Can_Charge_Customer()
        {
            var customer = CreateCustomer();

            var charge = gateway.Post(new ChargeStripeCustomer
            {
                Amount = 100,
                Customer = customer.Id,
                Currency = "usd",
                Description = "Test Charge Customer",
            });

            charge.PrintDump();

            Assert.That(charge.Id, Is.Not.Null);
            Assert.That(charge.Customer, Is.EqualTo(customer.Id));
            Assert.That(charge.Amount, Is.EqualTo(100));
            //Assert.That(charge.Source.DynamicLast4, Is.EqualTo("4242"));
            Assert.That(charge.Paid, Is.True);
        }

        [Test]
        public void Can_Handle_Charge_Customer_Nothing()
        {
            var customer = CreateCustomer();

            try
            {
                var charge = gateway.Post(new ChargeStripeCustomer
                {
                    Amount = 0,
                    Customer = customer.Id,
                    Currency = "usd",
                    Description = "Test Charge Customer",
                });
            }
            catch (StripeException ex)
            {
                Assert.That(ex.Type, Is.EqualTo("invalid_request_error"));
                Assert.That(ex.Param, Is.EqualTo("amount"));
                Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            }
        }

        [Test]
        public void Can_Handle_Charge_Customer_with_CardDeclined_Code()
        {
            try
            {
                var newCustomerRequest = CreateStripeCustomerRequest();
                //https://stripe.com/docs/testing#cards-responses
                newCustomerRequest.Source.Number = "4000000000000002";
                var customer = gateway.Post(newCustomerRequest);

                Assert.Fail("Should throw");
            }
            catch (StripeException ex)
            {
                Assert.That(ex.Message, Is.EqualTo("Your card was declined."));
                Assert.That(ex.Type, Is.EqualTo("card_error"));
                Assert.That(ex.Code, Is.EqualTo("card_declined"));
                Assert.That(ex.DeclineCode, Is.EqualTo("generic_decline"));
                Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.PaymentRequired));
            }
        }

        [Test]
        public void Can_Handle_Charge_Invalid_Customer()
        {
            try
            {
                var charge = gateway.Post(new ChargeStripeCustomer
                {
                    Amount = 100,
                    Customer = "thisisnotavalidcustomerid",
                    Currency = "usd",
                    Description = "Test Charge Customer",
                });
            }
            catch (StripeException ex)
            {
                Assert.That(ex.Type, Is.EqualTo("invalid_request_error"));
                Assert.That(ex.Param, Is.EqualTo("customer"));
                Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            }
        }

        [Test]
        public void Can_Charge_Customer_with_idempotency_key()
        {
            var customer = CreateCustomer();
            var idempotencyKey = Guid.NewGuid();
            
            var chargeInput = new ChargeStripeCustomer
            {
                Amount = 100,
                Customer = customer.Id,
                Currency = "usd",
                Description = "Test Charge Customer",
            };

            var charge = gateway.Post(chargeInput, idempotencyKey.ToString());

            charge.PrintDump();

            Assert.That(charge.Id, Is.Not.Null);
            Assert.That(charge.Customer, Is.EqualTo(customer.Id));
            Assert.That(charge.Amount, Is.EqualTo(100));
            Assert.That(charge.Source.Last4, Is.EqualTo("4242"));
            Assert.That(charge.Paid, Is.True);

            var charge2 = gateway.Post(chargeInput, idempotencyKey.ToString());
            Assert.That(charge.Id, Is.Not.Null);
            Assert.That(charge2.Id, Is.EqualTo(charge.Id)); //with idempotency key should not create additional charge
            Assert.That(charge.Customer, Is.EqualTo(customer.Id));
            Assert.That(charge.Amount, Is.EqualTo(100));
            Assert.That(charge.Source.Last4, Is.EqualTo("4242"));
            Assert.That(charge.Paid, Is.True);
        }

        [Test]
        public void Can_Charge_Customer_without_idempotency_key()
        {
            var customer = CreateCustomer();            

            var chargeInput = new ChargeStripeCustomer
            {
                Amount = 100,
                Customer = customer.Id,
                Currency = "usd",
                Description = "Test Charge Customer",
            };

            var charge = gateway.Post(chargeInput);

            charge.PrintDump();

            Assert.That(charge.Id, Is.Not.Null);
            Assert.That(charge.Customer, Is.EqualTo(customer.Id));
            Assert.That(charge.Amount, Is.EqualTo(100));
            Assert.That(charge.Source.Last4, Is.EqualTo("4242"));
            Assert.That(charge.Paid, Is.True);

            var charge2 = gateway.Post(chargeInput);
            Assert.That(charge.Id, Is.Not.Null);
            Assert.That(charge2.Id, Is.Not.EqualTo(charge.Id)); //without idempotency key should create additional charge
            Assert.That(charge.Customer, Is.EqualTo(customer.Id));
            Assert.That(charge.Amount, Is.EqualTo(100));
            Assert.That(charge.Source.Last4, Is.EqualTo("4242"));
            Assert.That(charge.Paid, Is.True);
        }

        [Test]
        public void Can_Get_Charge()
        {
            var customer = CreateCustomer();

            var charge = gateway.Post(new ChargeStripeCustomer
            {
                Amount = 100,
                Customer = customer.Id,
                Currency = "usd",
                Description = "Test Charge Customer",
            });

            charge = gateway.Get(new GetStripeCharge { ChargeId = charge.Id });
            charge.PrintDump();

            Assert.That(charge.Id, Is.Not.Null);
            Assert.That(charge.Customer, Is.EqualTo(customer.Id));
            Assert.That(charge.Amount, Is.EqualTo(100));
            Assert.That(charge.Source.Last4, Is.EqualTo("4242"));
            Assert.That(charge.Paid, Is.True);
        }

        [Test]
        public void Can_Update_Charge()
        {
            var customer = CreateCustomer();

            var charge = gateway.Post(new ChargeStripeCustomer
            {
                Amount = 100,
                Customer = customer.Id,
                Currency = "usd",
                Description = "Test Charge Customer",
            });

            charge = gateway.Post(new UpdateStripeCharge
            {
                ChargeId = charge.Id,
                Description = "Updated Charge Description"
            });

            charge.PrintDump();

            Assert.That(charge.Description, Is.EqualTo("Updated Charge Description"));
        }

        [Test]
        public void Can_RefundCharge()
        {
            var customer = CreateCustomer();

            var charge = gateway.Post(new ChargeStripeCustomer
            {
                Amount = 100,
                Customer = customer.Id,
                Currency = "usd",
                Description = "Test Charge Customer",
            });

            //charge.PrintDump();

            Assert.That(charge.Id, Is.Not.Null);
            Assert.That(charge.Customer, Is.EqualTo(customer.Id));
            Assert.That(charge.Amount, Is.EqualTo(100));
            Assert.That(charge.Source.Last4, Is.EqualTo("4242"));
            Assert.That(charge.Paid, Is.True);

            var refundCharge = gateway.Post(new RefundStripeCharge
            {
                ChargeId = charge.Id,
            });

            refundCharge.PrintDump();

            Assert.That(refundCharge.Id, Is.Not.Null);
            Assert.That(refundCharge.Customer, Is.EqualTo(customer.Id));
            Assert.That(refundCharge.Amount, Is.EqualTo(100));
            Assert.That(refundCharge.Paid, Is.True);
            Assert.That(refundCharge.Refunded, Is.True);
            Assert.That(refundCharge.Refunds.TotalCount, Is.EqualTo(1));
            Assert.That(refundCharge.Refunds.Data[0].Amount, Is.EqualTo(100));
        }

        [Test]
        public void Can_CaptureCharge()
        {
            var customer = CreateCustomer();

            var charge = gateway.Post(new ChargeStripeCustomer
            {
                Amount = 100,
                Customer = customer.Id,
                Currency = "usd",
                Description = "Test Charge Customer",
                Capture = false,
            });

            Assert.That(charge.Paid, Is.True);
            Assert.That(charge.Captured, Is.False);

            var captureCharge = gateway.Post(new CaptureStripeCharge
            {
                ChargeId = charge.Id,
            });

            captureCharge.PrintDump();

            Assert.That(captureCharge.Paid, Is.True);
            Assert.That(captureCharge.Captured, Is.True);
        }

        [Test]
        public void Can_List_all_Charges()
        {
            var customer = CreateCustomer();

            var charge = gateway.Post(new ChargeStripeCustomer
            {
                Amount = 100,
                Customer = customer.Id,
                Currency = "usd",
                Description = "Test Charge Customer",
                Capture = false,
            });

            var charges = gateway.Get(new GetStripeCharges());

            charges.PrintDump();

            Assert.That(charges.TotalCount, Is.GreaterThan(0));
            Assert.That(charges.Data[0].Id, Is.Not.Null);
        }

        [Test]
        public void Can_List_Customer_Charges()
        {
            var customer = CreateCustomer();

            var charge = gateway.Post(new ChargeStripeCustomer
            {
                Amount = 100,
                Customer = customer.Id,
                Currency = "usd",
                Description = "Test Charge Customer",
                Capture = false,
            });

            var charges = gateway.Get(new GetStripeCharges
            {
                Customer = customer.Id,
            });

            charges.PrintDump();

            Assert.That(charges.TotalCount, Is.EqualTo(1));
            Assert.That(charges.Data[0].Id, Is.Not.Null);
            Assert.That(charges.Data[0].Customer, Is.EqualTo(customer.Id));
        }
    }
}