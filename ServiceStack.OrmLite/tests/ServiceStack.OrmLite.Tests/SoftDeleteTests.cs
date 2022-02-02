using System;
using System.Collections.Generic;
using System.Data;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests.UseCase;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests
{
    public class Vendor : ISoftDelete
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }

        [Reference]
        public List<Product> Products { get; set; }
    }

    public class Product : ISoftDelete
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }

        [ForeignKey(typeof(Vendor))]
        public Guid VendorId { get; set; }
    }

    [TestFixtureOrmLite]
    public class SoftDeleteTests : OrmLiteProvidersTestBase
    {
        public SoftDeleteTests(DialectContext context) : base(context) {}

        private static void InitData(IDbConnection db)
        {
            db.DropTable<Product>();
            db.DropTable<Vendor>();
            db.CreateTable<Vendor>();
            db.CreateTable<Product>();

            db.Save(new Vendor
            {
                Id = Guid.NewGuid(),
                Name = "Active Vendor",
                Products = new List<Product>
                {
                    new Product {Id = Guid.NewGuid(), Name = "Active Product"},
                    new Product {Id = Guid.NewGuid(), Name = "Retired Product", IsDeleted = true},
                }
            }, references:true);

            db.Save(new Vendor
            {
                Id = Guid.NewGuid(),
                Name = "Retired Vendor",
                IsDeleted = true,
                Products = new List<Product>
                {
                    new Product {Id = Guid.NewGuid(), Name = "Active Product"},
                    new Product {Id = Guid.NewGuid(), Name = "Retired Product", IsDeleted = true},
                }
            }, references: true);
        }

        [Test]
        public void Can_filter_deleted_products_reference_data()
        {
            using (var db = OpenDbConnection())
            {
                InitData(db);

                var vendors = db.LoadSelect<Vendor>(x => !x.IsDeleted);

                Assert.That(vendors.Count, Is.EqualTo(1));
                Assert.That(vendors[0].Name, Is.EqualTo("Active Vendor"));
                Assert.That(vendors[0].Products.Count, Is.EqualTo(2));
            }
        }

        [Test]
        public void Can_get_active_products_using_merge()
        {
            using (var db = OpenDbConnection())
            {
                InitData(db);

                var vendors = db.Select<Vendor>(x => !x.IsDeleted);
                var products = db.Select(db.From<Product>().Join<Vendor>()
                    .Where(p => !p.IsDeleted)
                    .And<Vendor>(v => !v.IsDeleted));

                var merged = vendors.Merge(products);

                Assert.That(merged.Count, Is.EqualTo(1));
                Assert.That(merged[0].Name, Is.EqualTo("Active Vendor"));
                Assert.That(merged[0].Products.Count, Is.EqualTo(1));
                Assert.That(merged[0].Products[0].Name, Is.EqualTo("Active Product"));
            }
        }

        [Test]
        public void Can_get_active_products_using_SoftDelete_SqlExpression()
        {
            OrmLiteConfig.SqlExpressionSelectFilter = q =>
            {
                if (q.ModelDef.ModelType.HasInterface(typeof(ISoftDelete)))
                {
                    q.Where<ISoftDelete>(x => !x.IsDeleted);
                }
            };

            using (var db = OpenDbConnection())
            {
                InitData(db);

                var vendors = db.LoadSelect<Vendor>();

                Assert.That(vendors.Count, Is.EqualTo(1));
                Assert.That(vendors[0].Name, Is.EqualTo("Active Vendor"));
            }

            OrmLiteConfig.SqlExpressionSelectFilter = null;
        }

        [Test]
        public void Can_get_active_vendor_and_active_references_using_SoftDelete_ref_filter()
        {
            OrmLiteConfig.SqlExpressionSelectFilter = q =>
            {
                if (q.ModelDef.ModelType.HasInterface(typeof(ISoftDelete)))
                {
                    q.Where<ISoftDelete>(x => !x.IsDeleted);
                }
            };

            OrmLiteConfig.LoadReferenceSelectFilter = (type, sql) =>
            {
                var meta = type.GetModelMetadata();
                if (type.HasInterface(typeof(ISoftDelete)))
                {
                    var sqlFalse = DialectProvider.SqlBool(false);
                    sql += $" AND ({meta.ModelName.SqlTable(DialectProvider)}.{"IsDeleted".SqlColumn(DialectProvider)} = {sqlFalse})";
                }

                return sql;
            };

            using (var db = OpenDbConnection())
            {
                InitData(db);

                var vendors = db.LoadSelect<Vendor>();

                Assert.That(vendors.Count, Is.EqualTo(1));
                Assert.That(vendors[0].Name, Is.EqualTo("Active Vendor"));
                Assert.That(vendors[0].Products.Count, Is.EqualTo(1));
            }

            OrmLiteConfig.SqlExpressionSelectFilter = null;
            OrmLiteConfig.LoadReferenceSelectFilter = null;
        }

        [Test]
        public void Can_get_single_vendor_and__load_active_references_using_soft_delete_ref_filter()
        {
            OrmLiteConfig.LoadReferenceSelectFilter = (type, sql) =>
            {
                var meta = type.GetModelMetadata();
                if (type.HasInterface(typeof(ISoftDelete)))
                {
                    var sqlFalse = DialectProvider.SqlBool(false);
                    sql += $" AND ({meta.ModelName.SqlTable(DialectProvider)}.{"IsDeleted".SqlColumn(DialectProvider)} = {sqlFalse})";
                }

                return sql;
            };

            using (var db = OpenDbConnection())
            {
                InitData(db);
                

                var vendor = db.Single<Vendor>(v=>v.Name == "Active Vendor");
                db.LoadReferences(vendor);
                
                Assert.That(vendor.Name, Is.EqualTo("Active Vendor"));
                Assert.That(vendor.Products.Count, Is.EqualTo(1));
            }

            OrmLiteConfig.LoadReferenceSelectFilter = null;
        }
    }
}