using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.SqlServer.Converters;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.SqlServerTests;

[TestFixture]
public class SchemaTests : OrmLiteTestBase
{
    public class TableTest
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [Test]
    public void Can_drop_and_add_column()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<TableTest>();

        Assert.That(db.ColumnExists<TableTest>(x => x.Id));
        Assert.That(db.ColumnExists<TableTest>(x => x.Name));

        db.DropColumn<TableTest>(x => x.Name);
        Assert.That(!db.ColumnExists<TableTest>(x => x.Name));

        try
        {
            db.DropColumn<TableTest>(x => x.Name);
            Assert.Fail("Should throw");
        }
        catch (Exception) { }

        db.AddColumn<TableTest>(x => x.Name);
        Assert.That(db.ColumnExists<TableTest>(x => x.Name));

        try
        {
            db.AddColumn<TableTest>(x => x.Name);
            Assert.Fail("Should throw");
        }
        catch (Exception) {}
    }

    [Schema("Schema")]
    public class SchemaTest
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [Test]
    public void Can_drop_and_add_column_with_Schema()
    {
        using var db = OpenDbConnection();
        db.ExecuteSql(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Schema')
            BEGIN
                EXEC( 'CREATE SCHEMA [Schema]' );
            END
            """);
        db.DropAndCreateTable<SchemaTest>();

        Assert.That(db.ColumnExists<SchemaTest>(x => x.Id));
        Assert.That(db.ColumnExists<SchemaTest>(x => x.Name));

        db.DropColumn<SchemaTest>(x => x.Name);
        Assert.That(!db.ColumnExists<SchemaTest>(x => x.Name));

        try
        {
            db.DropColumn<SchemaTest>(x => x.Name);
            Assert.Fail("Should throw");
        }
        catch (Exception) { }

        db.AddColumn<SchemaTest>(x => x.Name);
        Assert.That(db.ColumnExists<SchemaTest>(x => x.Name));

        try
        {
            db.AddColumn<SchemaTest>(x => x.Name);
            Assert.Fail("Should throw");
        }
        catch (Exception) { }
    }

    public class TestDecimalConverter
    {
        public int Id { get; set; }
        public decimal Decimal { get; set; }
    }

    [Test]
    public void Can_replace_decimal_column()
    {
        SqlServerDialect.Provider.RegisterConverter<decimal>(new SqlServerFloatConverter());

        //Requires OrmLiteConnection Wrapper to capture last SQL executed
        var dbFactory = new OrmLiteConnectionFactory(ConnectionString, SqlServerDialect.Provider);

        using var db = dbFactory.OpenDbConnection();
        db.DropAndCreateTable<TestDecimalConverter>();

        Assert.That(db.GetLastSql(), Does.Contain("FLOAT"));
    }

    [Test]
    public void Get_actual_column_definition()
    {
        var sql = 
            """
            select COLUMN_NAME, data_type + 
                case
                    when data_type like '%text' or data_type like 'image' or data_type like 'sql_variant' or data_type like 'xml'
                        then ''
                    when data_type = 'float'
                        then '(' + convert(varchar(10), isnull(numeric_precision, 18)) + ')'
                    when data_type = 'numeric' or data_type = 'decimal'
                        then '(' + convert(varchar(10), isnull(numeric_precision, 18)) + ',' + convert(varchar(10), isnull(numeric_scale, 0)) + ')'
                    when (data_type like '%char' or data_type like '%binary') and character_maximum_length = -1
                        then '(max)'
                    when character_maximum_length is not null
                        then '(' + convert(varchar(10), character_maximum_length) + ')'
                    else ''
                end as COLUMN_DEFINITION
            FROM INFORMATION_SCHEMA.COLUMNS
            """;

        using var db = OpenDbConnection();
        db.DropAndCreateTable<TableTest>();

        var results = db.Dictionary<string,string>(sql + " WHERE table_name = 'TableTest'");
        results.PrintDump();
        Assert.That(results["Name"], Is.EqualTo("varchar(8000)"));
    }
}