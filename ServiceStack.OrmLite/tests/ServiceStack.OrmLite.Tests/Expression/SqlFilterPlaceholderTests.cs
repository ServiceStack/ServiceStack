using NUnit.Framework;
using ServiceStack.OrmLite.Tests.UseCases;

namespace ServiceStack.OrmLite.Tests.Expression;

/// <summary>
/// {n} placeholders in Where(string, params object[]) filters are sent as db params
/// </summary>
[TestFixtureOrmLite]
public class SqlFilterPlaceholderTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Placeholders_are_sent_as_params()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var q = db.From<Book>().Where("Title = {0} OR Author = {1}", "Dune", "Carl Sagan");

        Assert.That(q.Params.Count, Is.EqualTo(2));
        Assert.That(db.Select(q).Map(x => x.Title), Is.EquivalentTo(new[] { "Dune", "Cosmos" }));
    }

    [Test]
    public void Placeholders_can_be_used_in_any_order_and_repeated()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var q = db.From<Book>().Where("(Year >= {1} AND Year < {0}) OR Title = {2} OR Author = {2}", 1970, 1960, "Cosmos");

        Assert.That(q.Params.Count, Is.EqualTo(3));
        Assert.That(db.Select(q).Map(x => x.Title), Is.EquivalentTo(new[] { "Dune", "The Guns of August", "Cosmos" }));
    }

    [Test]
    public void Placeholders_with_multiple_digits()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // {1} must not match the start of {10}
        var years = new object[] { 0, 1937, 0, 0, 0, 0, 0, 0, 0, 0, 2015 };
        var q = db.From<Book>().Where("Year IN ({1}, {10})", years);

        Assert.That(db.Select(q).Map(x => x.Title), Is.EquivalentTo(new[] { "The Hobbit", "SPQR" }));
    }

    [Test]
    public void SqlInValues_are_expanded_into_params()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var q = db.From<Book>().Where("Author IN ({0})", new SqlInValues(new[] { "Frank Herbert", "Carl Sagan" }));

        Assert.That(q.Params.Count, Is.EqualTo(2));
        Assert.That(db.Select(q).Map(x => x.Title), Is.EquivalentTo(new[] { "Dune", "Cosmos" }));
    }
}
