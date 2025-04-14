using System;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Stripe;
using ServiceStack.Stripe.Types;
using ServiceStack.Text;
using GetStripeCharge = ServiceStack.Stripe.GetStripeCharge;
using UpdateStripeCharge = ServiceStack.Stripe.UpdateStripeCharge;

namespace Stripe.Tests;

/*
 * Charges
 * https://stripe.com/docs/api/curl#charges
 */
[TestFixture]
public class StripeGatewayChargeTestsAsync : TestsBase
{
    [Test]
    public async Task Can_Charge_Customer()
    {
        var customer = await CreateCustomerAsync();

        var charge = await gateway.PostAsync(new ChargeStripeCustomer
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
        Assert.That(charge.Source.Last4, Is.EqualTo("4242"));
        Assert.That(charge.Paid, Is.True);
    }

    [Test]
    public async Task Can_Handle_Charge_Customer_Nothing()
    {
        var customer = await CreateCustomerAsync();

        try
        {
            var charge = await gateway.PostAsync(new ChargeStripeCustomer
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
    public async Task Can_Handle_Charge_Invalid_Customer()
    {
        try
        {
            var charge = await gateway.PostAsync(new ChargeStripeCustomer
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
    public async Task Can_Charge_Customer_with_idempotency_key()
    {
        var customer = await CreateCustomerAsync();
        var idempotencyKey = Guid.NewGuid();

        var chargeInput = new ChargeStripeCustomer
        {
            Amount = 100,
            Customer = customer.Id,
            Currency = "usd",
            Description = "Test Charge Customer",
        };

        var charge = await gateway.PostAsync(chargeInput, idempotencyKey.ToString());

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
    public async Task Can_Charge_Customer_without_idempotency_key()
    {
        var customer = await CreateCustomerAsync();

        var chargeInput = new ChargeStripeCustomer
        {
            Amount = 100,
            Customer = customer.Id,
            Currency = "usd",
            Description = "Test Charge Customer",
        };

        var charge = await gateway.PostAsync(chargeInput);

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
    public async Task Can_Get_Charge()
    {
        var customer = await CreateCustomerAsync();

        var charge = await gateway.PostAsync(new ChargeStripeCustomer
        {
            Amount = 100,
            Customer = customer.Id,
            Currency = "usd",
            Description = "Test Charge Customer",
        });

        charge = await gateway.GetAsync(new GetStripeCharge { ChargeId = charge.Id });
        charge.PrintDump();

        Assert.That(charge.Id, Is.Not.Null);
        Assert.That(charge.Customer, Is.EqualTo(customer.Id));
        Assert.That(charge.Amount, Is.EqualTo(100));
        Assert.That(charge.Source.Last4, Is.EqualTo("4242"));
        Assert.That(charge.Paid, Is.True);
    }

    [Test]
    public async Task Can_Update_Charge()
    {
        var customer = await CreateCustomerAsync();

        var charge = await gateway.PostAsync(new ChargeStripeCustomer
        {
            Amount = 100,
            Customer = customer.Id,
            Currency = "usd",
            Description = "Test Charge Customer",
        });

        charge = await gateway.PostAsync(new UpdateStripeCharge
        {
            ChargeId = charge.Id,
            Description = "Updated Charge Description"
        });

        charge.PrintDump();

        Assert.That(charge.Description, Is.EqualTo("Updated Charge Description"));
    }

    [Test]
    public async Task Can_RefundCharge()
    {
        var customer = await CreateCustomerAsync();

        var charge = await gateway.PostAsync(new ChargeStripeCustomer
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

        var refundCharge = await gateway.PostAsync(new RefundStripeCharge
        {
            ChargeId = charge.Id,
        });

        refundCharge.PrintDump();

        Assert.That(refundCharge.Id, Is.Not.Null);
        Assert.That(refundCharge.Customer, Is.EqualTo(customer.Id));
        Assert.That(refundCharge.Amount, Is.EqualTo(100));
        Assert.That(refundCharge.Paid, Is.True);
        Assert.That(refundCharge.Refunded, Is.True);
        Assert.That(refundCharge.Refunds.Data.Count, Is.EqualTo(1));
        Assert.That(refundCharge.Refunds.Data[0].Amount, Is.EqualTo(100));
    }

    [Test]
    public async Task Can_CaptureCharge()
    {
        var customer = await CreateCustomerAsync();

        var charge = await gateway.PostAsync(new ChargeStripeCustomer
        {
            Amount = 100,
            Customer = customer.Id,
            Currency = "usd",
            Description = "Test Charge Customer",
            Capture = false,
        });

        Assert.That(charge.Paid, Is.True);
        Assert.That(charge.Captured, Is.False);

        var captureCharge = await gateway.PostAsync(new CaptureStripeCharge
        {
            ChargeId = charge.Id,
        });

        captureCharge.PrintDump();

        Assert.That(captureCharge.Paid, Is.True);
        Assert.That(captureCharge.Captured, Is.True);
    }

    [Test]
    public async Task Can_List_all_Charges()
    {
        var customer = await CreateCustomerAsync();

        var charge = await gateway.PostAsync(new ChargeStripeCustomer
        {
            Amount = 100,
            Customer = customer.Id,
            Currency = "usd",
            Description = "Test Charge Customer",
            Capture = false,
        });

        var charges = await gateway.GetAsync(new GetStripeCharges());

        charges.PrintDump();

        Assert.That(charges.Data.Count, Is.GreaterThan(0));
        Assert.That(charges.Data[0].Id, Is.Not.Null);
    }

    [Test]
    public async Task Can_List_Customer_Charges()
    {
        var customer = await CreateCustomerAsync();

        var charge = await gateway.PostAsync(new ChargeStripeCustomer
        {
            Amount = 100,
            Customer = customer.Id,
            Currency = "usd",
            Description = "Test Charge Customer",
            Capture = false,
        });

        var charges = await gateway.GetAsync(new GetStripeCharges
        {
            Customer = customer.Id,
        });

        charges.PrintDump();

        Assert.That(charges.Data.Count, Is.EqualTo(1));
        Assert.That(charges.Data[0].Id, Is.Not.Null);
        Assert.That(charges.Data[0].Customer, Is.EqualTo(customer.Id));
    }
}