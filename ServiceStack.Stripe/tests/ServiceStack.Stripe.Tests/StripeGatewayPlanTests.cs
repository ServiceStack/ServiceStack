using NUnit.Framework;
using ServiceStack.Stripe;
using ServiceStack.Stripe.Types;
using ServiceStack.Text;

namespace Stripe.Tests
{
    [TestFixture]
    public class StripeGatewayPlanTests : TestsBase
    {
        [Test]
        public void Can_Create_Plan()
        {
            var plan = GetOrCreatePlan();
            plan.PrintDump();

            var product = gateway.Send(new GetStripeProduct { Id = plan.Product });
            product.PrintDump();

            Assert.That(plan.Id, Is.EqualTo("TEST-PLAN-01"));
            Assert.That(plan.Nickname, Is.EqualTo("Test Plan"));
            Assert.That(plan.Product, Is.EqualTo("TEST-PLAN-01"));
            Assert.That(product.Name, Is.EqualTo("Test Plan Product"));
            Assert.That(plan.Amount, Is.EqualTo(10000));
            Assert.That(plan.Interval, Is.EqualTo(StripePlanInterval.month));
        }

        [Test]
        public void Can_Get_Plan()
        {
            var plan = GetOrCreatePlan();

            plan = gateway.Get(new GetStripePlan { Id = plan.Id });

            Assert.That(plan.Id, Is.Not.Null);
        }

        [Test]
        public void Can_Update_Plan()
        {
            var plan = GetOrCreatePlan("NEW PLAN");

            var updatedPlan = gateway.Post(new UpdateStripePlan
            {
                Id = plan.Id,
                Product = "TEST-PLAN-01" //Products only be changed on its own
            });

            Assert.That(updatedPlan.Product, Is.EqualTo("TEST-PLAN-01"));
            var product = gateway.Send(new GetStripeProduct { Id = updatedPlan.Product });
            Assert.That(product.Name, Is.EqualTo("Test Plan Product"));

            updatedPlan = gateway.Post(new UpdateStripePlan
            {
                Id = plan.Id,
                Nickname = "NEW PLAN UPDATED",
                TrialPeriodDays = 14,
            });
            Assert.That(updatedPlan.Product, Is.EqualTo("TEST-PLAN-01"));
            Assert.That(updatedPlan.TrialPeriodDays, Is.EqualTo(14));
        }

        // [Test] Can no longer delete prices https://github.com/stripe/stripe-python/issues/658
        public void Can_Delete_All_Plans_and_Products()
        {
            var plans = gateway.Get(new GetStripePlans { Limit = 100 });
            foreach (var plan in plans.Data)
            {
                gateway.Delete(new DeleteStripePlan { Id = plan.Id });
            }

            var products = gateway.Send(new GetStripeProducts { Limit = 100 });
            foreach (var product in products.Data)
            {
                gateway.Send(new DeleteStripeProduct { Id = product.Id });
            }
        }

        [Test]
        public void Can_Get_All_Plans()
        {
            var plan = GetOrCreatePlan();

            var plans = gateway.Get(new GetStripePlans { Limit = 20 });

            Assert.That(plans.Data.Count, Is.GreaterThan(0));
            Assert.That(plans.Data[0].Id, Is.Not.Null);
        }
    }
}