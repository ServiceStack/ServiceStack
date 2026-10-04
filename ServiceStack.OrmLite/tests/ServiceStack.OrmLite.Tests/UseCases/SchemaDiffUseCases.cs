using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// GetSchemaDiff() compares models with their tables in the database, to find what's changed in one and not the
/// other: missing tables, columns, indexes, foreign keys and unique and check constraints, the ones that aren't in
/// the model, columns with a different type, size, nullability or default value, and primary keys of other columns. The differences can be logged, applied to the database or written as a migration.
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
        CreateOrderTables<DiffOrder>(db);

        var diff = db.GetSchemaDiff(typeof(Book), typeof(BookReview), typeof(EveryType),
            typeof(DdlAttributeUseCases.Upload), typeof(DdlAttributeUseCases.Shipment), typeof(BatchWriteUseCases.Ticket),
            typeof(DiffCustomer), typeof(DiffWarehouse), typeof(DiffOrder));

        Assert.That(diff.Changes, Is.Empty, diff.ToString());
        Assert.That(diff.Warnings, Is.Empty);
        Assert.That(diff.HasChanges, Is.False);
        Assert.That(diff.ToString(), Is.EqualTo("No schema differences"));

        // A filtered unique index, which MySQL doesn't have
        if (db.GetDialectProvider().Kind == DbKind.MySql)
            return;
        db.DropAndCreateTable<DdlAttributeUseCases.Subscriber>();
        diff = db.GetSchemaDiff<DdlAttributeUseCases.Subscriber>();
        Assert.That(diff.Changes, Is.Empty, diff.ToString());
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
        if (db.GetDialectProvider().Kind == DbKind.Sqlite)
        {
            // SQLite creates DateTime columns as text, so PaidDate is a column of the same type as LegacyCode too
            Assert.That(source, Does.Match(@"// Db\.RenameColumn<Invoice>\(""legacy_?code"", ""NewName""\);").IgnoreCase);
            return;
        }
        // LegacyCode is likely renamed to Currency, the only new column of its type
        Assert.That(source, Does.Match(@"// Db\.RenameColumn<Invoice>\(""legacy_?code"", ""currency""\);").IgnoreCase);
        Assert.That(source, Does.Match(@"// Db\.RenameColumn<Invoice>\(x => x\.Currency, ""legacy_?code""\);").IgnoreCase);
    }

    public static class Before
    {
        [CompositeIndex(nameof(Reference), nameof(Status), Name = "ix_shipment_by_status")]
        public class Shipment
        {
            [AutoIncrement]
            public int Id { get; set; }

            [Index(Name = "ix_shipment_reference")]
            public string Reference { get; set; }

            [Index(Name = "ix_shipment_carrier")]
            public string Carrier { get; set; }

            [Unique]
            public string TrackingCode { get; set; }

            public string Status { get; set; }
            public int Weight { get; set; }
        }

        public class DiffOrder
        {
            [AutoIncrement]
            public int Id { get; set; }

            [ForeignKey(typeof(DiffCustomer))]
            public int CustomerId { get; set; }

            [ForeignKey(typeof(DiffWarehouse))]
            public int? WarehouseId { get; set; }

            public int? BillingCustomerId { get; set; }

            [Default(1)]
            public int Quantity { get; set; }

            [Default("'draft'"), StringLength(20)]
            public string Status { get; set; }

            public int Priority { get; set; }

            [Default(OrmLiteVariables.SystemUtc)]
            public DateTime Created { get; set; }
        }

        [UniqueConstraint(nameof(Sku), nameof(Region))]
        public class DiffProduct
        {
            [AutoIncrement]
            public int Id { get; set; }

            [Unique]
            public string Code { get; set; }

            public string Barcode { get; set; }
            public string Sku { get; set; }
            public string Region { get; set; }

            [CheckConstraint("Qty >= 0")]
            public int Qty { get; set; }

            [CheckConstraint("Price > 0")]
            public decimal Price { get; set; }

            public int Rating { get; set; }
        }

        public class DiffKey
        {
            [PrimaryKey]
            public string Code { get; set; }
            public int Id { get; set; }
        }

        public class DiffEvent
        {
            [AutoIncrement]
            public int Id { get; set; }

            [Index(Name = "ix_diff_event_kind", Where = "{Kind} > 0")]
            public int Kind { get; set; }

            [Index(Name = "ix_diff_event_user", Include = [nameof(Status)])]
            public int UserId { get; set; }

            public int Status { get; set; }
        }
    }

    [UniqueConstraint(nameof(Region), nameof(Sku))] // the same columns in another order
    public class DiffProduct
    {
        [AutoIncrement]
        public int Id { get; set; }

        public string Code { get; set; } // no longer unique

        [Unique] // now unique
        public string Barcode { get; set; }

        public string Sku { get; set; }
        public string Region { get; set; }

        [CheckConstraint("Qty > 0")] // another condition
        public int Qty { get; set; }

        public decimal Price { get; set; } // no longer checked

        [CheckConstraint("Rating BETWEEN 1 AND 5")] // now checked
        public int Rating { get; set; }
    }

    public class DiffKey
    {
        [PrimaryKey] // was Code
        public int Id { get; set; }
        public string Code { get; set; }
    }

    public class DiffEvent
    {
        [AutoIncrement]
        public int Id { get; set; }

        [Index(Name = "ix_diff_event_kind", Where = "{Kind} > 1")] // another condition
        public int Kind { get; set; }

        [Index(Name = "ix_diff_event_user", Include = [nameof(Status), nameof(Kind)])] // includes another column
        public int UserId { get; set; }

        public int Status { get; set; }
    }

    static bool IsUnique(SchemaChange x) =>
        (x.ModelColumn ?? x.DatabaseColumn)?.StartsWith("UNIQUE") == true && x.Type is SchemaChangeType.AddConstraint
            or SchemaChangeType.AlterConstraint or SchemaChangeType.DropConstraint;
    static bool IsCheck(SchemaChange x) =>
        (x.ModelColumn ?? x.DatabaseColumn)?.StartsWith("CHECK") == true && x.Type is SchemaChangeType.AddConstraint
            or SchemaChangeType.AlterConstraint or SchemaChangeType.DropConstraint;

    [Test]
    public void Compare_the_columns_of_unique_constraints()
    {
        using var db = OpenDbConnection();
        db.DropTable<DiffProduct>();
        db.CreateTable<Before.DiffProduct>();

        var diff = db.GetSchemaDiff<DiffProduct>();
        var uniques = diff.Changes.Where(IsUnique).ToList();
        // [UniqueConstraint] of the same columns in another order is the same constraint
        Assert.That(uniques.Count, Is.EqualTo(2), diff.ToString());

        // Rows can have the same values
        var barcode = uniques.Single(x => x.Type == SchemaChangeType.AddConstraint);
        Assert.That(barcode.ModelColumn, Is.EqualTo("UNIQUE (barcode)").IgnoreCase);
        Assert.That(barcode.IsDestructive, Is.True);

        // [Unique] constraints are named by the database
        var code = uniques.Single(x => x.Type == SchemaChangeType.DropConstraint);
        Assert.That(code.DatabaseColumn, Is.EqualTo("UNIQUE (code)").IgnoreCase);
        Assert.That(code.IsDestructive, Is.True);
        Assert.That(diff.ToString(), Does.Contain("+ constraint "));
        Assert.That(diff.ToString(), Does.Contain("- constraint "));

        if (!CanAlterColumns(db))
        {
            // SQLite can only change the constraints of a table by creating it again
            Assert.That(uniques.All(x => x.Sql == null));
            return;
        }

        db.ApplySchemaDiff(diff, allowDestructive: true);
        diff = db.GetSchemaDiff<DiffProduct>();
        Assert.That(diff.Changes.Where(IsUnique), Is.Empty, diff.ToString());

        // SQL Server's unique constraints only allow one row with nulls
        db.Insert(new DiffProduct { Code = "A", Barcode = "1", Sku = "S1", Qty = 1, Rating = 1 });
        db.Insert(new DiffProduct { Code = "A", Barcode = "2", Sku = "S2", Qty = 1, Rating = 1 });
        Assert.Throws(Is.InstanceOf<Exception>(), () => db.Insert(new DiffProduct { Code = "B", Barcode = "2", Sku = "S3", Qty = 1, Rating = 1 }));
    }

    [Test]
    public void Compare_the_conditions_of_check_constraints()
    {
        using var db = OpenDbConnection();
        db.DropTable<DiffProduct>();
        db.CreateTable<Before.DiffProduct>();

        var diff = db.GetSchemaDiff<DiffProduct>();
        var checks = diff.Changes.Where(IsCheck).ToList();
        Assert.That(checks.Count, Is.EqualTo(3), diff.ToString());

        // Conditions are compared as the database writes them
        var qty = checks.Single(x => x.Field?.Name == "Qty");
        Assert.That(qty.Type, Is.EqualTo(SchemaChangeType.AlterConstraint));
        Assert.That(qty.DatabaseColumn, Does.Contain(">="));
        Assert.That(qty.ModelColumn, Is.EqualTo("CHECK (Qty > 0)"));

        var rating = checks.Single(x => x.Field?.Name == "Rating");
        Assert.That(rating.Type, Is.EqualTo(SchemaChangeType.AddConstraint));

        var price = checks.Single(x => x.Type == SchemaChangeType.DropConstraint);
        Assert.That(price.Name, Is.EqualTo("CHK__DiffProduct_Price").IgnoreCase);

        // Rows can break their conditions
        Assert.That(checks.All(x => x.IsDestructive));

        if (!CanAlterColumns(db))
        {
            Assert.That(checks.All(x => x.Sql == null));
            return;
        }

        var source = diff.ToMigration("Migration1010");
        Assert.That(source, Does.Contain($"// Constraint {qty.Name} is "));

        db.ApplySchemaDiff(diff, allowDestructive: true);
        diff = db.GetSchemaDiff<DiffProduct>();
        Assert.That(diff.Changes.Where(IsCheck), Is.Empty, diff.ToString());

        Assert.Throws(Is.InstanceOf<Exception>(), () => db.Insert(new DiffProduct { Code = "A", Barcode = "1", Qty = 1, Rating = 9 }));
        Assert.Throws(Is.InstanceOf<Exception>(), () => db.Insert(new DiffProduct { Code = "A", Barcode = "1", Qty = 0, Rating = 1 }));
        db.Insert(new DiffProduct { Code = "A", Barcode = "1", Qty = 1, Price = -1, Rating = 1 });
    }

    [Test]
    public void A_primary_key_of_other_columns_is_reported_but_not_changed()
    {
        using var db = OpenDbConnection();
        db.DropTable<DiffKey>();
        db.CreateTable<Before.DiffKey>();

        var diff = db.GetSchemaDiff<DiffKey>();
        var key = diff.Changes.Single(x => x.Type == SchemaChangeType.AlterPrimaryKey);
        Assert.That(key.DatabaseColumn, Is.EqualTo("PRIMARY KEY (code)").IgnoreCase);
        Assert.That(key.ModelColumn, Is.EqualTo("PRIMARY KEY (id)").IgnoreCase);
        Assert.That(key.Sql, Is.Null);
        Assert.That(diff.ToString(), Does.Contain("~ primary key  "));
        Assert.That(diff.ToMigration("Migration1011"), Does.Contain("// The primary key of "));

        // nor are the columns of the primary key in the database, which can't allow nulls
        var code = diff.Changes.FirstOrDefault(x => x.Type == SchemaChangeType.AlterColumn && x.Name.EqualsIgnoreCase("code"));
        Assert.That(code?.Sql, Is.Null);
        Assert.That(db.ApplySchemaDiff(diff, allowDestructive: true), Does.Not.Contain(key));
    }

    [Test]
    [IgnoreDialect(Dialect.AnyMySql, "MySQL and MariaDB don't have filtered indexes")]
    public void Compare_the_include_columns_and_conditions_of_indexes()
    {
        using var db = OpenDbConnection();
        db.DropTable<DiffEvent>();
        db.CreateTable<Before.DiffEvent>();

        var diff = db.GetSchemaDiff<DiffEvent>();
        var indexes = diff.Changes.Where(x => x.Type == SchemaChangeType.AlterIndex).ToDictionary(x => x.Name.ToLower());
        Assert.That(indexes.Keys, Is.EquivalentTo(new[] { "ix_diff_event_kind", "ix_diff_event_user" }), diff.ToString());
        Assert.That(indexes["ix_diff_event_kind"].DatabaseColumn, Does.Contain(" WHERE "));
        Assert.That(indexes["ix_diff_event_kind"].ModelColumn, Does.EndWith("> 1"));

        // The indexes are the same once they're applied, as the database writes their conditions
        db.ApplySchemaDiff(diff);
        diff = db.GetSchemaDiff<DiffEvent>();
        Assert.That(diff.Changes, Is.Empty, diff.ToString());
    }

    public class DiffCustomer
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class DiffWarehouse
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class DiffOrder
    {
        [AutoIncrement]
        public int Id { get; set; }

        [ForeignKey(typeof(DiffCustomer), OnDelete = "CASCADE")] // deletes its orders
        public int CustomerId { get; set; }

        public int? WarehouseId { get; set; } // no longer a foreign key

        [ForeignKey(typeof(DiffCustomer))] // a foreign key of a column with values
        public int? BillingCustomerId { get; set; }

        [ForeignKey(typeof(DiffWarehouse))] // a new column
        public int? ShipFromId { get; set; }

        [Default(5)] // changed
        public int Quantity { get; set; }

        [StringLength(20)] // no longer has a default
        public string Status { get; set; }

        [Default(3)] // has a default
        public int Priority { get; set; }

        [Default(OrmLiteVariables.SystemUtc)]
        public DateTime Created { get; set; }
    }

    static void CreateOrderTables<T>(System.Data.IDbConnection db)
    {
        db.DropTable<DiffOrder>();
        db.DropAndCreateTable<DiffCustomer>();
        db.DropAndCreateTable<DiffWarehouse>();
        db.CreateTable<T>();
    }

    [Test]
    public void Compare_the_default_values_of_columns()
    {
        using var db = OpenDbConnection();
        CreateOrderTables<Before.DiffOrder>(db);

        var diff = db.GetSchemaDiff<DiffOrder>();
        var defaults = diff.Changes.Where(x => x.Type == SchemaChangeType.AlterDefault).ToDictionary(x => x.Field.Name);
        Assert.That(defaults.Keys, Is.EquivalentTo(new[] { "Quantity", "Status", "Priority" }), diff.ToString());

        // As they're written by the database, e.g. ((1)) on SQL Server
        Assert.That(defaults["Quantity"].DatabaseColumn, Does.Match(@"^DEFAULT \(*1\)*$"));
        Assert.That(defaults["Quantity"].ModelColumn, Does.Match(@"^DEFAULT \(*5\)*$"));
        Assert.That(defaults["Status"].DatabaseColumn, Does.Contain("draft"));
        Assert.That(defaults["Status"].ModelColumn, Is.EqualTo("no default"));
        Assert.That(defaults["Priority"].DatabaseColumn, Is.EqualTo("no default"));
        Assert.That(diff.ToString(), Does.Match(@"~ quantity  default DEFAULT \(*1\)*").IgnoreCase);

        // Defaults only change the rows that are inserted without a value
        Assert.That(defaults.Values.All(x => !x.IsDestructive));

        if (!CanAlterColumns(db))
        {
            Assert.That(defaults.Values.All(x => x.Sql == null));
            Assert.That(diff.ToString(), Does.Contain("(can't be changed in this database)"));
            return;
        }

        var source = diff.ToMigration("Migration1008");
        Assert.That(source, Does.Contain("// The default of "));

        db.ApplySchemaDiff(diff);
        diff = db.GetSchemaDiff<DiffOrder>();
        Assert.That(diff.Changes.Where(x => x.Type == SchemaChangeType.AlterDefault), Is.Empty, diff.ToString());

        db.Insert(new DiffCustomer { Name = "Customer" });
        // Columns that aren't inserted have their defaults
        db.InsertOnly(() => new DiffOrder { CustomerId = 1 });
        var order = db.Single<DiffOrder>(x => x.CustomerId == 1);
        Assert.That(order.Quantity, Is.EqualTo(5));
        Assert.That(order.Priority, Is.EqualTo(3));
        Assert.That(order.Status, Is.Null);
    }

    [Test]
    public void Compare_foreign_keys_and_find_the_foreign_keys_that_are_not_in_the_model()
    {
        using var db = OpenDbConnection();
        CreateOrderTables<Before.DiffOrder>(db);

        var diff = db.GetSchemaDiff<DiffOrder>();
        var foreignKeys = diff.Changes.Where(x => x.Type is SchemaChangeType.AddForeignKey
            or SchemaChangeType.AlterForeignKey or SchemaChangeType.DropForeignKey).ToList();
        Assert.That(foreignKeys.Count, Is.EqualTo(4), diff.ToString());

        // Foreign keys with other actions are dropped and added again
        var customer = foreignKeys.Single(x => x.Field?.Name == "CustomerId");
        Assert.That(customer.Type, Is.EqualTo(SchemaChangeType.AlterForeignKey));
        Assert.That(customer.ModelColumn, Does.EndWith("ON DELETE CASCADE"));
        Assert.That(customer.DatabaseColumn, Does.Not.Contain("CASCADE"));
        Assert.That(customer.IsDestructive, Is.False);

        // The rows of a column can reference rows that don't exist
        var billing = foreignKeys.Single(x => x.Field?.Name == "BillingCustomerId");
        Assert.That(billing.Type, Is.EqualTo(SchemaChangeType.AddForeignKey));
        Assert.That(billing.ModelColumn, Does.Match(@"^\(billing_?customer_?id\) REFERENCES diff_?customer \(id\)$").IgnoreCase);
        Assert.That(billing.IsDestructive, Is.True);

        // unlike a column that's added
        var shipFrom = foreignKeys.Single(x => x.Field?.Name == "ShipFromId");
        Assert.That(shipFrom.Type, Is.EqualTo(SchemaChangeType.AddForeignKey));
        Assert.That(shipFrom.IsDestructive, Is.False);

        // A foreign key that's not in the model is only dropped with allowDestructive
        var warehouse = foreignKeys.Single(x => x.Type == SchemaChangeType.DropForeignKey);
        Assert.That(warehouse.DatabaseColumn, Does.Match(@"^\(warehouse_?id\) REFERENCES diff_?warehouse").IgnoreCase);
        Assert.That(warehouse.IsDestructive, Is.True);

        Assert.That(diff.ToString(), Does.Contain("+ foreign key "));
        Assert.That(diff.ToString(), Does.Contain("~ foreign key "));
        Assert.That(diff.ToString(), Does.Contain("- foreign key "));

        if (!CanAlterColumns(db))
        {
            // SQLite can only change the foreign keys of a table by creating it again
            Assert.That(foreignKeys.All(x => x.Sql == null));
            return;
        }

        var source = diff.ToMigration("Migration1009");
        Assert.That(source, Does.Contain($"Db.DropForeignKey<DiffOrder>(\"{customer.Name}\");"));
        Assert.That(source, Does.Contain($"// Db.DropForeignKey<DiffOrder>(\"{warehouse.Name}\");"));
        Assert.That(source, Does.Contain($"Db.ExecuteSql(\"{shipFrom.Sql.Replace("\"", "\\\"")}\");"));

        var applied = db.ApplySchemaDiff(diff);
        Assert.That(applied, Does.Contain(customer).And.Contain(shipFrom));
        Assert.That(applied, Does.Not.Contain(billing).And.Not.Contain(warehouse));

        db.ApplySchemaDiff(db.GetSchemaDiff<DiffOrder>(), allowDestructive: true);
        diff = db.GetSchemaDiff<DiffOrder>();
        Assert.That(diff.Changes.Where(x => x.Type is SchemaChangeType.AddForeignKey
            or SchemaChangeType.AlterForeignKey or SchemaChangeType.DropForeignKey), Is.Empty, diff.ToString());

        // Deleting a customer deletes their orders
        var customerId = (int)db.Insert(new DiffCustomer { Name = "Customer" }, selectIdentity: true);
        db.Insert(new DiffOrder { CustomerId = customerId, Created = DateTime.UtcNow });
        db.DeleteById<DiffCustomer>(customerId);
        Assert.That(db.Count<DiffOrder>(), Is.EqualTo(0));
    }

    [CompositeIndex(nameof(Status), nameof(Reference), Name = "ix_shipment_by_status")] // columns in another order
    public class Shipment
    {
        [AutoIncrement]
        public int Id { get; set; }

        [Index(Name = "ix_shipment_reference", Unique = true)] // made unique
        public string Reference { get; set; }

        public string Carrier { get; set; } // no longer indexed

        [Unique]
        public string TrackingCode { get; set; }

        public string Status { get; set; }
        public int WeightInGrams { get; set; } // renamed
    }

    [Test]
    public void Compare_the_columns_of_indexes_and_find_the_indexes_that_are_not_in_the_model()
    {
        using var db = OpenDbConnection();
        db.DropTable<Shipment>();
        db.CreateTable<Before.Shipment>();

        var diff = db.GetSchemaDiff<Shipment>();
        var indexes = diff.Changes.Where(x => x.Type is SchemaChangeType.AlterIndex or SchemaChangeType.DropIndex)
            .ToDictionary(x => x.Name.ToLower());

        // Indexes whose columns or uniqueness changed are dropped and created again
        var byStatus = indexes["ix_shipment_by_status"];
        Assert.That(byStatus.Type, Is.EqualTo(SchemaChangeType.AlterIndex));
        Assert.That(byStatus.DatabaseColumn.ToLower(), Is.EqualTo("(reference, status)"));
        Assert.That(byStatus.ModelColumn.ToLower(), Is.EqualTo("(status, reference)"));
        Assert.That(byStatus.IsDestructive, Is.False);

        // An index that's made unique fails if rows have the same values
        var reference = indexes["ix_shipment_reference"];
        Assert.That(reference.Type, Is.EqualTo(SchemaChangeType.AlterIndex));
        Assert.That(reference.ModelColumn.ToLower(), Is.EqualTo("unique (reference)"));
        Assert.That(reference.IsDestructive, Is.True);

        // An index that's not in the model is only dropped with allowDestructive, as it may have been added by hand
        var carrier = indexes["ix_shipment_carrier"];
        Assert.That(carrier.Type, Is.EqualTo(SchemaChangeType.DropIndex));
        Assert.That(carrier.IsDestructive, Is.True);

        // Indexes of constraints aren't compared, e.g. of the primary key and [Unique] TrackingCode
        Assert.That(indexes.Count, Is.EqualTo(3), diff.ToString());
        Assert.That(diff.ToString(), Does.Contain("~ index ix_shipment_by_status"));
        Assert.That(diff.ToString(), Does.Contain("- index ix_shipment_carrier"));

        var source = diff.ToMigration("Migration1006");
        Assert.That(source, Does.Contain("Db.DropIndex<Shipment>(\"ix_shipment_by_status\");"));
        Assert.That(source, Does.Contain("// Db.DropIndex<Shipment>(\"ix_shipment_carrier\");"));

        // The indexes are the same once they're applied
        db.ApplySchemaDiff(diff, allowDestructive: true);
        diff = db.GetSchemaDiff<Shipment>();
        Assert.That(diff.Changes.Where(x => x.Type is SchemaChangeType.CreateIndex or SchemaChangeType.AlterIndex
            or SchemaChangeType.DropIndex), Is.Empty, diff.ToString());
    }

    [Test]
    public void A_column_that_is_not_in_the_model_and_a_new_column_of_the_same_type_are_likely_renamed()
    {
        using var db = OpenDbConnection();
        db.DropTable<Shipment>();
        db.CreateTable<Before.Shipment>();

        var diff = db.GetSchemaDiff<Shipment>();
        var added = diff.Changes.Single(x => x.Type == SchemaChangeType.AddColumn);
        var dropped = diff.Changes.Single(x => x.Type == SchemaChangeType.DropColumn);
        Assert.That(added.LikelyRename, Is.EqualTo(dropped.Name));
        Assert.That(dropped.LikelyRename, Is.EqualTo(added.Name));
        Assert.That(diff.ToString(), Does.Contain($"(renamed from {dropped.Name}?)"));

        // They're not renamed when they're applied, as it's only likely
        var source = diff.ToMigration("Migration1007");
        Assert.That(source, Does.Contain($"// Db.RenameColumn<Shipment>(x => x.WeightInGrams, \"{dropped.Name}\");"));
        Assert.That(source, Does.Contain("Db.AddColumn<Shipment>(x => x.WeightInGrams);"));
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
