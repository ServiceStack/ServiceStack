using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;

namespace ServiceStack.OrmLite.Tests.Issues;

[TestFixtureOrmLite]
public class NullExpressionIssues(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Can_compare_null_constant()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Person>();

        db.Insert(new Person
        {
            Id = 1,
            FirstName = "FirstName",
            LastName = null,
            Age = 27
        });
        
        db.UpdateOnly(() => new Person
        {
            LastName = "<none>"
        }, where:x => x.LastName == null);
        
        Assert.That(db.SingleById<Person>(1).LastName, Is.EqualTo("<none>"));
        
        db.UpdateOnly(() => new Person
        {
            LastName = "NULL"
        }, where:x => x.LastName != null);
        
        Assert.That(db.SingleById<Person>(1).LastName, Is.EqualTo("NULL"));

        db.UpdateOnly(() => new Person
        {
            LastName = "<none>"
        }, where:x => x.LastName == "NULL");
        
        Assert.That(db.SingleById<Person>(1).LastName, Is.EqualTo("<none>"));

        db.UpdateOnly(() => new Person
        {
            LastName = "NULL"
        }, where:x => x.LastName != "NULL");
        
        Assert.That(db.SingleById<Person>(1).LastName, Is.EqualTo("NULL"));
    }
}