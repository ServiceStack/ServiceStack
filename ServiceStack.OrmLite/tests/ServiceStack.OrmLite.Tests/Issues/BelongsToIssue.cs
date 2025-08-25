using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Issues;

class A
{
    public int Id { get; set; }
    public string SomeAProperty { get; set; }
}
class B
{
    public int Id { get; set; }
    public string SomeBProperty { get; set; }
    public int AId { get; set; }
}
class C
{
    public int Id { get; set; }
    public string SomeCProperty { get; set; }
    public int BId { get; set; }
}
class D
{
    public int Id { get; set; }

    [BelongTo(typeof(B))]
    public int BId { get; set; }

    public int CId { get; set; }

    public string SomeAProperty { get; set; }
    public string SomeBProperty { get; set; }
    public string SomeCProperty { get; set; }
}

[TestFixtureOrmLite]
public class BelongsToIssue(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Does_use_BelongsTo_to_specify_Attribute()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<A>();
        db.DropAndCreateTable<B>();
        db.DropAndCreateTable<C>();

        db.Insert(new A { Id = 1, SomeAProperty = "A1" });
        db.Insert(new A { Id = 2, SomeAProperty = "A2" });
        db.Insert(new A { Id = 3, SomeAProperty = "A3" });
        db.Insert(new B { Id = 10, SomeBProperty = "B1", AId = 1 });
        db.Insert(new B { Id = 11, SomeBProperty = "B2", AId = 2 });
        db.Insert(new C { Id = 20, SomeCProperty = "C", BId = 10 });

        var q = db.From<A>()
            .Join<B>()
            .LeftJoin<B,C>();

        var results = db.Select<D>(q);
        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results.Map(x => x.BId), Is.EquivalentTo(new[] { 10, 11 }));
    }
}