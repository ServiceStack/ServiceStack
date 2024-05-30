using System;
using System.Globalization;
using System.Text;
using NUnit.Framework;
using ServiceStack.OrmLite.SqlServerTests.UseCase;
using System.Data;

namespace ServiceStack.OrmLite.SqlServerTests;

[TestFixture]
public class SqlServerExpressionVisitorQueryTest : OrmLiteTestBase
{
    [OneTimeSetUp]
    public override void TestFixtureSetUp()
    {
        base.TestFixtureSetUp();
            
        OrmLiteConfig.SanitizeFieldNameForParamNameFn = s =>
            (s ?? "").Replace(" ", "").Replace("°", "");
    }

    [Test]
    public void Skip_Take_works_with_injected_Visitor()
    {
        using var db = OpenDbConnection();
        FillTestEntityTableWithTestData(db);

        var result = db.Select(db.From<TestEntity>().Limit(10, 100));

        Assert.NotNull(result);
        Assert.AreEqual(100, result.Count);
        Assert.Less(10, result[0].Id);
        Assert.Greater(111, result[99].Id);
    }

    [Test]
    public void test_if_limit_works_with_rows_and_skip()
    {
        using var db = OpenDbConnection();
        FillTestEntityTableWithTestData(db);

        var ev = OrmLiteConfig.DialectProvider.SqlExpression<TestEntity>();
        ev.Limit(10, 100);

        var result = db.Select(ev);
        Assert.NotNull(result);
        Assert.AreEqual(100, result.Count);
        Assert.Less(10, result[0].Id);
        Assert.Greater(111, result[99].Id);
    }

    [Test]
    public void test_if_limit_works_with_rows()
    {
        using var db = OpenDbConnection();
        FillTestEntityTableWithTestData(db);

        var ev = OrmLiteConfig.DialectProvider.SqlExpression<TestEntity>();
        ev.Limit(100);

        var result = db.Select(ev);
        Assert.NotNull(result);
        Assert.AreEqual(100, result.Count);
        Assert.Less(0, result[0].Id);
        Assert.Greater(101, result[99].Id);
    }

    [Test]
    public void test_if_limit_works_with_rows_and_skip_and_orderby()
    {
        using var db = OpenDbConnection();
        FillTestEntityTableWithTestData(db);

        var ev = OrmLiteConfig.DialectProvider.SqlExpression<TestEntity>();
        ev.Limit(10, 100);
        ev.OrderBy(e => e.Baz);

        var result = db.Select(ev);
        Assert.NotNull(result);
        Assert.AreEqual(100, result.Count);
        Assert.LessOrEqual(result[10].Baz, result[11].Baz);
    }

    [Test]
    public void test_if_ev_still_works_without_limit_and_orderby()
    {
        using var db = OpenDbConnection();
        FillTestEntityTableWithTestData(db);

        var ev = OrmLiteConfig.DialectProvider.SqlExpression<TestEntity>();
        ev.OrderBy(e => e.Baz);
        ev.Where(e => e.Baz < 0.1m);

        var result = db.Select(ev);
        Assert.NotNull(result);
        Assert.IsTrue(result.Count > 0);
    }

    [Test]
    public void test_if_and_works_with_nullable_parameter()
    {
        using var db = OpenDbConnection();
        db.CreateTable<TestEntity>(true);
        var id = db.Insert(new TestEntity
        {
            Foo = this.RandomString(16),
            Bar = this.RandomString(16),
            Baz = this.RandomDecimal()
        }, selectIdentity: true);

        var ev = OrmLiteConfig.DialectProvider.SqlExpression<TestEntity>();
        ev.Where(e => e.Id == id);
        int? i = null;
        ev.And(e => e.NullInt == i);

        var result = db.Select(ev);
        Assert.NotNull(result);
        Assert.IsTrue(result.Count > 0);
    }

