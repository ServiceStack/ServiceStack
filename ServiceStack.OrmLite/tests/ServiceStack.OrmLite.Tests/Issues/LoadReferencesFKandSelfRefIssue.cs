using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Issues;

public class PareInfo
{
    [AutoIncrement]
    public int Id { get; set; }
    public string name { get; set; }
    public int? PareInfoId { get; set; }
    [Reference]
    public PareInfo Pare { get; set; }
}

[TestFixtureOrmLite]
public class LoadReferencesFKandSelfRefIssue(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Does_not_populate_both_FK_and_self_reference()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<PareInfo>();

        var p1 = new PareInfo { name = "p1" };
        var p2 = new PareInfo
        {
            name = "p2",
        };
        p1.Pare = p2;

        db.Save(p1, references: true);

        var p1AndRefs = db.LoadSingleById<PareInfo>(p1.Id);
        Assert.That(p1AndRefs.Pare, Is.Not.Null);
        Assert.That(p1AndRefs.PareInfoId, Is.EqualTo(p2.Id));

        var p2AndRefs = db.LoadSingleById<PareInfo>(p2.Id);
        Assert.That(p2AndRefs.Pare, Is.Null);
        Assert.That(p2AndRefs.PareInfoId, Is.Null);

        var rows = db.Select<PareInfo>().OrderBy(x => x.Id).ToList();
        Assert.That(rows[0].name, Is.EqualTo("p1"));
        Assert.That(rows[0].PareInfoId, Is.EqualTo(p2.Id));
        Assert.That(rows[1].name, Is.EqualTo("p2"));
        Assert.That(rows[1].PareInfoId, Is.Null);
    }
}