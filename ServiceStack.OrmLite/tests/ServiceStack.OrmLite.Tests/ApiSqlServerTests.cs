using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLiteDialects(Dialect.AnySqlServer)]
public class ApiSqlServerTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    [Ignore("Needs review - MONOREPO")]
    public void API_SqlServer_Examples()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Person>();
        db.DropAndCreateTable<PersonWithAutoId>();

        db.Insert(Person.Rockstars);

        db.Select<Person>(x => x.Age > 40);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" \nFROM \"Person\"\nWHERE (\"Age\" > @0)"));

        db.Select(db.From<Person>().Where(x => x.Age > 40).OrderBy(x => x.Id));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" \nFROM \"Person\"\nWHERE (\"Age\" > @0)\nORDER BY \"Id\""));

        db.Select(db.From<Person>().Where(x => x.Age > 40));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" \nFROM \"Person\"\nWHERE (\"Age\" > @0)"));

        db.Single<Person>(x => x.Age == 42);
        if (DialectFeatures.RowOffset)
        {
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" \nFROM \"Person\"\nWHERE (\"Age\" = @0) ORDER BY 1 OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY"));
        }
        else
        {
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT TOP 1 \"Id\", \"FirstName\", \"LastName\", \"Age\" \nFROM \"Person\"\nWHERE (\"Age\" = @0)"));    
        }

        db.Single(db.From<Person>().Where(x => x.Age == 42));
        if (DialectFeatures.RowOffset)
        {
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" \nFROM \"Person\"\nWHERE (\"Age\" = @0) ORDER BY 1 OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY"));
        }
        else
        {
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT TOP 1 \"Id\", \"FirstName\", \"LastName\", \"Age\" \nFROM \"Person\"\nWHERE (\"Age\" = @0)"));    
        }

        db.Scalar<Person, int>(x => Sql.Max(x.Age));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Max(\"Age\") \nFROM \"Person\""));

        db.Scalar<Person, int>(x => Sql.Max(x.Age), x => x.Age < 50);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Max(\"Age\") \nFROM \"Person\"\nWHERE (\"Age\" < @0)"));

        db.Scalar<Person, int>(x => Sql.Count(x.Age), x => x.Id > 0);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Count(\"Age\") \nFROM \"Person\"\nWHERE (\"Id\" > @0)"));

        db.Count<Person>(x => x.Age < 50);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) \nFROM \"Person\"\nWHERE (\"Age\" < @0)"));

        db.Count(db.From<Person>().Where(x => x.Age < 50));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) \nFROM \"Person\"\nWHERE (\"Age\" < @0)"));


        db.Select<Person>("Age > 40");
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE Age > 40"));

        db.Select<Person>("SELECT * FROM Person WHERE Age > 40");
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM Person WHERE Age > 40"));

        db.Select<Person>("SELECT * FROM Person WHERE Age > @age", new[] { db.CreateParam("age", 40) });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM Person WHERE Age > @age"));

        db.Select<Person>("Age > @age", new { age = 40 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE Age > @age"));

        db.Select<Person>("SELECT * FROM Person WHERE Age > @age", new { age = 40 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM Person WHERE Age > @age"));

        db.Select<Person>("Age > @age", new Dictionary<string, object> { { "age", 40 } });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE Age > @age"));

        db.Select<Person>("SELECT * FROM Person WHERE Age > @age", new Dictionary<string, object> { { "age", 40 } });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM Person WHERE Age > @age"));

        db.Select<EntityWithId>(typeof(Person));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\" FROM \"Person\""));

        db.Select<EntityWithId>(typeof(Person), "Age > @age", new { age = 40 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\" FROM \"Person\" WHERE Age > @age"));

        db.Where<Person>("Age", 27);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE \"Age\" = @Age"));

        db.Where<Person>(new { Age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE \"Age\" = @Age"));

        db.SelectByIds<Person>(new[] { 1, 2, 3 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE \"Id\" IN (@0,@1,@2)"));

        db.SelectNonDefaults(new Person { Id = 1 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE \"Id\" = @Id"));

        db.SelectNonDefaults("Age > @Age", new Person { Age = 40 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE Age > @Age"));

        db.SelectLazy<Person>().ToList();
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\""));

        db.SelectLazy(db.From<Person>().Where(x => x.Age > 40)).ToList();
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" \nFROM \"Person\"\nWHERE (\"Age\" > @0)"));

        db.SelectLazy<Person>("Age > @age", new { age = 40 }).ToList();
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE Age > @age"));

        db.WhereLazy<Person>(new { Age = 27 }).ToList();
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE \"Age\" = @Age"));

        db.SingleById<Person>(1);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE \"Id\" = @Id"));

        db.Single<Person>(new { Age = 42 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE \"Age\" = @Age"));

        db.Single<Person>("Age = @age", new { age = 42 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE Age = @age"));

        db.Single<Person>("Age = @age", new[]{ db.CreateParam("age", 42)});
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE Age = @age"));

        db.SingleById<Person>(1);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE \"Id\" = @Id"));

        db.SingleWhere<Person>("Age", 42);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE \"Age\" = @Age"));

        db.Scalar<int>(db.From<Person>().Where(q => q.Age > 40).Select(Sql.Count("*")));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) \nFROM \"Person\"\nWHERE (\"Age\" > @0)"));
        db.Scalar<int>(db.From<Person>().Where(q => q.Age > 40).Select(x => Sql.Count("*")));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Count(*) \nFROM \"Person\"\nWHERE (\"Age\" > @0)"));

        db.Scalar<int>("SELECT COUNT(*) FROM Person WHERE Age > @age", new { age = 40 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) FROM Person WHERE Age > @age"));

        db.Scalar<int>("SELECT COUNT(*) FROM Person WHERE Age > @age", new[] { db.CreateParam("age", 40) });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) FROM Person WHERE Age > @age"));

        db.Column<string>(db.From<Person>().Select(x => x.LastName).Where(q => q.Age == 27));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"LastName\" \nFROM \"Person\"\nWHERE (\"Age\" = @0)"));

        db.Column<string>("SELECT LastName FROM Person WHERE Age = @age", new { age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT LastName FROM Person WHERE Age = @age"));

        db.Column<string>("SELECT LastName FROM Person WHERE Age = @age", new[] { db.CreateParam("age", 27) });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT LastName FROM Person WHERE Age = @age"));

        db.ColumnDistinct<int>(db.From<Person>().Select(x => x.Age).Where(q => q.Age < 50));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Age\" \nFROM \"Person\"\nWHERE (\"Age\" < @0)"));

        db.ColumnDistinct<int>("SELECT Age FROM Person WHERE Age < @age", new { age = 50 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Age FROM Person WHERE Age < @age"));

        db.ColumnDistinct<int>("SELECT Age FROM Person WHERE Age < @age", new[] { db.CreateParam("age", 50) });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Age FROM Person WHERE Age < @age"));

        db.Lookup<int, string>(db.From<Person>().Select(x => new { x.Age, x.LastName }).Where(q => q.Age < 50));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Age\", \"LastName\" \nFROM \"Person\"\nWHERE (\"Age\" < @0)"));

        db.Lookup<int, string>("SELECT Age, LastName FROM Person WHERE Age < @age", new { age = 50 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Age, LastName FROM Person WHERE Age < @age"));

        db.Dictionary<int, string>(db.From<Person>().Select(x => new { x.Id, x.LastName }).Where(x => x.Age < 50));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"LastName\" \nFROM \"Person\"\nWHERE (\"Age\" < @0)"));

        db.Dictionary<int, string>("SELECT Id, LastName FROM Person WHERE Age < @age", new { age = 50 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Id, LastName FROM Person WHERE Age < @age"));

        db.Exists<Person>(x => x.Age < 50);
        if (DialectFeatures.RowOffset)
        {
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT 'exists' \nFROM \"Person\"\nWHERE (\"Age\" < @0) ORDER BY 1 OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY"));
        }
        else
        {
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT TOP 1 'exists' \nFROM \"Person\"\nWHERE (\"Age\" < @0)"));
        }

        db.Exists(db.From<Person>().Where(x => x.Age < 50));
        if (DialectFeatures.RowOffset)
        { 
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT 'exists' \nFROM \"Person\"\nWHERE (\"Age\" < @0) ORDER BY 1 OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY"));
        }
        else
        {
            Assert.That(db.GetLastSql(), Is.EqualTo("SELECT TOP 1 'exists' \nFROM \"Person\"\nWHERE (\"Age\" < @0)"));
        }

        db.Exists<Person>(new { Age = 42 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE \"Age\" = @Age"));

        db.Exists<Person>("Age = @age", new { age = 42 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"Id\", \"FirstName\", \"LastName\", \"Age\" FROM \"Person\" WHERE Age = @age"));
        db.Exists<Person>("SELECT * FROM Person WHERE Age = @age", new { age = 42 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM Person WHERE Age = @age"));
        Assert.That(db.ExistsById<Person>(1));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT 1 FROM \"Person\" WHERE \"Id\" = @Id"));
        Assert.That(db.ExistsById<Person>(-1), Is.False);

        db.SqlList<Person>(db.From<Person>().Select("*").Where(q => q.Age < 50));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * \nFROM \"Person\"\nWHERE (\"Age\" < @0)"));

        db.SqlList<Person>("SELECT * FROM Person WHERE Age < @age", new { age = 50 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM Person WHERE Age < @age"));

        db.SqlList<Person>("SELECT * FROM Person WHERE Age < @age", new[] { db.CreateParam("age", 50) });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM Person WHERE Age < @age"));

        db.SqlList<Person>("SELECT * FROM Person WHERE Age < @age", new Dictionary<string, object> { { "age", 50 } });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM Person WHERE Age < @age"));

        db.SqlColumn<string>(db.From<Person>().Select(x => x.LastName).Where(q => q.Age < 50));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT \"LastName\" \nFROM \"Person\"\nWHERE (\"Age\" < @0)"));

        db.SqlColumn<string>("SELECT LastName FROM Person WHERE Age < @age", new { age = 50 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT LastName FROM Person WHERE Age < @age"));

        db.SqlColumn<string>("SELECT LastName FROM Person WHERE Age < @age", new[] { db.CreateParam("age", 50) });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT LastName FROM Person WHERE Age < @age"));

        db.SqlColumn<string>("SELECT LastName FROM Person WHERE Age < @age", new Dictionary<string, object> { { "age", 50 } });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT LastName FROM Person WHERE Age < @age"));

        db.SqlScalar<int>(db.From<Person>().Select(Sql.Count("*")).Where(q => q.Age < 50));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) \nFROM \"Person\"\nWHERE (\"Age\" < @0)"));

        db.SqlScalar<int>("SELECT COUNT(*) FROM Person WHERE Age < @age", new { age = 50 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) FROM Person WHERE Age < @age"));

        db.SqlScalar<int>("SELECT COUNT(*) FROM Person WHERE Age < @age", new[] { db.CreateParam("age", 50) });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) FROM Person WHERE Age < @age"));

        db.SqlScalar<int>("SELECT COUNT(*) FROM Person WHERE Age < @age", new Dictionary<string, object> { { "age", 50 } });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) FROM Person WHERE Age < @age"));

        var rowsAffected = db.ExecuteNonQuery("UPDATE Person SET LastName=@name WHERE Id=@id", new { name = "WaterHouse", id = 7 });
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE Person SET LastName=@name WHERE Id=@id"));

        db.Insert(new Person { Id = 7, FirstName = "Amy", LastName = "Winehouse", Age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO \"Person\" (\"Id\",\"FirstName\",\"LastName\",\"Age\") VALUES (@Id,@FirstName,@LastName,@Age)"));

        db.Insert(new Person { Id = 8, FirstName = "Tupac", LastName = "Shakur", Age = 25 },
            new Person { Id = 9, FirstName = "Tupac", LastName = "Shakur2", Age = 26 });
        Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO \"Person\" (\"Id\",\"FirstName\",\"LastName\",\"Age\") VALUES (@Id,@FirstName,@LastName,@Age)"));

        db.InsertAll(new[] { new Person { Id = 10, FirstName = "Biggie", LastName = "Smalls", Age = 24 } });
        Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO \"Person\" (\"Id\",\"FirstName\",\"LastName\",\"Age\") VALUES (@Id,@FirstName,@LastName,@Age)"));

        db.InsertOnly(new PersonWithAutoId { FirstName = "Amy", Age = 27 }, x => new { x.FirstName, x.Age });
        Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO \"PersonWithAutoId\" (\"FirstName\",\"Age\") VALUES (@FirstName,@Age)"));

        db.InsertOnly(() => new PersonWithAutoId { FirstName = "Amy", Age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO \"PersonWithAutoId\" (\"FirstName\",\"Age\") VALUES (@FirstName,@Age)"));

        db.Update(new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET \"FirstName\"=@FirstName, \"LastName\"=@LastName, \"Age\"=@Age WHERE \"Id\"=@Id"));

        db.Update(new Person { Id = 8, FirstName = "Tupac", LastName = "Shakur3", Age = 27 },
            new Person { Id = 9, FirstName = "Tupac", LastName = "Shakur4", Age = 28 });

        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET \"FirstName\"=@FirstName, \"LastName\"=@LastName, \"Age\"=@Age WHERE \"Id\"=@Id"));

        db.Update(new[] { new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 } });
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET \"FirstName\"=@FirstName, \"LastName\"=@LastName, \"Age\"=@Age WHERE \"Id\"=@Id"));

        db.UpdateAll(new[] { new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 } });
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET \"FirstName\"=@FirstName, \"LastName\"=@LastName, \"Age\"=@Age WHERE \"Id\"=@Id"));

        db.Update(new Person { Id = 1, FirstName = "JJ", Age = 27 }, where: x => x.LastName == "Hendrix");
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET \"Id\"=@Id, \"FirstName\"=@FirstName, \"LastName\"=@LastName, \"Age\"=@Age WHERE (\"LastName\" = @0)"));

        db.Update<Person>(new { FirstName = "JJ" }, where: x => x.LastName == "Hendrix");
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET \"FirstName\"=@FirstName WHERE (\"LastName\" = @0)"));

        db.UpdateNonDefaults(new Person { FirstName = "JJ" }, x => x.LastName == "Hendrix");
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET \"FirstName\"=@FirstName WHERE (\"LastName\" = @0)"));

        db.UpdateOnly(() => new Person { FirstName = "JJ" });
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET \"FirstName\"=@FirstName"));

        db.UpdateOnlyFields(new Person { FirstName = "JJ" }, x => x.FirstName);
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET \"FirstName\"=@FirstName"));

        db.UpdateOnlyFields(new Person { FirstName = "JJ", Age = 27 }, p => new { p.FirstName, p.Age });
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET \"FirstName\"=@FirstName, \"Age\"=@Age"));

        db.UpdateOnlyFields(new Person { FirstName = "JJ", Age = 27 }, new[] { "FirstName", "Age" });
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET \"FirstName\"=@FirstName, \"Age\"=@Age"));

        db.UpdateOnlyFields(new Person { FirstName = "JJ" }, x => x.FirstName, x => x.LastName == "Hendrix");
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET \"FirstName\"=@FirstName WHERE (\"LastName\" = @0)"));

        db.UpdateOnly(() => new Person { FirstName = "JJ" }, x => x.LastName == "Hendrix");
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET \"FirstName\"=@FirstName WHERE (\"LastName\" = @0)"));

        db.UpdateOnly(() => new Person { FirstName = "JJ" }, db.From<Person>().Where(p => p.LastName == "Hendrix"));
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET \"FirstName\"=@FirstName WHERE (\"LastName\" = @0)"));

        db.UpdateOnlyFields(new Person { FirstName = "JJ" }, db.From<Person>().Update(x => x.FirstName).Where(x => x.FirstName == "Jimi"));
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET \"FirstName\"=@FirstName WHERE (\"FirstName\" = @0)"));

        db.UpdateAdd(() => new Person { Age = 3 });
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET \"Age\"=\"Age\"+@Age"));

        db.UpdateAdd(() => new Person { Age = 5 }, x => x.LastName == "Presley");
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET \"Age\"=\"Age\"+@Age WHERE (\"LastName\" = @0)"));

        db.UpdateAdd(() => new Person { Age = 5 }, db.From<Person>().Where(x => x.LastName == "Presley"));
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET \"Age\"=\"Age\"+@Age WHERE (\"LastName\" = @0)"));

        db.Delete<Person>(new { FirstName = "Jimi", Age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"Person\" WHERE \"FirstName\"=@FirstName AND \"Age\"=@Age"));

        db.Delete<Person>(new Dictionary<string,object> { ["FirstName"] = "Jimi", ["Age"] = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"Person\" WHERE \"FirstName\"=@FirstName AND \"Age\"=@Age"));

        db.Delete(new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"Person\" WHERE \"Id\"=@Id AND \"FirstName\"=@FirstName AND \"LastName\"=@LastName AND \"Age\"=@Age"));

        db.DeleteNonDefaults(new Person { FirstName = "Jimi", Age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"Person\" WHERE \"FirstName\"=@FirstName AND \"Age\"=@Age"));

        db.DeleteNonDefaults(new Person { FirstName = "Jimi", Age = 27 },
            new Person { FirstName = "Janis", Age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"Person\" WHERE \"FirstName\"=@FirstName AND \"Age\"=@Age"));

        db.DeleteById<Person>(1);
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"Person\" WHERE \"Id\" = @0"));

        db.DeleteByIds<Person>(new[] { 1, 2, 3 });
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"Person\" WHERE \"Id\" IN (@0,@1,@2)"));

        db.Delete<Person>("Age = @age", new { age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"Person\" WHERE Age = @age"));

        db.Delete(typeof(Person), "Age = @age", new { age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"Person\" WHERE Age = @age"));

        db.Delete<Person>(x => x.Age == 27);
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"Person\" WHERE (\"Age\" = @0)"));

        db.Delete(db.From<Person>().Where(x => x.Age == 27));
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM \"Person\" WHERE (\"Age\" = @0)"));

        db.Save(new Person { Id = 11, FirstName = "Amy", LastName = "Winehouse", Age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO \"Person\" (\"Id\",\"FirstName\",\"LastName\",\"Age\") VALUES (@Id,@FirstName,@LastName,@Age)"));
        db.Save(new Person { Id = 11, FirstName = "Amy", LastName = "Winehouse", Age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE \"Person\" SET \"FirstName\"=@FirstName, \"LastName\"=@LastName, \"Age\"=@Age WHERE \"Id\"=@Id"));

        db.Save(new Person { Id = 12, FirstName = "Amy", LastName = "Winehouse", Age = 27 },
            new Person { Id = 13, FirstName = "Amy", LastName = "Winehouse", Age = 27 });

        db.SaveAll(new[]{ new Person { Id = 14, FirstName = "Amy", LastName = "Winehouse", Age = 27 },
            new Person { Id = 15, FirstName = "Amy", LastName = "Winehouse", Age = 27 } });
    }
}