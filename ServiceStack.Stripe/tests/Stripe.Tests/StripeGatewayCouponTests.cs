using NUnit.Framework;
using ServiceStack.Stripe;
using ServiceStack.Stripe.Types;
using ServiceStack.Text;

namespace Stripe.Tests
{
    [TestFixture]
    public class StripeGatewayCouponTests : TestsBase
    {
        [Test]
        public void Can_Create_Coupon()
        {
            var coupon = GetOrCreateCoupon();

            coupon.PrintDump();

            Assert.That(coupon.Id, Is.EqualTo("TEST-COUPON-01"));
            Assert.That(coupon.PercentOff.Value, Is.EqualTo(20));
            Assert.That(coupon.Duration, Is.EqualTo(StripeCouponDuration.repeating));
            Assert.That(coupon.MaxRedemptions.Value, Is.EqualTo(10));
            Assert.That(coupon.DurationInMonths.Value, Is.EqualTo(2));
        }

        [Test]
        public void Can_Get_Coupon()
        {
            var coupon = GetOrCreateCoupon();

            coupon = gateway.Get(new GetStripeCoupon { Id = coupon.Id });

            Assert.That(coupon, Is.Not.Null);
        }

        [Test]
        public void Can_Delete_All_Coupons()
        {
            var plans = gateway.Get(new GetStripeCoupons { Limit = 100 });
            foreach (var plan in plans.Data)
            {
                gateway.Delete(new DeleteStripeCoupon { Id = plan.Id });
            }
        }

        [Test]
        public void Can_Get_All_Coupons()
        {
            var coupon = GetOrCreateCoupon();

            var coupons = gateway.Get(new GetStripeCoupons { Limit = 20 });

            Assert.That(coupons.Data.Count, Is.GreaterThan(0));
            Assert.That(coupons.Data[0].Id, Is.Not.Null);
        }
    }
}