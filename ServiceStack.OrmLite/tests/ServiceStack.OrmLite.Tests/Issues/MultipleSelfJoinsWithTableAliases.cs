﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues
{
    public interface IHaveTenantId
    {
        Guid TenantId { get; }
    }

    public class ContactIssue : IHaveTenantId
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }
    }

    public class Sale : IHaveTenantId
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        [ForeignKey(typeof(ContactIssue), OnDelete = "NO ACTION")]
        public Guid BuyerId { get; set; }

        [ForeignKey(typeof(ContactIssue), OnDelete = "NO ACTION")]
        public Guid SellerId { get; set; }

        public int AmountCents { get; set; }
    }

    public class SaleView
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string BuyerFirstName { get; set; }
        public string BuyerLastName { get; set; }
        public string BuyerInitials { get; set; }
        public string SellerFirstName { get; set; }
        public string SellerLastName { get; set; }
        public string SellerInitials { get; set; }
        public int AmountCents { get; set; }
    }

    public class SaleJson : Sale
    {
        public string Json { get; set; }

        private List<ContactIssue> results;
        public List<ContactIssue> Results => results ??= CustomJsonSerializer.FromJson<List<ContactIssue>>(Json);
    }

    public static class CustomJsonSerializer
    {
        public static T FromJson<T>(string json)
        {
            using var scope = JsConfig.With(new Config { PropertyConvention = PropertyConvention.Lenient });
            return json.FromJson<T>();
        }
    }

    [TestFixtureOrmLite]
    public class MultipleSelfJoinsWithTableAliases : OrmLiteProvidersTestBase
    {
        public MultipleSelfJoinsWithTableAliases(DialectContext context) : base(context) {}

        private static Sale PopulateData(IDbConnection db, Guid tenantId)
        {
            db.DropTable<Sale>();
            db.DropTable<ContactIssue>();

            db.CreateTable<ContactIssue>();
            db.CreateTable<Sale>();

            var buyer = new ContactIssue
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FirstName = "BuyerFirst",
                LastName = "LastBuyer"
            };

            var seller = new ContactIssue
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FirstName = "SellerFirst",
                LastName = "LastSeller"
            };

            db.Insert(buyer, seller);

            var sale = new Sale
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                BuyerId = buyer.Id,
                SellerId = seller.Id,
                AmountCents = 100,
            };

            db.Insert(sale);
            return sale;
        }

        [Test]
        public void Can_use_custom_SqlExpression_to_add_multiple_self_Left_Joins_with_TableAlias()
        {
            using var db = OpenDbConnection();
            var tenantId = Guid.NewGuid();
            var sale = PopulateData(db, tenantId);

            var q = db.From<Sale>()
                .CustomJoin("LEFT JOIN {0} seller on (Sale.{1} = seller.Id)"
                    .Fmt("ContactIssue".SqlTable(DialectProvider), "SellerId".SqlColumn(DialectProvider)))
                .CustomJoin("LEFT JOIN {0} buyer on (Sale.{1} = buyer.Id)"
                    .Fmt("ContactIssue".SqlTable(DialectProvider), "BuyerId".SqlColumn(DialectProvider)))
                .Select(@"Sale.*
                        , buyer.{0} AS BuyerFirstName
                        , buyer.{1} AS BuyerLastName
                        , seller.{0} AS SellerFirstName
                        , seller.{1} AS SellerLastName"
                    .Fmt("FirstName".SqlColumn(DialectProvider), "LastName".SqlColumn(DialectProvider)));

            q.Where(x => x.TenantId == tenantId);

            var sales = db.Select<SaleView>(q);
            Assert.That(sales.Count, Is.EqualTo(1));

            //Alternative
            q = db.From<Sale>()
                .LeftJoin<ContactIssue>((s, c) => s.SellerId == c.Id, db.TableAlias("seller"))
                .LeftJoin<ContactIssue>((s, c) => s.BuyerId == c.Id, db.TableAlias("buyer"))
                .Select<Sale, ContactIssue>((s, c) => new
                {
                    s,
                    BuyerFirstName = Sql.TableAlias(c.FirstName, "buyer"),
                    BuyerLastName = Sql.TableAlias(c.LastName, "buyer"),
                    SellerFirstName = Sql.TableAlias(c.FirstName, "seller"),
                    SellerLastName = Sql.TableAlias(c.LastName, "seller"),
                });

            q.Where(x => x.TenantId == tenantId);

            sales = db.Select<SaleView>(q);
            Assert.That(sales.Count, Is.EqualTo(1));


            var salesView = sales[0];

            //salesView.PrintDump();

            Assert.That(salesView.Id, Is.EqualTo(sale.Id));
            Assert.That(salesView.TenantId, Is.EqualTo(sale.TenantId));
            Assert.That(salesView.AmountCents, Is.EqualTo(sale.AmountCents));
            Assert.That(salesView.BuyerFirstName, Is.EqualTo("BuyerFirst"));
            Assert.That(salesView.BuyerLastName, Is.EqualTo("LastBuyer"));
            Assert.That(salesView.SellerFirstName, Is.EqualTo("SellerFirst"));
            Assert.That(salesView.SellerLastName, Is.EqualTo("LastSeller"));

            q.Select("seller.*, 0 EOT, buyer.*");

            var multi = db.Select<Tuple<ContactIssue, ContactIssue>>(q);
            multi.PrintDump();

            Assert.That(multi[0].Item1.FirstName, Is.EqualTo("SellerFirst"));
            Assert.That(multi[0].Item2.FirstName, Is.EqualTo("BuyerFirst"));
        }

        [Test]
        public void Can_use_CustomSql_with_TableAlias()
        {
            var customFmt = "";
            if (Dialect == Dialect.SqlServer || Dialect == Dialect.SqlServer2012)
                customFmt = "CONCAT(LEFT({0}, 1),LEFT({1},1))";
            else if (Dialect == Dialect.Sqlite)
                customFmt = "substr({0}, 1, 1) || substr({1}, 1, 1)";

            if (string.IsNullOrEmpty(customFmt))
                return;

            using var db = OpenDbConnection();
            var tenantId = Guid.NewGuid();
            var sale = PopulateData(db, tenantId);

            var q = db.From<Sale>()
                .LeftJoin<ContactIssue>((s, c) => s.SellerId == c.Id, db.TableAlias("seller"))
                .LeftJoin<ContactIssue>((s, c) => s.BuyerId == c.Id, db.TableAlias("buyer"))
                .Select<Sale, ContactIssue>((s, c) => new
                {
                    s,
                    BuyerFirstName = Sql.TableAlias(c.FirstName, "buyer"),
                    BuyerLastName = Sql.TableAlias(c.LastName, "buyer"),
                    BuyerInitials = Sql.Custom(customFmt.Fmt("buyer.FirstName", "buyer.LastName")),
                    SellerFirstName = Sql.TableAlias(c.FirstName, "seller"),
                    SellerLastName = Sql.TableAlias(c.LastName, "seller"),
                    SellerInitials = Sql.Custom(customFmt.Fmt("seller.FirstName", "seller.LastName")),
                });

            var sales = db.Select<SaleView>(q);
            var salesView = sales[0];

            Assert.That(salesView.BuyerFirstName, Is.EqualTo("BuyerFirst"));
            Assert.That(salesView.BuyerLastName, Is.EqualTo("LastBuyer"));
            Assert.That(salesView.BuyerInitials, Is.EqualTo("BL"));
            Assert.That(salesView.SellerFirstName, Is.EqualTo("SellerFirst"));
            Assert.That(salesView.SellerLastName, Is.EqualTo("LastSeller"));
            Assert.That(salesView.SellerInitials, Is.EqualTo("SL"));
        }

        [Test]
        public void Can_use_CustomSql_with_TableAlias_and_GroupBy()
        {
            var customFmt = "";
            if ((Dialect & Dialect.AnySqlServer) == Dialect)
                customFmt = "CONCAT(LEFT({0}, 1),LEFT({1},1))";
            else if (Dialect == Dialect.Sqlite)
                customFmt = "substr({0}, 1, 1) || substr({1}, 1, 1)";

            if (string.IsNullOrEmpty(customFmt))
                return;
            // OrmLiteUtils.PrintSql();

            using (var db = OpenDbConnection())
            {
                var tenantId = Guid.NewGuid();
                var sale = PopulateData(db, tenantId);

                var q = db.From<Sale>()
                    .LeftJoin<ContactIssue>((s, c) => s.SellerId == c.Id, db.TableAlias("seller"))
                    .LeftJoin<ContactIssue>((s, c) => s.BuyerId == c.Id, db.TableAlias("buyer"))
                    // .GroupBy<Sale, ContactIssue>((s,c) => new object[] { s.Id, "BuyerFirstName", "BuyerLastName" })
                    // .GroupBy<Sale, ContactIssue>((s,c) => new  { s.Id, BuyerFirstName = "BuyerFirstName", BuyerLastName = "BuyerLastName" })
                    .GroupBy<Sale, ContactIssue>((s,c) => new {
                        s.Id,
                        BuyerFirstName = Sql.TableAlias(c.FirstName, "buyer"),
                        BuyerLastName = Sql.TableAlias(c.LastName, "buyer"),
                        SellerFirstName = Sql.TableAlias(c.FirstName, "seller"),
                        SellerLastName = Sql.TableAlias(c.LastName, "seller"),
                    })
                    .Select<Sale, ContactIssue>((s, c) => new
                    {
                        s.Id,
                        BuyerFirstName = Sql.TableAlias(c.FirstName, "buyer"),
                        BuyerLastName = Sql.TableAlias(c.LastName, "buyer"),
                        BuyerInitials = Sql.Custom(customFmt.Fmt("buyer.FirstName", "buyer.LastName")),
                        SellerFirstName = Sql.TableAlias(c.FirstName, "seller"),
                        SellerLastName = Sql.TableAlias(c.LastName, "seller"),
                        SellerInitials = Sql.Custom(customFmt.Fmt("seller.FirstName", "seller.LastName")),
                    });

                var sales = db.Select<SaleView>(q);
                var salesView = sales[0];

                Assert.That(salesView.BuyerFirstName, Is.EqualTo("BuyerFirst"));
                Assert.That(salesView.BuyerLastName, Is.EqualTo("LastBuyer"));
                Assert.That(salesView.BuyerInitials, Is.EqualTo("BL"));
                Assert.That(salesView.SellerFirstName, Is.EqualTo("SellerFirst"));
                Assert.That(salesView.SellerLastName, Is.EqualTo("LastSeller"));
                Assert.That(salesView.SellerInitials, Is.EqualTo("SL"));
            }
        }

        void AssertTupleResults(List<Tuple<Sale, ContactIssue, ContactIssue>> results)
        {
            var result = results[0];
            var sales = result.Item1;
            var buyer = result.Item2;
            var seller = result.Item3;

            Assert.That(sales.AmountCents, Is.EqualTo(100));
            Assert.That(buyer.FirstName, Is.EqualTo("BuyerFirst"));
            Assert.That(buyer.LastName, Is.EqualTo("LastBuyer"));
            Assert.That(seller.FirstName, Is.EqualTo("SellerFirst"));
            Assert.That(seller.LastName, Is.EqualTo("LastSeller"));
        }

        [Test]
        public void Can_use_Custom_Select_with_Tuples_with_TableAlias()
        {
            using var db = OpenDbConnection();
            var tenantId = Guid.NewGuid();
            var sale = PopulateData(db, tenantId);

            var q = db.From<Sale>()
                .LeftJoin<ContactIssue>((s, c) => s.SellerId == c.Id, db.TableAlias("seller"))
                .LeftJoin<ContactIssue>((s, c) => s.BuyerId == c.Id, db.TableAlias("buyer"))
                .Select("Sale.*, 0 EOT, buyer.*, 0 EOT, seller.*, 0 EOT");

            AssertTupleResults(db.Select<Tuple<Sale, ContactIssue, ContactIssue>>(q));

            q = db.From<Sale>()
                .LeftJoin<ContactIssue>((s, c) => s.SellerId == c.Id, db.TableAlias("seller"))
                .LeftJoin<ContactIssue>((s, c) => s.BuyerId == c.Id, db.TableAlias("buyer"));

            AssertTupleResults(db.SelectMulti<Sale, ContactIssue, ContactIssue>(q, new[] { "Sale.*", "buyer.*", "seller.*" }));
        }

        [Test]
        public async Task Can_use_Custom_Select_with_Tuples_with_TableAlias_Async()
        {
            using var db = await OpenDbConnectionAsync();
            var tenantId = Guid.NewGuid();
            var sale = PopulateData(db, tenantId);

            var q = db.From<Sale>()
                .LeftJoin<ContactIssue>((s, c) => s.SellerId == c.Id, db.TableAlias("seller"))
                .LeftJoin<ContactIssue>((s, c) => s.BuyerId == c.Id, db.TableAlias("buyer"))
                .Select("Sale.*, 0 EOT, buyer.*, 0 EOT, seller.*, 0 EOT");

            AssertTupleResults(await db.SelectAsync<Tuple<Sale, ContactIssue, ContactIssue>>(q));

            q = db.From<Sale>()
                .LeftJoin<ContactIssue>((s, c) => s.SellerId == c.Id, db.TableAlias("seller"))
                .LeftJoin<ContactIssue>((s, c) => s.BuyerId == c.Id, db.TableAlias("buyer"));

            AssertTupleResults(await db.SelectMultiAsync<Sale, ContactIssue, ContactIssue>(q, new[] { "Sale.*", "buyer.*", "seller.*" }));
        }

        [Test]
        public async Task Can_select_custom_result_with_json_type()
        {
            if ((Dialect & Dialect.AnyPostgreSql) != Dialect)
                return;
            
            using var db = await OpenDbConnectionAsync();
            var tenantId = Guid.NewGuid();
            var sale = PopulateData(db, tenantId);
            
            var q = db.From<Sale>(db.TableAlias("s"))
                .LeftJoin<ContactIssue>((s, c) => s.SellerId == c.Id, db.TableAlias("seller"))
                .GroupBy(x => x.Id)
                .Select("s.*, json_agg(seller) as Json");

            var values = db.Select<SaleJson>(q);
            // values.PrintDump();
            var result = values[0].Results[0];
            Assert.That(result.Id, Is.Not.EqualTo(default(Guid)));
            Assert.That(result.TenantId, Is.EqualTo(tenantId));
            Assert.That(result.FirstName, Is.Not.Null);
            Assert.That(result.LastName, Is.Not.Null);
        }
        
        public class ContactIssueJson
        {
            public string Json { get; set; }
        }

        [Test]
        public async Task Can_select_custom_json_result_in_Table_type()
        {
            if ((Dialect & Dialect.AnyPostgreSql) != Dialect)
                return;
            
            using var db = await OpenDbConnectionAsync();
            var tenantId = Guid.NewGuid();
            var sale = PopulateData(db, tenantId);
            
            var q = db.From<Sale>()
                .LeftJoin<ContactIssue>((s, c) => s.SellerId == c.Id, db.TableAlias("seller"))
                .LeftJoin<ContactIssue>((s, c) => s.BuyerId == c.Id, db.TableAlias("buyer"))
                .GroupBy<Sale, ContactIssue>((s,c) => new { SaleId = s.Id, BuyerId = Sql.TableAlias(c.Id, "buyer") })
                .Select<Sale,ContactIssue>((s,c) => new {
                    s, 
                    buyer = Sql.TableAlias(c, "buyer"), 
                    Json = Sql.Custom($"json_agg(seller)"),
                });
            
            var results = db.Select<Tuple<Sale, ContactIssue, ContactIssueJson>>(q);
            Assert.That(results[0].Item3.Json, Does.StartWith("["));
        }

    }
}