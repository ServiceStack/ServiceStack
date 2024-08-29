using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests.Expression;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

public class PocoTable
{
    public int Id { get; set; }

    [CustomField("CHAR(20)")]
    public string CharColumn { get; set; }

    [CustomField("DECIMAL(18,4)")]
    public decimal? DecimalColumn { get; set; }

    [CustomField(OrmLiteVariables.MaxText)]        //= {MAX_TEXT}
    public string MaxText { get; set; }

    [CustomField(OrmLiteVariables.MaxTextUnicode)] //= {NMAX_TEXT}
    public string MaxUnicodeText { get; set; }
}

[PreCreateTable("CREATE INDEX udxNoTable on NonExistingTable (Name);")]
public class ModelWithPreCreateSql
{
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; }
}

[PostCreateTable("INSERT INTO ModelWithSeedDataSql (Name) VALUES ('Foo');" +
                 "INSERT INTO ModelWithSeedDataSql (Name) VALUES ('Bar');")]
public class ModelWithSeedDataSql
{
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; }
}

[PostCreateTable("INSERT INTO ModelWithSeedDataSqlMulti (Name) VALUES ('Foo')"),
 PostCreateTable("INSERT INTO ModelWithSeedDataSqlMulti (Name) VALUES ('Bar')")]
public class ModelWithSeedDataSqlMulti
{
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; }
}

public class DynamicAttributeSeedData
{
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; }
}

[PreDropTable("CREATE INDEX udxNoTable on NonExistingTable (Name);")]
public class ModelWithPreDropSql
{
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; }
}

[PostDropTable("CREATE INDEX udxNoTable on NonExistingTable (Name);")]
public class ModelWithPostDropSql
{
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; }
}

[PreDropTable("-- PreDropTable")]
[PostDropTable("-- PostDropTable")]
[PreCreateTable("-- PreCreateTable")]
[PostCreateTable("-- PostCreateTable")]
public class ModelWithPreAndPostDrop
{
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; }
}

[TestFixtureOrmLite]
public class CustomSqlTests : OrmLiteProvidersTestBase
{
    public CustomSqlTests(DialectContext context) : base(context) {}

    [Test]
    public void Can_create_field_with_custom_sql()
    {
        OrmLiteConfig.BeforeExecFilter = cmd => cmd.GetDebugString().Print();

        using var db = OpenDbConnection();
        db.DropAndCreateTable<PocoTable>();

        var createTableSql = db.GetLastSql().NormalizeSql();

        createTableSql.Print();

        if (Dialect != Dialect.Firebird)
        {
            Assert.That(createTableSql, Does.Contain("charcolumn char(20) null"));
            Assert.That(createTableSql, Does.Contain("decimalcolumn decimal(18,4) null"));
        }
        else
        {
            Assert.That(createTableSql, Does.Contain("charcolumn char(20)"));
            Assert.That(createTableSql, Does.Contain("decimalcolumn decimal(18,4)"));
        }
    }

    [Test]
    public void Does_execute_CustomSql_before_table_created()
    {
        using var db = OpenDbConnection();
        try
        {
            db.CreateTable<ModelWithPreCreateSql>();
            Assert.Fail("Should throw");
        }
        catch (Exception)
        {
            Assert.That(!db.TableExists("ModelWithPreCreateSql".SqlColumn(DialectProvider)));
        }
    }

    [Test]
    [IgnoreDialect(Dialect.AnyOracle | Dialect.AnyPostgreSql, "multiple SQL statements need to be wrapped in an anonymous block")]
    public void Does_execute_CustomSql_after_table_created()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithSeedDataSql>();

        var seedDataNames = db.Select<ModelWithSeedDataSql>().ConvertAll(x => x.Name);

