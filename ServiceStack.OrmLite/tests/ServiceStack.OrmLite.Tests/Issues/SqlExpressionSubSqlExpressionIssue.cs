using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.SqlServer;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues;

public class AnyObjectClass
{
    [Alias("_id")]
    public Guid Id { get; set; }

    [Alias("_identity")]
    public Guid? Identity { get; set; }

    [Alias("_name")]
    [StringLength(250)]
    public string Name { get; set; }
}

[TestFixtureOrmLite]
public class SqlExpressionSubSqlExpressionIssue(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    private static void RecreateAnyObjectTables(IDbConnection db)
    {
        db.DropTable<AnyObjectClassItem>();
        db.DropTable<AnyObjectClass>();
        db.CreateTable<AnyObjectClass>();
        db.CreateTable<AnyObjectClassItem>();
    }

    [Test]
    public void Can_compare_null_constant_in_subquery()
    {
        using var db = OpenDbConnection();
        RecreateAnyObjectTables(db);

        var inQ = db.From<AnyObjectClass>()
            .Where(y => y.Identity != null)
            .Select(y => y.Identity.Value);

        var q = db.From<AnyObjectClass>().Where(x => Sql.In(x.Identity, inQ));

        var results = db.Select(q);

        results.PrintDump();
    }

    [Test]
    public void Can_compare_null_constant_in_subquery_nested_in_SqlExpression()
    {
        using var db = OpenDbConnection();
        RecreateAnyObjectTables(db);

        var q = db.From<AnyObjectClass>().Where(x => Sql.In(x.Identity,
            db.From<AnyObjectClass>()
                .Where(y => y.Identity != null)
                .Select(y => y.Identity.Value)));

        var results = db.Select(q);

        results.PrintDump();
    }

    public class Person2
    {
        [AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class Order2
    {
        [AutoIncrement]
        public int Id { get; set; }

        [References(typeof(Person2))]
        public int Person2Id { get; set; }

        public DateTime OrderDate { get; set; }

        public int OrderTypeId { get; set; }
    }

    [Test]
    public void Can_reference_variable_in_sub_expression()
    {
        int orderTypeId = 2;

        using var db = OpenDbConnection();
        var subExpr = db.From<Order2>()
            .Where(y => y.OrderTypeId == orderTypeId)
            .Select(y => y.Person2Id);

        subExpr.ToSelectStatement().Print();
        Assert.That(subExpr.ToSelectStatement().NormalizeSql(), Does.Contain("@0"));

        var expr = db.From<Person2>()
            .Where(x => Sql.In(x.Id, subExpr));

        expr.ToSelectStatement().Print();
        Assert.That(expr.ToSelectStatement().NormalizeSql(), Does.Contain("@0"));
    }

    public class AnyObjectClass
    {
        public Guid? Identity { get; set; }

        public string Name { get; set; }

        public IDbConnection db;

        [DataAnnotations.Ignore]
        public decimal CustomProperty
        {
            get
            {
                return db.Select<AnyObjectClassItem>
                (s => Sql.In(s.Identity,
                    db.From<AnyObjectClassItem>()
                        .Where(b => b.AnyObjectClassId == this.Identity)
                        .Select(b => b.Identity))
                ).Sum(r => r.PurchasePrice);
            }
        }
    }

    public class AnyObjectClassItem
    {
        public Guid? Identity { get; set; }

        public string Name { get; set; }

        public decimal PurchasePrice { get; set; }

        [ForeignKey(typeof(AnyObjectClass))]
        public Guid AnyObjectClassId { get; set; }

        public AnyObjectClass AnyObjectClass { get; set; }
    }

    [Test]
    public void Can_select_sub_expression_when_called_within_a_datamodel()
    {
        using var db = OpenDbConnection();
        RecreateAnyObjectTables(db);

        var model = new AnyObjectClass { db = db };
        var result = model.CustomProperty;

        result.PrintDump();
        db.GetLastSql().PrintDump();
        Assert.That(db.GetLastSql().NormalizeSql(), Does.Contain("is null"));
                
        model = new AnyObjectClass { db = db, Identity = Guid.Parse("104ECE6A-7117-4205-961C-126AD276565C") };
        result = model.CustomProperty;

        result.PrintDump();
        db.GetLastSql().PrintDump();
        Assert.That(db.GetLastSql().NormalizeSql(), Does.Contain("@"));
    }

    [Test]
    public void SubExpressions2()
    {
        int orderTypeId = 2;
        using var db = OpenDbConnection();
        var subExpr = db.From<Order3>()
            .Where(y => y.Order2TypeId == orderTypeId)
            .Select(y => y.Person2Id);

        Assert.That(subExpr.ToSelectStatement().NormalizeSql(), Does.Contain("@"));

        var expr = db.From<Person2>()
            .Where(x => Sql.In(x.Id, subExpr));

        Assert.That(subExpr.ToSelectStatement().NormalizeSql(), Does.Contain("@"));
    }

    [Test]
    public void Can_query_sub_expression_using_lambda()
    {
        using var db = OpenDbConnection();
        var q = db.From<Order3>()
            .Where(x => Sql.In(x.Person2Id,
                db.From<Person2>()
                    .Where(p => p.Id == x.Person2Id)
                    .Select(p => new {p.Id})
            ));

        q.ToSelectStatement().Print();
    }

    [Test]
    public void SubExpressions_TestMethod1()
    {
        using var db = OpenDbConnection();
        var w = new Waybill(db)
        {
            Identity = Guid.Empty,
            Name = "WaybillTest"
        };

        w.TestMethod1();
    }

    [Test]
    public void SubExpressions_TestMethod2()
    {
        using var db = OpenDbConnection();
        var w = new Waybill(db)
        {
            Identity = Guid.Empty,
            Name = "WaybillTest"
        };

        w.TestMethod2();
    }

    [Test]
    public void SubExpressions_TestMethod3()
    {
        using var db = OpenDbConnection();
        var w = new Waybill(db)
        {
            Identity = Guid.Empty,
            Name = "WaybillTest"
        };

        w.TestMethod3();
    }

    [Test]
    public void SubExpressions_with_CustomSqlExpression_and_merging_multiple_predicates()
    {
        var db = new OrmLiteConnection(new OrmLiteConnectionFactory("test", new CustomSqlServerDialectProvider()));

        var q = db.From<MarginItem>().Where(s => Sql.In(s.Identity,
            db.From<WaybillItem>()
                .Where(w => Sql.In(w.WaybillId,
                    db.From<Waybill>()
                        .Where(bb => bb.Identity == null)
                        .And(bb => bb.Name == "test")
                        .Select(ww => ww.Identity))
                )
                .Select(b => b.MarginItemId)));

        Assert.That(q.ToSelectStatement().NormalizeSql(), Does.Contain("@"));
    }
}

class CustomSqlExpression<T>(IOrmLiteDialectProvider dialectProvider) : SqlServerExpression<T>(dialectProvider)
{
    private Expression<Func<T, bool>> _whereExpression;

    public override SqlExpression<T> And(Expression<Func<T, bool>> predicate)
    {
        _whereExpression = _whereExpression == null ? predicate : predicate.And(_whereExpression);
        return base.And(predicate);
    }
}

class CustomSqlServerDialectProvider : SqlServerOrmLiteDialectProvider
{
    public override SqlExpression<T> SqlExpression<T>()
    {
        return new CustomSqlExpression<T>(this);
    }
}

public class WaybillItem : BaseObject
{
    public string WbItemName { get; set; }

    [ForeignKey(typeof(Waybill))]
    public Guid WaybillId { get; set; }

    [ForeignKey(typeof(MarginItem))]
    public Guid? MarginItemId { get; set; }
}

public class MarginItem : BaseObject
{
    public string MarginName { get; set; }
}


//-------------------------
public class Person3
{
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; }
}

public class Order3
{
    [AutoIncrement]
    public int Id { get; set; }

    [References(typeof(Person3))]
    public int Person2Id { get; set; }

    public DateTime Order2Date { get; set; }

    public int Order2TypeId { get; set; }
}

public class BaseObject
{
    public Guid? Identity { get; set; }
}

public class Waybill : BaseObject
{
    private readonly IDbConnection db;

    public string Name { get; set; }

    public Waybill(IDbConnection db)
    {
        this.db = db;
    }

    public void TestMethod1()
    {
        var localIdentity = this.Identity;

        var q = this.db.From<MarginItem>()
            .Where(s => Sql.In(s.Identity,
                this.db.From<WaybillItem>()
                    .Where(b => b.WaybillId == localIdentity)
                    .Select(b => b.MarginItemId)));

        q.ToSelectStatement().PrintDump();
        Assert.That(q.ToSelectStatement().NormalizeSql(), Does.Contain("@"));
    }

    public void TestMethod2()
    {
        var q = db.From<MarginItem>()
            .Where(s => Sql.In(s.Identity,
                db.From<WaybillItem>()
                    .Where(b => b.WaybillId == this.Identity)
                    .Select(b => b.MarginItemId)));

        q.ToSelectStatement().PrintDump();
        Assert.That(q.ToSelectStatement().NormalizeSql(), Does.Contain("@"));
    }

    public void TestMethod3()
    {
        var q = db.From<MarginItem>()
            .Where(s => Sql.In(s.Identity,
                db.From<WaybillItem>()
                    .LeftJoin<MarginItem>((wi, mi) => wi.MarginItemId == mi.Identity)
                    .Where(b => b.WaybillId == this.Identity)
                    .And<MarginItem>(b => b.MarginName == this.Name)
                    .Select(b => b.MarginItemId)));

        q.ToSelectStatement().PrintDump();
        Assert.That(q.ToSelectStatement().NormalizeSql(), Does.Contain("@"));
    }
}