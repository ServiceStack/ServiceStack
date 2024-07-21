using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests;

public class ComputedTest
{
    [AutoIncrement] public int Id { get; set; }
    [Computed] public string DisplayName => FirstName + " " + LastName;
    public string FirstName { get; set; }
    public string LastName { get; set; }
}


[TestFixtureOrmLite]
public class ComputedTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Does_not_select_computed_property()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ComputedTest>();

        var id = db.Insert(new ComputedTest { FirstName = "First", LastName = "Last" }, selectIdentity:true);
        var result = db.SingleById<ComputedTest>(id);
        
        Assert.That(result.FirstName, Is.EqualTo("First"));
    }
}