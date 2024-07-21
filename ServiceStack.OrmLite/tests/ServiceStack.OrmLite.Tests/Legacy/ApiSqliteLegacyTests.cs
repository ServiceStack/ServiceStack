using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;
using ServiceStack.OrmLite.Legacy;

namespace ServiceStack.OrmLite.Tests.Legacy;

[TestFixtureOrmLiteDialects(Dialect.Sqlite)]
public class ApiSqliteLegacyTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    private IDbConnection db;

    [SetUp]
    public void SetUp()
    {
        db = OpenDbConnection();
        db.DropAndCreateTable<Person>();
        db.DropAndCreateTable<PersonWithAutoId>();
    }

    [TearDown]
    public void TearDown()
    {
        db.Dispose();
    }

#pragma warning disable 618
    [Test]
    public void API_Sqlite_Legacy_Examples()
    {
        db.Insert(Person.Rockstars);

        db.Select<Person>(q => q.Where(x => x.Age > 40).OrderBy(x => x.Id));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" \nFROM \"Person\"\nWHERE (\"Age\" > @0)\nORDER BY \"Id\""));

        db.Select<Person>(q => q.Where(x => x.Age > 40));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" \nFROM \"Person\"\nWHERE (\"Age\" > @0)"));

        db.Single<Person>(q => q.Where(x => x.Age == 42));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" \nFROM \"Person\"\nWHERE (\"Age\" = @0)"));

        db.SelectFmt<Person>("Age > {0}", 40);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE Age > 40"));

        db.SelectFmt<Person>("SELECT * FROM Person WHERE Age > {0}", 40);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM Person WHERE Age > 40"));

        db.SelectFmt<EntityWithId>(typeof(Person), "Age > {0}", 40);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\" FROM \"Person\" WHERE Age > 40"));

        db.SelectLazyFmt<Person>("Age > {0}", 40).ToList();
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE Age > 40"));

        db.SingleFmt<Person>("Age = {0}", 42);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE Age = 42"));

        db.ScalarFmt<int>("SELECT COUNT(*) FROM Person WHERE Age > {0}", 40);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) FROM Person WHERE Age > 40"));

        db.ColumnFmt<string>("SELECT LastName FROM Person WHERE Age = {0}", 27);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT LastName FROM Person WHERE Age = 27"));

        db.ColumnDistinctFmt<int>("SELECT Age FROM Person WHERE Age < {0}", 50);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Age FROM Person WHERE Age < 50"));

        db.LookupFmt<int, string>("SELECT Age, LastName FROM Person WHERE Age < {0}", 50);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Age, LastName FROM Person WHERE Age < 50"));

        db.DictionaryFmt<int, string>("SELECT Id, LastName FROM Person WHERE Age < {0}", 50);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Id, LastName FROM Person WHERE Age < 50"));

        db.ExistsFmt<Person>("Age = {0}", 42);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE Age = 42"));
        db.ExistsFmt<Person>("SELECT * FROM Person WHERE Age = {0}", 42);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM Person WHERE Age = 42"));

        var rowsAffected = db.ExecuteNonQuery("UPDATE Person SET LastName={0} WHERE Id={1}".SqlFmt(DialectProvider, "WaterHouse", 7));
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE Person SET LastName='WaterHouse' WHERE Id=7"));

        db.InsertOnly(new PersonWithAutoId { FirstName = "Amy", Age = 27 }, q => q.Insert(x => new { x.FirstName, x.Age }));
        Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO \"PersonWithAutoId\" (\"FirstName\",\"Age\") VALUES (@FirstName,@Age)"));

        db.InsertOnly(new PersonWithAutoId { FirstName = "Amy", Age = 27 }, db.From<PersonWithAutoId>().Insert(x => new { x.FirstName, x.Age }));
        Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO \"PersonWithAutoId\" (\"FirstName\",\"Age\") VALUES (@FirstName,@Age)"));

        db.InsertOnly(new PersonWithAutoId { FirstName = "Amy", Age = 27 }, q => q.Insert(x => new { x.FirstName, x.Age }));
        Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO \"PersonWithAutoId\" (\"FirstName\",\"Age\") VALUES (@FirstName,@Age)"));

        db.UpdateOnly(new Person { FirstName = "JJ", LastName = "Hendo" }, ev => ev.Update(p => p.FirstName));
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET \"FirstName\"=@FirstName"));

        db.UpdateOnly(new Person { FirstName = "JJ" }, q => q.Update(p => p.FirstName).Where(x => x.FirstName == "Jimi"));
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET \"FirstName\"=@FirstName WHERE (\"FirstName\" = @0)"));

        db.UpdateFmt<Person>(set: "FirstName = {0}".SqlFmt(DialectProvider, "JJ"), where: "LastName = {0}".SqlFmt(DialectProvider, "Hendrix"));
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET FirstName = 'JJ' WHERE LastName = 'Hendrix'"));

        db.UpdateFmt(table: "Person", set: "FirstName = {0}".SqlFmt(DialectProvider, "JJ"), where: "LastName = {0}".SqlFmt(DialectProvider, "Hendrix"));
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET FirstName = 'JJ' WHERE LastName = 'Hendrix'"));

        db.DeleteFmt<Person>("Age = {0}", 27);
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"Person\" WHERE Age = 27"));

        db.DeleteFmt(typeof(Person), "Age = {0}", 27);
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"Person\" WHERE Age = 27"));

        db.Delete<Person>(ev => ev.Where(p => p.Age == 27));
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"Person\" WHERE (\"Age\" = @0)"));

        db.DeleteFmt<Person>(where: "Age = {0}".SqlFmt(DialectProvider, 27));
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"Person\" WHERE Age = 27"));

        db.DeleteFmt(table: "Person", where: "Age = {0}".SqlFmt(DialectProvider, 27));
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"Person\" WHERE Age = 27"));
    }
#pragma warning restore 618
}