        Assert.That(seedDataNames, Is.EquivalentTo(new[] {"Foo", "Bar"}));
    }

    [Test]
    [IgnoreDialect(Dialect.AnyOracle | Dialect.AnyPostgreSql, "multiple SQL statements need to be wrapped in an anonymous block")]
    public void Does_execute_multi_CustomSql_after_table_created()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithSeedDataSqlMulti>();

        var seedDataNames = db.Select<ModelWithSeedDataSqlMulti>().ConvertAll(x => x.Name);

        Assert.That(seedDataNames, Is.EquivalentTo(new[] {"Foo", "Bar"}));
    }

    [Test]
    [IgnoreDialect(Dialect.AnyOracle | Dialect.AnyPostgreSql, "multiple SQL statements need to be wrapped in an anonymous block")]
    public void Does_execute_CustomSql_after_table_created_using_dynamic_attribute()
    {
        typeof(DynamicAttributeSeedData)
            .AddAttributes(new PostCreateTableAttribute(
                "INSERT INTO {0} (Name) VALUES ('Foo');".Fmt("DynamicAttributeSeedData".SqlTable(DialectProvider)) +
                "INSERT INTO {0} (Name) VALUES ('Bar');".Fmt("DynamicAttributeSeedData".SqlTable(DialectProvider))));

        using var db = OpenDbConnection();
        db.DropAndCreateTable<DynamicAttributeSeedData>();

        var seedDataNames = db.Select<DynamicAttributeSeedData>().ConvertAll(x => x.Name);

        Assert.That(seedDataNames, Is.EquivalentTo(new[] {"Foo", "Bar"}));
    }

    [Test]
    public void Does_execute_PostCreateTable_and_PreDropTable()
    {
        OrmLiteUtils.PrintSql();
        using var db = OpenDbConnection();
        db.CreateTable<ModelWithPreAndPostDrop>(true);
    }

    [Test]
    public void Does_execute_CustomSql_before_table_dropped()
    {
        using var db = OpenDbConnection();
        db.CreateTable<ModelWithPreDropSql>();
        try
        {
            db.DropTable<ModelWithPreDropSql>();
            Assert.Fail("Should throw");
        }
        catch (Exception)
        {
            Assert.That(db.TableExists("ModelWithPreDropSql".SqlTableRaw(DialectProvider)));
        }
    }

    [Test]
    public void Does_execute_CustomSql_after_table_dropped()
    {
        using var db = OpenDbConnection();
        db.CreateTable<ModelWithPostDropSql>();
        try
        {
            db.DropTable<ModelWithPostDropSql>();
            Assert.Fail("Should throw");
        }
        catch (Exception)
        {
            Assert.That(!db.TableExists("ModelWithPostDropSql"));
        }
    }

    public class CustomSelectTest
    {
        public int Id { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        [CustomSelect("Width * Height")]
        public int Area { get; set; }
    }

    [Test]
    public void Can_select_custom_field_expressions()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<CustomSelectTest>();

        db.Insert(new CustomSelectTest {Id = 1, Width = 10, Height = 5});

        var row = db.SingleById<CustomSelectTest>(1);

        Assert.That(row.Area, Is.EqualTo(10 * 5));
    }

    [Test]
    public void Can_Count_Distinct()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<LetterFrequency>();

        var rows = "A,B,B,C,C,C,D,D,E".Split(',').Map(x => new LetterFrequency {Letter = x});

        db.InsertAll(rows);

        var count = db.Count(db.From<LetterFrequency>().Select(x => x.Letter));
        Assert.That(count, Is.EqualTo(rows.Count));

        count = db.Scalar<long>(db.From<LetterFrequency>().Select(x => Sql.Count(x.Letter)));
        Assert.That(count, Is.EqualTo(rows.Count));

        var distinctCount = db.Scalar<long>(db.From<LetterFrequency>().Select(x => Sql.CountDistinct(x.Letter)));
        Assert.That(distinctCount, Is.EqualTo(rows.Map(x => x.Letter).Distinct().Count()));

        distinctCount = db.Scalar<long>(db.From<LetterFrequency>().Select("COUNT(DISTINCT Letter)"));
        Assert.That(distinctCount, Is.EqualTo(rows.Map(x => x.Letter).Distinct().Count()));
    }
        
    [Test]
    public void Does_select_aliases_on_constant_expressions()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<LetterFrequency>();
        db.Insert(new LetterFrequency { Letter = "A" });

        var q = db.From<LetterFrequency>()
            .Select(x => new
            {
                param = 1,
                descr = x.Letter,
                str = "hi",
                date = DateTime.UtcNow
            });

        var results = db.Select<Dictionary<string,object>>(q)[0];
                
        Assert.That(results["param"], Is.EqualTo(1));
        Assert.That(results["descr"], Is.EqualTo("A"));
        Assert.That(results["str"], Is.EqualTo("hi"));
        Assert.That(results["date"], Is.Not.Empty);
    }
}