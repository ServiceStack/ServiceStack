using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// GetSchemaDiff() compares models with their tables in the database, to find what's changed in one and not the
/// other: missing tables, columns and indexes, columns that aren't in the model and columns with a different type,
/// size or nullability. The differences can be logged, applied to the database or written as a migration.
/// </summary>
[TestFixtureOrmLite]
public class SchemaDiffUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public enum InvoiceStatus { Draft, Sent, Paid }

    [EnumAsInt]
    public enum InvoicePriority { Low, High }

    // The table as it was first created
    public static class V1
    {
        public class Invoice
        {
            [AutoIncrement]
            public int Id { get; set; }

            [Required, StringLength(50)]
            public string Reference { get; set; }

            public int CustomerId { get; set; }
            public decimal Total { get; set; }
            public string LegacyCode { get; set; }
        }
    }

    // The model after it's changed
    public class Invoice
    {
        [AutoIncrement]
        public int Id { get; set; }

        [StringLength(200)]
        public string Reference { get; set; } // larger, and no longer required

        [Index]
        public int CustomerId { get; set; }   // has an index

        public decimal Total { get; set; }
        public DateTime? PaidDate { get; set; } // new
        public string Currency { get; set; }    // new
        // LegacyCode was removed
    }

    // A model that needs changes that can fail with the rows of the table
    public static class Strict
    {
        public class Invoice
        {
            [AutoIncrement]
            public int Id { get; set; }

            [Required, StringLength(20)]
            public string Reference { get; set; } // smaller

            public int CustomerId { get; set; }
            public decimal Total { get; set; }

            [Required]
            public string LegacyCode { get; set; } // now required
        }
    }

    // A table with text columns, e.g. one that wasn't created by OrmLite
    public static class Text
    {
        public class Article
        {
            public int Id { get; set; }

            [StringLength(StringLengthAttribute.MaxText)]
            public string Title { get; set; }

            [StringLength(StringLengthAttribute.MaxText)]
            public string Summary { get; set; }

            [StringLength(100)]
            public string Slug { get; set; }
        }
    }

    public class Article
    {
        public int Id { get; set; }
        public string Title { get; set; }

        [StringLength(500)]
        public string Summary { get; set; }

        [StringLength(StringLengthAttribute.MaxText)]
        public string Slug { get; set; }
    }

    // A table with integer columns of a different size to its model
    public static class Sizes
    {
        public class Counter
        {
            public int Id { get; set; }
            public int Views { get; set; }
            public long Total { get; set; }
            public short Level { get; set; }
            public int Score { get; set; }
        }
    }

    public class Counter
    {
        public int Id { get; set; }
        public long Views { get; set; }
        public int Total { get; set; }
        public int Level { get; set; }
        public decimal Score { get; set; }
    }

    public class EveryType
    {
        [AutoIncrement]
        public int Id { get; set; }
        [Required, StringLength(50)]
        public string Code { get; set; }
        public string Name { get; set; }
        [StringLength(StringLengthAttribute.MaxText)]
        public string Notes { get; set; }
        public int Quantity { get; set; }
        public int? Rating { get; set; }
        public long Views { get; set; }
        public decimal Price { get; set; }
        [DecimalLength(10, 2)]
        public decimal Total { get; set; }
        public double Ratio { get; set; }
        public float Weight { get; set; }
        public bool Active { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Shipped { get; set; }
        public DateTimeOffset Modified { get; set; }
        public TimeSpan Duration { get; set; }
        public Guid Uid { get; set; }
        public InvoiceStatus Status { get; set; }
        public InvoicePriority Priority { get; set; }
        public byte[] Data { get; set; }
        public List<string> Tags { get; set; }
        public Dictionary<string, string> Meta { get; set; }
        [Default(5)]
        public int Retries { get; set; }
        [Index]
        public string Email { get; set; }
        [Index(Unique = true)]
        public string Slug { get; set; }
    }

    static bool CanAlterColumns(System.Data.IDbConnection db) => db.GetDialectProvider().Kind != DbKind.Sqlite;

    [Test]
    public void Tables_created_from_their_models_have_no_differences()
    {
        using var db = OpenDbConnection();
        db.DropTable<BookReview>();
        db.DropAndCreateTable<Book>();
        db.CreateTable<BookReview>();
        db.DropAndCreateTable<EveryType>();
        db.DropAndCreateTable<DdlAttributeUseCases.Upload>();
        db.DropAndCreateTable<DdlAttributeUseCases.Shipment>();
        db.DropAndCreateTable<BatchWriteUseCases.Ticket>();

        var diff = db.GetSchemaDiff(typeof(Book), typeof(BookReview), typeof(EveryType),
            typeof(DdlAttributeUseCases.Upload), typeof(DdlAttributeUseCases.Shipment), typeof(BatchWriteUseCases.Ticket));

        Assert.That(diff.Changes, Is.Empty, diff.ToString());
        Assert.That(diff.Warnings, Is.Empty);
        Assert.That(diff.HasChanges, Is.False);
        Assert.That(diff.ToString(), Is.EqualTo("No schema differences"));
    }

    [Test]
    public void System_versioned_tables_have_no_differences()
    {
        using var db = OpenDbConnection();
        try
        {
            db.DropAndCreateTable<SystemVersionedUseCases.PricedItem>();
            db.DropAndCreateTable<SystemVersionedUseCases.AppSetting>();
        }
        catch (NotSupportedException)
        {
            Assert.Ignore("System-versioned tables aren't supported");
        }

        var diff = db.GetSchemaDiff(typeof(SystemVersionedUseCases.PricedItem), typeof(SystemVersionedUseCases.AppSetting));

        Assert.That(diff.Changes, Is.Empty, diff.ToString());
        Assert.That(diff.Warnings, Is.Empty);
    }

    [Test]
    public void Tables_are_found_by_names_that_only_differ_in_case()
    {
        using var db = OpenDbConnection();
        db.DropTable<Invoice>();
        db.CreateTable<V1.Invoice>();

        // e.g. a table that was created as "invoice" for a model named Invoice, which SQL isn't case sensitive to
        var dialect = db.GetDialectProvider();
        if (dialect.Kind != DbKind.Sqlite)
            Assert.Ignore("Only tested for SQLite");

        db.ExecuteSql("ALTER TABLE \"Invoice\" RENAME TO \"invoice_tmp\"");
        db.ExecuteSql("ALTER TABLE \"invoice_tmp\" RENAME TO \"invoice\"");

        Assert.That(db.TableExists<Invoice>());
        Assert.That(db.GetSchemaDiff<Invoice>().Changes.Map(x => x.Type), Does.Not.Contain(SchemaChangeType.CreateTable));
        db.ExecuteSql("DROP TABLE \"invoice\"");
    }

    [Test]
    public void Text_columns_are_not_a_difference_to_properties_with_a_length()
    {
        using var db = OpenDbConnection();
        db.DropTable<Article>();
        db.CreateTable<Text.Article>();

        var diff = db.GetSchemaDiff<Article>();

        // TEXT holds the VARCHAR(n) values of Title and Summary
        Assert.That(diff.Changes.Any(x => x.Field.Name is nameof(Article.Title) or nameof(Article.Summary)), Is.False,
            diff.ToString());

        if (db.GetDialectProvider().Kind == DbKind.Sqlite)
        {
            // SQLite doesn't use the length of a column, so its character types are all the same
            Assert.That(diff.Changes, Is.Empty, diff.ToString());
            return;
        }

        // A VARCHAR(100) doesn't hold the text of Slug
        Assert.That(diff.Changes.Map(x => x.Field.Name), Is.EqualTo(new[] { nameof(Article.Slug) }), diff.ToString());
        Assert.That(diff.Changes[0].Type, Is.EqualTo(SchemaChangeType.AlterColumn));
        Assert.That(diff.Changes[0].DatabaseColumn, Does.Contain("100"));
    }

    [Test]
    public void Integer_columns_of_a_different_size_are_not_a_difference()
    {
        using var db = OpenDbConnection();
        db.DropTable<Counter>();
        db.CreateTable<Sizes.Counter>();

        var diff = db.GetSchemaDiff<Counter>();

        // e.g. a long property with an INTEGER column, or an int property with a BIGINT column.
        // An integer column is a difference to a decimal property
        Assert.That(diff.Changes.Map(x => x.Field.Name), Is.EqualTo(new[] { nameof(Counter.Score) }), diff.ToString());
    }

    [Test]
    public void Find_the_differences_between_a_model_and_its_table()
    {
        using var db = OpenDbConnection();
        db.DropTable<Invoice>();
        db.CreateTable<V1.Invoice>();

        var diff = db.GetSchemaDiff<Invoice>();

        Assert.That(diff.HasChanges);
        var changes = diff.Changes.ToDictionary(x => x.Name.ToLower().Replace("_", ""));

        // New properties need their column
        Assert.That(changes["paiddate"].Type, Is.EqualTo(SchemaChangeType.AddColumn));
        Assert.That(changes["currency"].Type, Is.EqualTo(SchemaChangeType.AddColumn));
        Assert.That(changes["paiddate"].Field.Name, Is.EqualTo(nameof(Invoice.PaidDate)));
        Assert.That(changes["paiddate"].Sql, Does.StartWith("ALTER TABLE"));
        Assert.That(changes["paiddate"].IsDestructive, Is.False);

        // The column is smaller than its property, and doesn't allow the nulls that it does
        var reference = changes["reference"];
        Assert.That(reference.Type, Is.EqualTo(SchemaChangeType.AlterColumn));
        Assert.That(reference.DatabaseColumn, Does.Contain("50").And.EndsWith("NOT NULL"));
        Assert.That(reference.ModelColumn, Does.Contain("200").And.EndsWith(" NULL").And.Not.Contain("NOT NULL"));
        Assert.That(reference.IsDestructive, Is.False);
        Assert.That(reference.Sql != null, Is.EqualTo(CanAlterColumns(db))); // SQLite can't alter a column

        // The column isn't a property, dropping it loses its data
        var legacyCode = changes["legacycode"];
        Assert.That(legacyCode.Type, Is.EqualTo(SchemaChangeType.DropColumn));
        Assert.That(legacyCode.IsDestructive);

        var index = diff.Changes.Single(x => x.Type == SchemaChangeType.CreateIndex);
        Assert.That(index.Name, Is.EqualTo("idx_invoice_customerid").IgnoreCase);
        Assert.That(index.Sql, Does.StartWith("CREATE INDEX") | Does.StartWith("CREATE NONCLUSTERED INDEX"));

        Assert.That(diff.Changes.Count, Is.EqualTo(5));
        Assert.That(diff.Warnings, Is.Empty);
    }

    [Test]
    public void Log_the_differences()
    {
        using var db = OpenDbConnection();
        db.DropTable<Invoice>();
        db.CreateTable<V1.Invoice>();

        // e.g. when the App starts: if (diff.HasChanges) log.Warn(diff.ToString());
        var diff = db.GetSchemaDiff<Invoice>();
        var lines = diff.ToString().Replace("\r", "").Split('\n');

        Assert.That(lines[0], Is.EqualTo("Invoice").IgnoreCase);
        Assert.That(lines.Count(x => x.StartsWith("  + ")), Is.EqualTo(3)); // 2 columns and an index
        Assert.That(lines.Single(x => x.StartsWith("  ~ ")), Does.Contain("50").And.Contain(" -> ").And.Contain("200"));
        Assert.That(lines.Single(x => x.StartsWith("  - ")), Does.Contain("(not in Invoice)"));
        Assert.That(lines.Any(x => x.StartsWith("  + index idx_invoice_customerid", StringComparison.OrdinalIgnoreCase)));

        // Each change also describes itself
        Assert.That(diff.Changes.Single(x => x.Type == SchemaChangeType.DropColumn).Description,
            Does.Match("^Column invoice.legacy_?code isn't in Invoice: ").IgnoreCase);
    }

    [Test]
    public void Tables_that_do_not_exist_are_created()
    {
        using var db = OpenDbConnection();
        db.DropTable<Invoice>();

        var diff = db.GetSchemaDiff<Invoice>();

        var change = diff.Changes.Single();
        Assert.That(change.Type, Is.EqualTo(SchemaChangeType.CreateTable));
        Assert.That(change.Sql, Does.StartWith("CREATE TABLE").And.Contain("idx_invoice_customerid"));
        Assert.That(diff.ToString(), Does.Contain("table isn't in the database"));

        var applied = db.ApplySchemaDiff(diff);

        Assert.That(applied.Count, Is.EqualTo(1));
        Assert.That(db.TableExists<Invoice>());
        Assert.That(db.GetSchemaDiff<Invoice>().Changes, Is.Empty); // with its index
    }

    [Test]
    public void Apply_the_changes_that_keep_the_data_of_a_table()
    {
        using var db = OpenDbConnection();
        db.DropTable<Invoice>();
        db.CreateTable<V1.Invoice>();
        db.Insert(new V1.Invoice { Reference = "INV-1", CustomerId = 1, Total = 10, LegacyCode = "A1" });

        var applied = db.ApplySchemaDiff(db.GetSchemaDiff<Invoice>());

        // Columns and indexes are added and columns are made larger, columns aren't dropped
        Assert.That(applied.Map(x => x.Type), Is.EquivalentTo(CanAlterColumns(db)
            ? new[] { SchemaChangeType.AddColumn, SchemaChangeType.AddColumn, SchemaChangeType.AlterColumn, SchemaChangeType.CreateIndex }
            : new[] { SchemaChangeType.AddColumn, SchemaChangeType.AddColumn, SchemaChangeType.CreateIndex }));

        db.Insert(new Invoice { Reference = "INV-2", CustomerId = 2, Total = 20, PaidDate = new DateTime(2026, 1, 2), Currency = "USD" });
        var invoices = db.Select<Invoice>().OrderBy(x => x.Id).ToList();
        Assert.That(invoices[0].Reference, Is.EqualTo("INV-1"));
        Assert.That(invoices[1].Currency, Is.EqualTo("USD"));
        Assert.That(db.Select<V1.Invoice>().Map(x => x.LegacyCode), Does.Contain("A1"));

        var remaining = db.GetSchemaDiff<Invoice>();
        Assert.That(remaining.Changes.Map(x => x.Type), Is.EquivalentTo(CanAlterColumns(db)
            ? new[] { SchemaChangeType.DropColumn }
            : new[] { SchemaChangeType.AlterColumn, SchemaChangeType.DropColumn }));
    }

    [Test]
    public void Apply_changes_that_lose_data_when_they_are_allowed()
    {
        using var db = OpenDbConnection();
        db.DropTable<Invoice>();
        db.CreateTable<V1.Invoice>();
        db.Insert(new V1.Invoice { Reference = "INV-1", CustomerId = 1, Total = 10, LegacyCode = "A1" });

        var applied = db.ApplySchemaDiff(db.GetSchemaDiff<Invoice>(), allowDestructive: true);

        Assert.That(applied.Any(x => x.Type == SchemaChangeType.DropColumn));
        Assert.That(db.Select<Invoice>().Single().Reference, Is.EqualTo("INV-1"));

        var remaining = db.GetSchemaDiff<Invoice>();
        if (CanAlterColumns(db))
            Assert.That(remaining.Changes, Is.Empty, remaining.ToString());
        else
            Assert.That(remaining.Changes.Single().Sql, Is.Null); // SQLite can't alter a column
    }

    [Test]
    public void Changes_that_can_fail_with_the_rows_of_a_table_are_destructive()
    {
        using var db = OpenDbConnection();
        db.DropTable<Invoice>();
        db.CreateTable<V1.Invoice>();

        var diff = db.GetSchemaDiff<Strict.Invoice>();

        // A smaller column, and a column that no longer allows nulls. SQLite doesn't use the length of a column
        Assert.That(diff.Changes.Map(x => x.Type), Is.All.EqualTo(SchemaChangeType.AlterColumn));
        Assert.That(diff.Changes.Count, Is.EqualTo(CanAlterColumns(db) ? 2 : 1));
        Assert.That(diff.Changes.Map(x => x.IsDestructive), Is.All.True);

        Assert.That(db.ApplySchemaDiff(diff), Is.Empty);
    }

    [Test]
    public void Write_the_changes_as_a_migration()
    {
        using var db = OpenDbConnection();
        db.DropTable<Invoice>();
        db.CreateTable<V1.Invoice>();

        var source = db.GetSchemaDiff<Invoice>().ToMigration("Migration1005", "MyApp.Migrations");

        Assert.That(source, Does.Contain("namespace MyApp.Migrations;"));
        Assert.That(source, Does.Contain("public class Migration1005 : MigrationBase"));

        // The properties that are changed, and its primary key
        Assert.That(source, Does.Contain("public class Invoice"));
        Assert.That(source, Does.Contain("public int Id { get; set; }"));
        Assert.That(source, Does.Contain("public DateTime? PaidDate { get; set; }"));
        Assert.That(source.Contains("[StringLength(200)]"), Is.EqualTo(CanAlterColumns(db)));
        Assert.That(source.Contains("public string Reference { get; set; }"), Is.EqualTo(CanAlterColumns(db)));
        Assert.That(source, Does.Not.Contain("public decimal Total"));

        Assert.That(source, Does.Contain("Db.AddColumn<Invoice>(x => x.PaidDate);"));
        Assert.That(source, Does.Contain("Db.AddColumn<Invoice>(x => x.Currency);"));
        Assert.That(source, Does.Contain("Db.DropColumn<Invoice>(x => x.PaidDate);"));
        Assert.That(source, Does.Contain("Db.DropIndex<Invoice>(\"idx_invoice_customerid\");").IgnoreCase);
        if (CanAlterColumns(db))
            Assert.That(source, Does.Contain("Db.AlterColumn<Invoice>(x => x.Reference);"));
        else
            Assert.That(source, Does.Contain("which can't be altered to VARCHAR(200) NULL in this database"));

        // Columns that aren't in the model may have been renamed, so they're only dropped by a comment
        Assert.That(source, Does.Match(@"// Db\.DropColumn<Invoice>\(""legacy_?code""\);").IgnoreCase);
        Assert.That(source, Does.Match(@"// Db\.RenameColumn<Invoice>\(""legacy_?code"", ""NewName""\);").IgnoreCase);
    }

    [Test]
    public void Write_a_migration_that_creates_a_table()
    {
        using var db = OpenDbConnection();
        db.DropTable<EveryType>();

        var source = db.GetSchemaDiff<EveryType>().ToMigration("Migration1006");

        // It's a guide to review, which is compiled with a warning until it's reviewed
        Assert.That(source, Does.StartWith("// GENERATED BY SCHEMA DIFF: A GUIDE TO REVIEW, NOT A FINISHED MIGRATION"));
        Assert.That(source, Does.Contain("\n#warning Generated by Schema Diff: review this migration"));

        Assert.That(source, Does.Contain("Db.CreateTable<EveryType>();"));
        Assert.That(source, Does.Contain("Db.DropTable<EveryType>();"));
        Assert.That(source, Does.Contain("[AutoIncrement]"));
        Assert.That(source, Does.Contain("[Required]"));
        Assert.That(source, Does.Contain("[StringLength(50)]"));
        Assert.That(source, Does.Contain("[DecimalLength(10, 2)]"));
        Assert.That(source, Does.Contain("[Default(5)]"));
        Assert.That(source, Does.Contain("[Index(Unique = true)]"));
        Assert.That(source, Does.Contain("public int? Rating { get; set; }"));
        Assert.That(source, Does.Contain("public byte[] Data { get; set; }"));
        Assert.That(source, Does.Contain("public List<string> Tags { get; set; }"));
        Assert.That(source, Does.Contain("public Dictionary<string, string> Meta { get; set; }"));
        Assert.That(source, Does.Contain(
            "public ServiceStack.OrmLite.Tests.UseCases.SchemaDiffUseCases.InvoiceStatus Status { get; set; }"));
        Assert.That(source, Does.Not.Contain("namespace "));
    }

    // A table that's managed by ASP.NET Core Identity, not OrmLite
    [Alias("AspNetUserTokens")]
    public class IdentityToken
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }

    [Alias("LegacyOrders")]
    public class LegacyOrder
    {
        public int Id { get; set; }
    }

    [Test]
    public void Ignores_tables_that_are_not_managed_by_OrmLite()
    {
        using var db = OpenDbConnection();
        db.DropTable<IdentityToken>();
        db.DropTable<LegacyOrder>();

        // AspNet* tables are ignored by default, whose table name is after [Alias]
        var diff = db.GetSchemaDiff(typeof(IdentityToken), typeof(LegacyOrder));
        Assert.That(diff.Changes.Map(x => x.ModelType), Is.EqualTo(new[] { typeof(LegacyOrder) }));
        Assert.That(diff.Ignored.Single(), Is.EqualTo("AspNetUserTokens").IgnoreCase.Or.EqualTo("asp_net_user_tokens"));
        Assert.That(diff.ToString(), Does.EndWith("Ignored: " + diff.Ignored[0]));

        // Ignore other tables by name or type
        var options = new SchemaDiffOptions();
        options.IgnoreTables.Add("legacy*");
        diff = db.GetSchemaDiff(options, typeof(IdentityToken), typeof(LegacyOrder));
        Assert.That(diff.HasChanges, Is.False);
        Assert.That(diff.Ignored.Count, Is.EqualTo(2));
        Assert.That(diff.ToString(), Does.StartWith("No schema differences"));

        options = new SchemaDiffOptions { IgnoreTables = [] };
        options.IgnoreTypes.Add(typeof(LegacyOrder));
        diff = db.GetSchemaDiff(options, typeof(IdentityToken), typeof(LegacyOrder));
        Assert.That(diff.Changes.Map(x => x.ModelType), Is.EqualTo(new[] { typeof(IdentityToken) }));
        Assert.That(diff.Ignored.Count, Is.EqualTo(1));

        // Without rules, nothing is ignored
        diff = db.GetSchemaDiff(new SchemaDiffOptions { IgnoreTables = [] }, typeof(IdentityToken), typeof(LegacyOrder));
        Assert.That(diff.Changes.Count, Is.EqualTo(2));
        Assert.That(diff.Ignored, Is.Empty);
    }

    [Test]
    public void Ignored_table_names_match_ignoring_case_where_star_matches_any_characters()
    {
        var options = new SchemaDiffOptions { IgnoreTables = ["AspNet*", "*_audit", "tmp*import*", "Exact"] };
        Assert.That(options.IsIgnoredTable("AspNetUsers"));
        Assert.That(options.IsIgnoredTable("aspnetroles"));
        Assert.That(options.IsIgnoredTable("AspNet"));
        Assert.That(options.IsIgnoredTable("order_audit"));
        Assert.That(options.IsIgnoredTable("tmp_import"));
        Assert.That(options.IsIgnoredTable("TMP_orders_IMPORT_2024"));
        Assert.That(options.IsIgnoredTable("exact"));
        Assert.That(options.IsIgnoredTable("Users"), Is.False);
        Assert.That(options.IsIgnoredTable("MyAspNetUsers"), Is.False);
        Assert.That(options.IsIgnoredTable("audit_order"), Is.False);
        Assert.That(options.IsIgnoredTable("tmp_orders"), Is.False);
        Assert.That(options.IsIgnoredTable("Exactly"), Is.False);
    }
}
