using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.Stripe;
using ServiceStack.Stripe.Types;

namespace Stripe.Tests;

public class TestsBase
{
    //Register License Key in SERVICESTACK_LICENSE Environment Variable or in App.config, test account team@
    protected readonly StripeGateway gateway = new StripeGateway("sk_test_Q9XojyuQxzWqe8b2rm6Vodna");

    protected StripeCustomer CreateCustomer()
    {
        var customer = gateway.Post(CreateStripeCustomerRequest());
        return customer;
    }

    protected async Task<StripeCustomer> CreateCustomerAsync()
    {
        var customer = await gateway.PostAsync(CreateStripeCustomerRequest());
        return customer;
    }

    protected static CreateStripeCustomer CreateStripeCustomerRequest()
    {
        return new CreateStripeCustomer
        {
            AccountBalance = 10000,
            Source = new StripeCard
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
            Shipping = new StripeShipping
            {
                Name = "Ship To",
                Phone = "555-5555-5555",
                Address = new StripeAddress
                {
                    Line1 = "1 Address Road",
                    Line2 = "12345",
                    City = "City",
                    State = "NY",
                    Country = "US",
                },
            },
            Description = "Description",
            Email = "test@email.com",
        };
    }

    protected StripeCoupon CreateCoupon()
    {
        var coupon = gateway.Post(new CreateStripeCoupon
        {
            Id = "TEST-COUPON-01",
            Duration = StripeCouponDuration.repeating,
            PercentOff = 20,
            Currency = "usd",
            DurationInMonths = 2,
            RedeemBy = DateTime.UtcNow.AddYears(1),
            MaxRedemptions = 10,
        });
        return coupon;
    }

    protected StripeCoupon GetOrCreateCoupon()
    {
        try
        {
            return gateway.Get(new GetStripeCoupon { Id = "TEST-COUPON-01" });
        }
        catch (StripeException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
                return CreateCoupon();

            throw;
        }
    }

    protected StripePlan GetOrCreatePlan(string id = "TEST-PLAN-01")
    {
        try
        {
            return gateway.Get(new GetStripePlan { Id = id });
        }
        catch (StripeException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
                return CreatePlan(id);

            throw;
        }
    }

    protected StripePlan CreatePlan(string id = "TEST-PLAN-01")
    {
        try
        {
            var product = gateway.Send(new GetStripeProduct { Id = id });
            if (product != null)
            {
                gateway.Send(new DeleteStripeProduct { Id = id });
            }
        }
        catch (StripeException ex)
        {
            if (ex.StatusCode != HttpStatusCode.NotFound)
                throw;
        }

        var plan = gateway.Post(new CreateStripePlan
        {
            Id = id,
            Amount = 10000,
            Currency = "usd",
            Nickname = "Test Plan",
            Product = new StripePlanProduct
            {
                Id = id,
                Name = "Test Plan Product",
            },
            Interval = StripePlanInterval.month,
            IntervalCount = 1,
        });
        return plan;
    }

    protected StripeProduct GetOrCreateProduct(string id = "TEST-PRODUCT-01")
    {
        try
        {
            return gateway.Get(new GetStripeProduct { Id = id });
        }
        catch (StripeException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
                return CreateProduct(id);

            throw;
        }
    }
    protected StripeProduct CreateProduct(string id = "TEST-PRODUCT-01")
    {
        var createStripeProduct = new CreateStripeProduct
        {
            Id = id,
            Name = "Test Product",
            Active = true,
            Attributes = new[] { "foo", "bar" },
            Caption = "Product Caption",
            Description = "Product Description",
            Images = new[] { "http://url.to/img.jpg" },
            Metadata = new Dictionary<string, string>
            {
                {"foo", "bar"}
            },
            PackageDimensions = new StripePackageDimensions
            {
                Height = 100,
                Width = 200,
                Length = 300,
                Weight = 400,
            },
            Shippable = true,
            Type = StripeProductType.good,
            //StatementDescriptor = "Product Descriptor",  only for `service`
            Url = "http://url.to/product",
        };
        var product = gateway.Send(createStripeProduct);
        return product;
    }
}