    [Test]
    public void test_if_limit_works_with_rows_and_skip_if_pk_columnname_has_space()
    {
        using var db = OpenDbConnection();
        FillAliasedTestEntityTableWithTestData(db);

        var ev = OrmLiteConfig.DialectProvider.SqlExpression<TestEntityWithAliases>();
        ev.Limit(10, 100);

        var result = db.Select(ev);
        Assert.NotNull(result);
        Assert.AreEqual(100, result.Count);
    }

    [Test]
    public void test_if_limit_works_with_rows_and_skip_and_orderby_if_pk_columnname_has_space()
    {
        using var db = OpenDbConnection();
        FillAliasedTestEntityTableWithTestData(db);

        var ev = OrmLiteConfig.DialectProvider.SqlExpression<TestEntityWithAliases>();
        ev.Limit(10, 100);
        ev.OrderBy(e => e.Baz);

        var result = db.Select(ev);
        Assert.NotNull(result);
        Assert.AreEqual(100, result.Count);
        Assert.LessOrEqual(result[10].Baz, result[11].Baz);
    }

    [Test]
    public void Can_query_table_with_special_alias()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<TestEntityWithAliases>();

        db.Insert(new TestEntityWithAliases { Id = 1, Foo = "Foo", Bar = "Bar", Baz = 2 });

        var row = db.SingleById<TestEntityWithAliases>(1);
        Assert.That(row.Foo, Is.EqualTo("Foo"));

        row = db.Single<TestEntityWithAliases>(q => q.Bar == "Bar");
        Assert.That(row.Foo, Is.EqualTo("Foo"));
    }

    [Test]
    public void Can_OrderbyDesc_using_ComplexFunc()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<TestEntityWithAliases>();

        db.Insert(new TestEntityWithAliases {Id = 1, Foo = "Foo", Bar = "Bar", Baz = 2});

        System.Linq.Expressions.Expression<Func<TestEntityWithAliases, object>> orderBy = x =>
            String.Concat(String.Concat("Text1: ", x.Foo),
                String.Concat("Text2: ", x.Bar));
        var q = db.From<TestEntityWithAliases>().OrderByDescending(orderBy);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Not.Contain("desc,"));

        var target = db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));
    }

    [Test]
    public void Can_OrderBy_using_isnull()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<TestEntityWithAliases>();

        db.Insert(new TestEntityWithAliases {Id = 1, Foo = "Foo", Bar = "Bar", Baz = 2});
        System.Linq.Expressions.Expression<Func<TestEntityWithAliases, object>> orderBy = x => x.Foo == null ? x.Foo : x.Bar;
        var q = Db.From<TestEntityWithAliases>().OrderBy(orderBy);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Not.Contain("isnull"));

        var target = db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));
    }

    protected void FillTestEntityTableWithTestData(IDbConnection db)
    {
        db.CreateTable<TestEntity>(true);

        for (int i = 1; i < 1000; i++)
        {
            db.Insert(new TestEntity()
            {
                Foo = RandomString(16),
                Bar = RandomString(16),
                Baz = RandomDecimal(i)
            });
        }
    }

    protected void FillAliasedTestEntityTableWithTestData(IDbConnection db)
    {
        db.CreateTable<TestEntityWithAliases>(true);

        for (int i = 1; i < 1000; i++)
        {
            db.Insert(new TestEntityWithAliases()
            {
                Foo = RandomString(16),
                Bar = RandomString(16),
                Baz = RandomDecimal(i)
            });
        }
    }

    protected string RandomString(int length)
    {
        var rnd = new System.Random();
        var buffer = new StringBuilder();

        for (var i = 0; i < length; i++)
        {
            buffer.Append(Convert.ToChar(((byte)rnd.Next(254)))
                .ToString(CultureInfo.InvariantCulture));
        }

        return buffer.ToString();
    }

    protected decimal RandomDecimal(int seed = 0)
    {
        var rnd = new Random(seed);
        return new decimal(rnd.NextDouble());
    }
}