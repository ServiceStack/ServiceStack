using NUnit.Framework;
using ServiceStack.Stripe;
using ServiceStack.Text;

namespace Stripe.Tests;

/// <summary>
/// https://stripe.com/docs/api/curl#discounts
/// </summary>
[TestFixture]
public class StripeGatewayDiscountsTests : TestsBase
{
    [Test]
    public void Can_Delete_CustomerDiscount()
    {
        var customer = CreateCustomer();
        var coupon = GetOrCreateCoupon();

        gateway.Post(new UpdateStripeCustomer
        {
            Id = customer.Id,
            Coupon = coupon.Id
        });

        var deletedRef = gateway.Delete(new DeleteStripeDiscount { CustomerId = customer.Id });

        deletedRef.PrintDump();

        Assert.That(deletedRef.Deleted, Is.True);
    }
}