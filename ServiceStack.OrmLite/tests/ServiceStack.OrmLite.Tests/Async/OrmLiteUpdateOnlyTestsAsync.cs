using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;

namespace ServiceStack.OrmLite.Tests.Async;

[TestFixtureOrmLite]
public class OrmLiteUpdateOnlyTestsAsync(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public async Task Supports_different_ways_to_UpdateOnly()
    {
        using var db = await OpenDbConnectionAsync();
        db.DropAndCreateTable<Person>();
        await db.InsertAsync(new Person
            {Id = 1, FirstName = "OriginalFirst", LastName = "OriginalLast", Age = 100});

        await db.UpdateOnlyAsync(() => new Person {FirstName = "UpdatedFirst", Age = 27});
        var row = (await db.SelectAsync<Person>()).First();
        Assert.That(row, Is.EqualTo(new Person(1, "UpdatedFirst", "OriginalLast", 27)));

        await db.DeleteAllAsync<Person>();
        await db.InsertAsync(new Person
            {Id = 1, FirstName = "OriginalFirst", LastName = "OriginalLast", Age = 100});

        await db.UpdateOnlyFieldsAsync(new Person {FirstName = "UpdatedFirst", Age = 27}, p => p.FirstName);
        row = (await db.SelectAsync<Person>()).First();
        Assert.That(row, Is.EqualTo(new Person(1, "UpdatedFirst", "OriginalLast", 100)));

        await db.DeleteAllAsync<Person>();
        await db.InsertAsync(new Person
            {Id = 1, FirstName = "OriginalFirst", LastName = "OriginalLast", Age = 100});

        await db.UpdateOnlyFieldsAsync(new Person {FirstName = "UpdatedFirst", Age = 27},
            p => new {p.FirstName, p.Age});
        row = (await db.SelectAsync<Person>()).First();
        Assert.That(row, Is.EqualTo(new Person(1, "UpdatedFirst", "OriginalLast", 27)));

        await db.DeleteAllAsync<Person>();
        await db.InsertAsync(new Person
            {Id = 1, FirstName = "OriginalFirst", LastName = "OriginalLast", Age = 100});

        await db.UpdateOnlyFieldsAsync(new Person {FirstName = "UpdatedFirst", Age = 27}, new[] {"FirstName", "Age"});
        row = (await db.SelectAsync<Person>()).First();
        Assert.That(row, Is.EqualTo(new Person(1, "UpdatedFirst", "OriginalLast", 27)));
    }
}