using System.Data;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Expression;

[TestFixtureOrmLite]
public class SqlExpressionWhereTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    private static void Init(IDbConnection db)
    {
        db.DropAndCreateTable<Table1>();
        db.DropAndCreateTable<Table2>();
        db.DropAndCreateTable<Table3>();
        db.DropAndCreateTable<Table4>();
        db.DropAndCreateTable<Table5>();

        db.Insert(new Table1 {Id = 1, String = "A"});
        db.Insert(new Table2 {Id = 1, String = "A"});
        db.Insert(new Table3 {Id = 1, String = "A"});
        db.Insert(new Table4 {Id = 1, String = "A"});
        db.Insert(new Table5 {Id = 1, String = "A"});
    }

    [Test]
    public void Can_use_Where_on_multiple_tables()
    {
        using (var db = OpenDbConnection())
        {
            Init(db);

            var q = db.From<Table1>()
                .Join<Table2>((t1, t2) => t1.Id == t2.Id)
                .Join<Table3>((t1, t3) => t1.Id == t3.Id)
                .Join<Table4>((t1, t4) => t1.Id == t4.Id)
                .Join<Table5>((t1, t5) => t1.Id == t5.Id)
                .Where<Table1, Table2, Table3, Table4, Table5>((t1, t2, t3, t4, t5) =>
                    t1.String == t2.String
                    && t2.String == t3.String
                    && t4.String == t5.String);

            var results = db.Select(q);

            db.GetLastSql().Print();

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].String, Is.EqualTo("A"));
        }
    }

    [Test]
    public void Can_add_join_condition_to_join_multiple_tables_using_typed_api()
    {
        using (var db = OpenDbConnection())
        {
            Init(db);

            var q = db.From<Table1>()
                .Join<Table2>((t1, t2) => t1.Id == t2.Id)
                .LeftJoin<Table1, Table3, Table2>((t1, t3, t2) =>
                    t1.Id == t3.Id && t3.Id == t2.Id);

            var results = db.Select(q);

            db.GetLastSql().Print();

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].String, Is.EqualTo("A"));

            q = db.From<Table1>()
                .Join<Table2>((t1, t2) => t1.Id == t2.Id)
                .Join<Table3>((t1, t3) => t1.Id == t3.Id)
                .LeftJoin<Table1, Table4, Table2, Table3>((t1, t4, t2, t3) =>
                    t1.Id == t4.Id && t4.Id == t2.Id && t4.Id == t3.Id);

            results = db.Select(q);

            db.GetLastSql().Print();

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].String, Is.EqualTo("A"));
        }
    }

    [Test]
    public void Can_add_join_condition_to_join_multiple_tables_using_custom_join_format()
    {
        using (var db = OpenDbConnection())
        {
            Init(db);

            var q = db.From<Table1>()
                .Join<Table2>((t1, t2) => t1.Id == t2.Id)
                .LeftJoin<Table1, Table3>((t1, t3) => t1.Id == t3.Id,
                    (dialect, modelDef, joinExpr) =>
                    {
                        var injectJoin = " AND Table3.Id = Table2.Id)";
                        return dialect.QuoteTable(modelDef.ModelName)
                               + " " + joinExpr.Replace(")", injectJoin);
                    });

            var results = db.Select(q);

            db.GetLastSql().Print();

            Assert.That(db.GetLastSql(), Does.Contain("AND Table3.Id = Table2.Id"));

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].String, Is.EqualTo("A"));
        }
    }

    [Test]
    public void Can_add_join_condition_to_join_multiple_tables_using_custom_join_sql()
    {
        using (var db = OpenDbConnection())
        {
            Init(db);

            var q = db.From<Table1>()
                .Join<Table2>((t1, t2) => t1.Id == t2.Id)
                .CustomJoin("LEFT JOIN Table3 ON (Table1.Id = Table3.Id AND Table3.Id = Table2.Id)");

            var results = db.Select(q);

            db.GetLastSql().Print();

            Assert.That(db.GetLastSql(), Does.Contain("Table1.Id = Table3.Id AND Table3.Id = Table2.Id"));

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].String, Is.EqualTo("A"));
        }
    }

    [Test]
    public void Can_join_multiple_tables_using_custom_sql()
    {
        using (var db = OpenDbConnection())
        {
            Init(db);

            var results = db.SqlList<Table1>(@"SELECT Table1.* 
                    FROM Table1 INNER JOIN Table2 ON(Table1.Id = Table2.Id) LEFT JOIN Table3 ON(Table1.Id = Table3.Id AND Table3.Id = Table2.Id)");

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].String, Is.EqualTo("A"));
        }
    }

}