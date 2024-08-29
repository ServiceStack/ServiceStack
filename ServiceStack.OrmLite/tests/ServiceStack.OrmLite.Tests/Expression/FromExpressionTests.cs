using System.Data;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;

namespace ServiceStack.OrmLite.Tests.Expression;

public class Band
{
    public string Name { get; set; }
    public int PersonId { get; set; }
}

[TestFixtureOrmLite]
public class FromExpressionTests(DialectContext context) : ExpressionsTestBase(context)
{
    public void Init(IDbConnection db)
    {
        db.DropAndCreateTable<Person>();
        db.DropAndCreateTable<Band>();

        db.InsertAll(Person.Rockstars);

        db.Insert(new Band { Name = "The Doors", PersonId = 3 });
        db.Insert(new Band { Name = "Nirvana", PersonId = 4 });
    }

    [Test]
    public void Can_select_from_custom_FROM_expression()
    {
        using (var db = OpenDbConnection())
        {
            Init(db);

            var results = db.Select(db.From<Person>("Person INNER JOIN Band ON Person.Id = Band.{0}".Fmt("PersonId".SqlColumn(DialectProvider))));

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.ConvertAll(x => x.FirstName), Is.EquivalentTo(new[] { "Kurt", "Jim" }));
        }
    }
}