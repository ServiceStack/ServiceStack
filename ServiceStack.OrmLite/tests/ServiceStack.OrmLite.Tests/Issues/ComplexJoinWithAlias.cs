using System.Data;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;

namespace ServiceStack.OrmLite.Tests.Issues;

public class ClassA : IHasIntId
{
    [AutoIncrement]
    [PrimaryKey]
    public int Id { get; set; }

    [Alias("A")]
    public string ColumnA { get; set; }
}

public class ClassB : IHasIntId
{
    [AutoIncrement]
    [PrimaryKey]
    public int Id { get; set; }

    public string ColumnB { get; set; }
}

[TestFixtureOrmLite]
public class ComplexJoinWithAlias(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    private static long _classAColumnAId;

    private static void Init(IDbConnection db)
    {
        db.DropAndCreateTable<ClassA>();
        db.DropAndCreateTable<ClassB>();

        _classAColumnAId = db.Insert(new ClassA { ColumnA = "1" }, true);
        db.Insert(new ClassA { ColumnA = "2" });
        db.Insert(new ClassA { ColumnA = "3" });

        db.Insert(new ClassB { ColumnB = "1" });
        db.Insert(new ClassB { ColumnB = "2" });
    }

    [Test]
    public void ComplexJoin_With_Alias_Columns()
    {
        using var db = OpenDbConnection();
        Init(db);

        var q = db.From<ClassA>()
            .Join<ClassB>((a, b) => a.ColumnA == b.ColumnB)
            .Where<ClassA>(a => a.Id == _classAColumnAId);

        var results = db.Single(q);
        Assert.That(results.ColumnA, Is.EqualTo("1"));
    }
}