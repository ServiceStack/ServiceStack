using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using DescriptionAttribute = ServiceStack.DataAnnotations.DescriptionAttribute;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// Attributes that CreateTable uses to create filtered and covering indexes, generated columns, enum check constraints
/// and comments, which otherwise need a hand-written [PostCreateTable] or a migration.
/// {Property} names in their SQL are replaced with the quoted column name.
/// </summary>
[TestFixtureOrmLite]
public class DdlAttributeUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    const string NoFilteredIndexes = "MySQL and MariaDB don't have filtered indexes";

    public class Subscriber
    {
        [AutoIncrement]
        public int Id { get; set; }

        // An email can be reused once its subscriber is deleted
        [Index(Unique = true, Where = "{DeletedDate} IS NULL")]
        public string Email { get; set; }

        public DateTime? DeletedDate { get; set; }
    }

    [Test]
    [IgnoreDialect(Dialect.AnyMySql, NoFilteredIndexes)]
    public void Unique_among_the_rows_that_arent_deleted_with_a_filtered_index()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Subscriber>();

        db.Insert(new Subscriber { Email = "a@example.org", DeletedDate = DateTime.UtcNow });
        db.Insert(new Subscriber { Email = "a@example.org", DeletedDate = DateTime.UtcNow });
        db.Insert(new Subscriber { Email = "a@example.org" });

        Assert.That(() => db.Insert(new Subscriber { Email = "a@example.org" }), Throws.Exception);
        Assert.That(db.Count<Subscriber>(), Is.EqualTo(3));
    }

    [Test]
    public void Filtered_indexes_arent_supported_by_MySql()
    {
        if (!Dialect.AnyMySql.HasFlag(Dialect))
            return;

        using var db = OpenDbConnection();
        db.DropTable<Subscriber>();
        Assert.That(() => db.CreateTable<Subscriber>(), Throws.TypeOf<NotSupportedException>());
        // before the table is created without its index
        Assert.That(db.TableExists<Subscriber>(), Is.False);
    }

    [CompositeIndex(nameof(TenantId), nameof(Status), Include = [nameof(Name), nameof(Size)])]
    [CompositeIndex(true, nameof(TenantId), nameof(Name), Include = [nameof(Size)], Name = "uidx_upload_tenant_name")]
    [CompositeIndex(nameof(TenantId), "CreatedDate DESC", Name = "idx_upload_newest")]
    public class Upload
    {
        [AutoIncrement]
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string Status { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    [Test]
    public void Answer_queries_from_a_covering_index()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Upload>();

        var indexes = DialectProvider.ToCreateIndexStatements(typeof(Upload));
        var covering = indexes.First(x => x.Contains("idx_upload_tenantid_status"));
        var unique = indexes.First(x => x.Contains("uidx_upload_tenant_name"));
        var newest = indexes.First(x => x.Contains("idx_upload_newest"));

        if ((Dialect.AnyPostgreSql | Dialect.AnySqlServer).HasFlag(Dialect))
        {
            // PostgreSQL and SQL Server keep the columns with the index, without making them part of its key
            Assert.That(covering, Does.Contain(" INCLUDE (").And.Contain("Size").IgnoreCase);
            Assert.That(unique, Does.Contain(" INCLUDE ("));
        }
        else
        {
            // Other RDBMS add them to the key of non-unique indexes, which also covers the query
            Assert.That(covering, Does.Not.Contain("INCLUDE").And.Contain("Size").IgnoreCase);
            // A unique index is left as it is, as more key columns would change which rows it allows
            Assert.That(unique, Does.Not.Contain("INCLUDE").And.Not.Contain("Size").IgnoreCase);
        }

        // Index columns can be descending, to match a newest first sort
        Assert.That(newest, Does.Contain(" DESC"));

        db.Insert(new Upload { TenantId = 1, Status = "Ready", Name = "a.txt", Size = 10, CreatedDate = DateTime.UtcNow });
        Assert.That(() => db.Insert(new Upload { TenantId = 1, Status = "Ready", Name = "a.txt", Size = 20, CreatedDate = DateTime.UtcNow }),
            Throws.Exception);

        var sizes = db.Column<long>(db.From<Upload>().Where(x => x.TenantId == 1 && x.Status == "Ready").Select(x => x.Size));
        Assert.That(sizes, Is.EqualTo(new[] { 10L }));
    }

    public class LineItem
    {
        [AutoIncrement]
        public int Id { get; set; }
        public int Quantity { get; set; }
        public int UnitPrice { get; set; }

        // Stored with the row and kept up to date by the RDBMS
        [Compute("{Quantity} * {UnitPrice}"), Persisted]
        public int Total { get; set; }

        // Calculated by the RDBMS when it's read, except in PostgreSQL where it's stored
        [Compute("{Quantity} * 2")]
        public int DoubleQuantity { get; set; }
    }

    [Test]
    public void Let_the_database_calculate_a_generated_column()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<LineItem>();

        // Generated columns are never inserted or updated, the value they're given is ignored
        var id = (int)db.Insert(new LineItem { Quantity = 3, UnitPrice = 20, Total = -1 }, selectIdentity: true);

        var row = db.SingleById<LineItem>(id);
        Assert.That(row.Total, Is.EqualTo(60));
        Assert.That(row.DoubleQuantity, Is.EqualTo(6));

        row.Quantity = 5;
        db.Update(row);
        row = db.SingleById<LineItem>(id);
        Assert.That(row.Total, Is.EqualTo(100));
        Assert.That(row.DoubleQuantity, Is.EqualTo(10));

        db.UpdateOnly(() => new LineItem { UnitPrice = 30 }, where: x => x.Id == id);
        Assert.That(db.SingleById<LineItem>(id).Total, Is.EqualTo(150));

        // They can be queried like any other column
        Assert.That(db.Count<LineItem>(x => x.Total > 100), Is.EqualTo(1));
    }

    public enum ShipmentStatus { Packed, Shipped, Delivered }

    [EnumAsInt]
    public enum ShipmentPriority { Standard = 1, Express = 2 }

    public class Shipment
    {
        [AutoIncrement]
        public int Id { get; set; }

        [CheckEnum]
        public ShipmentStatus Status { get; set; }

        [CheckEnum]
        public ShipmentPriority Priority { get; set; }

        [CheckEnum]
        public ShipmentStatus? PreviousStatus { get; set; }
    }

    [Test]
    public void Only_allow_the_values_of_an_enum()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Shipment>();

        db.Insert(new Shipment { Status = ShipmentStatus.Packed, Priority = ShipmentPriority.Express });
        db.Insert(new Shipment { Status = ShipmentStatus.Shipped, Priority = ShipmentPriority.Standard, PreviousStatus = ShipmentStatus.Packed });

        // The database rejects values written by other apps or raw SQL that aren't in the Enum
        var table = db.TableRef<Shipment>();
        var (status, priority) = db.ColumnRefs<Shipment>(x => new { x.Status, x.Priority });
        Assert.That(() => db.ExecuteSql(Sql.Fmt($"UPDATE {table} SET {status} = {"Lost"}")), Throws.Exception);
        Assert.That(() => db.ExecuteSql(Sql.Fmt($"UPDATE {table} SET {priority} = {3}")), Throws.Exception);

        Assert.That(db.Select<Shipment>().Map(x => x.Status),
            Is.EquivalentTo(new[] { ShipmentStatus.Packed, ShipmentStatus.Shipped }));
    }

    [Description("Books that customers can order")]
    public class CatalogBook
    {
        [AutoIncrement]
        public int Id { get; set; }

        [Description("The book's title, as it's printed: Les Misérables")]
        public string Title { get; set; }

        public string Isbn { get; set; }
    }

    [Test]
    public void Describe_tables_and_columns_with_comments()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<CatalogBook>();

        var table = DialectProvider.GetTableNameOnly(new TableRef(ModelDefinition<CatalogBook>.Definition));
        var column = DialectProvider.GetQuotedColumnName(ModelDefinition<CatalogBook>.Definition.GetFieldDefinition("Title")).StripDbQuotes();

        string tableComment = null, columnComment = null;
        if (Dialect.AnyPostgreSql.HasFlag(Dialect))
        {
            var quotedTable = db.GetQuotedTableName<CatalogBook>();
            tableComment = db.SqlScalar<string>(Sql.Fmt($"SELECT obj_description({quotedTable}::regclass, 'pg_class')"));
            columnComment = db.SqlScalar<string>(Sql.Fmt(
                $"SELECT col_description({quotedTable}::regclass, (SELECT ordinal_position FROM information_schema.columns WHERE table_name = {table} AND column_name = {column} LIMIT 1))"));
        }
        else if (Dialect.AnySqlServer.HasFlag(Dialect))
        {
            tableComment = db.SqlScalar<string>(Sql.Fmt(
                $"SELECT CAST(value AS nvarchar(max)) FROM sys.extended_properties WHERE major_id = OBJECT_ID({table}) AND minor_id = 0 AND name = 'MS_Description'"));
            columnComment = db.SqlScalar<string>(Sql.Fmt(
                $"SELECT CAST(p.value AS nvarchar(max)) FROM sys.extended_properties p JOIN sys.columns c ON c.object_id = p.major_id AND c.column_id = p.minor_id WHERE p.major_id = OBJECT_ID({table}) AND c.name = {column} AND p.name = 'MS_Description'"));
        }
        else if (Dialect.AnyMySql.HasFlag(Dialect))
        {
            tableComment = db.SqlScalar<string>(Sql.Fmt(
                $"SELECT TABLE_COMMENT FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = {table}"));
            columnComment = db.SqlScalar<string>(Sql.Fmt(
                $"SELECT COLUMN_COMMENT FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = {table} AND COLUMN_NAME = {column}"));
        }
        else
        {
            // SQLite has no comments, so the descriptions are ignored
            Assert.That(DialectProvider.ToCreateCommentStatements(typeof(CatalogBook)), Is.Empty);
            return;
        }

        Assert.That(tableComment, Is.EqualTo("Books that customers can order"));
        Assert.That(columnComment, Is.EqualTo("The book's title, as it's printed: Les Misérables"));
    }
}
