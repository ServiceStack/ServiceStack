using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Stripe;
using ServiceStack.Stripe.Types;
using ServiceStack.Text;

namespace Stripe.Tests
{
    [TestFixture]
    public class StripeGatewayProductTests : TestsBase
    {
        [Test]
        public void Can_delete_all_products()
        {
            var products = gateway.Send(new GetStripeProducts { Limit = 100 });
            foreach (var product in products.Data)
            {
                DeleteProduct(product.Id);
            }
        }

        private void DeleteProduct(string id)
        {
            try
            {
                gateway.Send(new DeleteStripeProduct { Id = id });
            }
            catch (Exception) {}
        }

        [Test]
        public void Can_Create_Product()
        {
            try
            {
                gateway.Delete(new DeleteStripeProduct { Id = "TEST-PRODUCT-01" });
            }
            catch (Exception) { }

            var product = CreateProduct();
            product.PrintDump();
        }

        [Test]
        public void Can_Create_Product_with_conflicting_JsConfig()
        {
            JsConfig.TreatEnumAsInteger = true;
            var prodId = "TEST-PRODUCT-01-JSCONFIG";
            var product = CreateProduct(id: prodId);
            Assert.That(prodId, Is.EqualTo(product.Id));
            DeleteProduct(product.Id);
        }

        [Test]
        public void Can_update_product()
        {
            DeleteProduct("TEST-PRODUCT-02");
            var product = CreateProduct("TEST-PRODUCT-02");

            var updatedProduct = gateway.Send(new UpdateStripeProduct
            {
                Id = product.Id,
                Name = "UPDATED-PRODUCT-NAME",
                Images = new[] { "http://url.to/img.png" },
                Attributes = new[] { "qux" },
                Caption = "Updated Product Caption",
                Description = "Updated Product Description",
                PackageDimensions = new StripePackageDimensions
                {
                    Height = 200,
                    Width = 400,
                    Length = 600,
                    Weight = 800,
                },
                Url = "http://url.to/updated-product",
            });

            updatedProduct.PrintDump();
        }

    }
}