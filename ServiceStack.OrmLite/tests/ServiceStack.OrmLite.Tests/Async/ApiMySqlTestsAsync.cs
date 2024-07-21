using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;

namespace ServiceStack.OrmLite.Tests.Async;

[TestFixtureOrmLiteDialects(Dialect.AnyMySql)]
public class ApiMySqlTestsAsync(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public async Task API_MySql_Examples_Async()
    {
        var db = await OpenDbConnectionAsync();
        db.DropAndCreateTable<Person>();
        db.DropAndCreateTable<PersonWithAutoId>();

        await db.InsertAsync(Person.Rockstars);

        await db.SelectAsync<Person>(x => x.Age > 40);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` \nFROM `Person`\nWHERE (`Age` > @0)"));

        await db.SelectAsync(db.From<Person>().Where(x => x.Age > 40).OrderBy(x => x.Id));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` \nFROM `Person`\nWHERE (`Age` > @0)\nORDER BY `Id`"));

        await db.SelectAsync(db.From<Person>().Where(x => x.Age > 40));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` \nFROM `Person`\nWHERE (`Age` > @0)"));

        await db.SingleAsync<Person>(x => x.Age == 42);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` \nFROM `Person`\nWHERE (`Age` = @0)\nLIMIT 1"));

        await db.SingleAsync(db.From<Person>().Where(x => x.Age == 42));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` \nFROM `Person`\nWHERE (`Age` = @0)\nLIMIT 1"));

        await db.ScalarAsync<Person, int>(x => Sql.Max(x.Age));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Max(`Age`) \nFROM `Person`"));

        await db.ScalarAsync<Person, int>(x => Sql.Max(x.Age), x => x.Age < 50);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Max(`Age`) \nFROM `Person`\nWHERE (`Age` < @0)"));

        await db.CountAsync<Person>(x => x.Age < 50);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) \nFROM `Person`\nWHERE (`Age` < @0)"));

        await db.CountAsync(db.From<Person>().Where(x => x.Age < 50));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) \nFROM `Person`\nWHERE (`Age` < @0)"));


        await db.SelectAsync<Person>("Age > 40");
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` FROM `Person` WHERE Age > 40"));

        await db.SelectAsync<Person>("SELECT * FROM Person WHERE Age > 40");
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM Person WHERE Age > 40"));

        await db.SelectAsync<Person>("Age > @age", new { age = 40 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` FROM `Person` WHERE Age > @age"));

        await db.SelectAsync<Person>("Age > @age", new[] { db.CreateParam("age",40) });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` FROM `Person` WHERE Age > @age"));

        await db.SelectAsync<Person>("SELECT * FROM Person WHERE Age > @age", new { age = 40 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM Person WHERE Age > @age"));

        await db.SelectAsync<Person>("Age > @age", new Dictionary<string, object> { { "age", 40 } });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` FROM `Person` WHERE Age > @age"));

        await db.SelectAsync<Person>("SELECT * FROM Person WHERE Age > @age", new Dictionary<string, object> { { "age", 40 } });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM Person WHERE Age > @age"));

        await db.SelectAsync<EntityWithId>(typeof(Person));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id` FROM `Person`"));

        await db.SelectAsync<EntityWithId>(typeof(Person), "Age > @age", new { age = 40 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id` FROM `Person` WHERE Age > @age"));

        await db.WhereAsync<Person>("Age", 27);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` FROM `Person` WHERE `Age` = @Age"));

        await db.WhereAsync<Person>(new { Age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` FROM `Person` WHERE `Age` = @Age"));

        await db.SelectByIdsAsync<Person>(new[] { 1, 2, 3 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` FROM `Person` WHERE `Id` IN (@0,@1,@2)"));

        await db.SelectNonDefaultsAsync(new Person { Id = 1 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` FROM `Person` WHERE `Id` = @Id"));

        await db.SelectNonDefaultsAsync("Age > @Age", new Person { Age = 40 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` FROM `Person` WHERE Age > @Age"));

        await db.SingleByIdAsync<Person>(1);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` FROM `Person` WHERE `Id` = @Id"));

        await db.SingleAsync<Person>(new { Age = 42 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` FROM `Person` WHERE `Age` = @Age"));

        await db.SingleAsync<Person>("Age = @age", new { age = 42 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` FROM `Person` WHERE Age = @age"));

        await db.SingleAsync<Person>("Age = @age", new[] { db.CreateParam("age",42) });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` FROM `Person` WHERE Age = @age"));

        await db.SingleByIdAsync<Person>(1);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` FROM `Person` WHERE `Id` = @Id"));

        await db.SingleWhereAsync<Person>("Age", 42);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` FROM `Person` WHERE `Age` = @Age"));

        await db.ScalarAsync<int>(db.From<Person>().Select(Sql.Count("*")).Where(q => q.Age > 40));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) \nFROM `Person`\nWHERE (`Age` > @0)"));
        await db.ScalarAsync<int>(db.From<Person>().Select(x => Sql.Count("*")).Where(q => q.Age > 40));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Count(*) \nFROM `Person`\nWHERE (`Age` > @0)"));

        await db.ScalarAsync<int>("SELECT COUNT(*) FROM Person WHERE Age > @age", new { age = 40 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) FROM Person WHERE Age > @age"));

        await db.ScalarAsync<int>("SELECT COUNT(*) FROM Person WHERE Age > @age", new[] { db.CreateParam("age",40) });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) FROM Person WHERE Age > @age"));

        await db.ColumnAsync<string>(db.From<Person>().Select(x => x.LastName).Where(q => q.Age == 27));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `LastName` \nFROM `Person`\nWHERE (`Age` = @0)"));

        await db.ColumnAsync<string>("SELECT LastName FROM Person WHERE Age = @age", new { age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT LastName FROM Person WHERE Age = @age"));

        await db.ColumnAsync<string>("SELECT LastName FROM Person WHERE Age = @age", new[] { db.CreateParam("age",27) });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT LastName FROM Person WHERE Age = @age"));

        await db.ColumnDistinctAsync<int>(db.From<Person>().Select(x => x.Age).Where(q => q.Age < 50));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Age` \nFROM `Person`\nWHERE (`Age` < @0)"));

        await db.ColumnDistinctAsync<int>("SELECT Age FROM Person WHERE Age < @age", new { age = 50 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Age FROM Person WHERE Age < @age"));

        await db.ColumnDistinctAsync<int>("SELECT Age FROM Person WHERE Age < @age", new[] { db.CreateParam("age",50) });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Age FROM Person WHERE Age < @age"));

        await db.LookupAsync<int, string>(db.From<Person>().Select(x => new { x.Age, x.LastName }).Where(q => q.Age < 50));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Age`, `LastName` \nFROM `Person`\nWHERE (`Age` < @0)"));

        await db.LookupAsync<int, string>("SELECT Age, LastName FROM Person WHERE Age < @age", new { age = 50 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Age, LastName FROM Person WHERE Age < @age"));

        await db.LookupAsync<int, string>("SELECT Age, LastName FROM Person WHERE Age < @age", new[] { db.CreateParam("age",50) });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Age, LastName FROM Person WHERE Age < @age"));

        await db.DictionaryAsync<int, string>(db.From<Person>().Select(x => new { x.Id, x.LastName }).Where(x => x.Age < 50));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `LastName` \nFROM `Person`\nWHERE (`Age` < @0)"));

        await db.DictionaryAsync<int, string>("SELECT Id, LastName FROM Person WHERE Age < @age", new { age = 50 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Id, LastName FROM Person WHERE Age < @age"));

        await db.DictionaryAsync<int, string>("SELECT Id, LastName FROM Person WHERE Age < @age", new[] { db.CreateParam("age",50) });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT Id, LastName FROM Person WHERE Age < @age"));

        await db.ExistsAsync<Person>(x => x.Age < 50);
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT 'exists' \nFROM `Person`\nWHERE (`Age` < @0)\nLIMIT 1"));

        await db.ExistsAsync(db.From<Person>().Where(x => x.Age < 50));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT 'exists' \nFROM `Person`\nWHERE (`Age` < @0)\nLIMIT 1"));

        await db.ExistsAsync<Person>(new { Age = 42 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` FROM `Person` WHERE `Age` = @Age"));

        await db.ExistsAsync<Person>("Age = @age", new { age = 42 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `Id`, `FirstName`, `LastName`, `Age` FROM `Person` WHERE Age = @age"));
        await db.ExistsAsync<Person>("SELECT * FROM Person WHERE Age = @age", new { age = 42 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM Person WHERE Age = @age"));

        await db.SqlListAsync<Person>(db.From<Person>().Select("*").Where(q => q.Age < 50));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * \nFROM `Person`\nWHERE (`Age` < @0)"));

        await db.SqlListAsync<Person>("SELECT * FROM Person WHERE Age < @age", new { age = 50 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM Person WHERE Age < @age"));

        await db.SqlListAsync<Person>("SELECT * FROM Person WHERE Age < @age", new[] { db.CreateParam("age",50) });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM Person WHERE Age < @age"));

        await db.SqlListAsync<Person>("SELECT * FROM Person WHERE Age < @age", new Dictionary<string, object> { { "age", 50 } });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT * FROM Person WHERE Age < @age"));

        await db.SqlColumnAsync<string>(db.From<Person>().Select(x => x.LastName).Where(q => q.Age < 50));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT `LastName` \nFROM `Person`\nWHERE (`Age` < @0)"));

        await db.SqlColumnAsync<string>("SELECT LastName FROM Person WHERE Age < @age", new { age = 50 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT LastName FROM Person WHERE Age < @age"));

        await db.SqlColumnAsync<string>("SELECT LastName FROM Person WHERE Age < @age", new[] { db.CreateParam("age",50) });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT LastName FROM Person WHERE Age < @age"));

        await db.SqlColumnAsync<string>("SELECT LastName FROM Person WHERE Age < @age", new Dictionary<string, object> { { "age", 50 } });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT LastName FROM Person WHERE Age < @age"));

        await db.SqlScalarAsync<int>(db.From<Person>().Select(Sql.Count("*")).Where(q => q.Age < 50));
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) \nFROM `Person`\nWHERE (`Age` < @0)"));

        await db.SqlScalarAsync<int>("SELECT COUNT(*) FROM Person WHERE Age < @age", new { age = 50 });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) FROM Person WHERE Age < @age"));

        await db.SqlScalarAsync<int>("SELECT COUNT(*) FROM Person WHERE Age < @age", new[] { db.CreateParam("age",50) });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) FROM Person WHERE Age < @age"));

        await db.SqlScalarAsync<int>("SELECT COUNT(*) FROM Person WHERE Age < @age", new Dictionary<string, object> { { "age", 50 } });
        Assert.That(db.GetLastSql(), Is.EqualTo("SELECT COUNT(*) FROM Person WHERE Age < @age"));

        var rowsAffected = await db.ExecuteNonQueryAsync("UPDATE Person SET LastName=@name WHERE Id=@id", new { name = "WaterHouse", id = 7 });
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE Person SET LastName=@name WHERE Id=@id"));

        await db.InsertAsync(new Person { Id = 7, FirstName = "Amy", LastName = "Winehouse", Age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO `Person` (`Id`,`FirstName`,`LastName`,`Age`) VALUES (@Id,@FirstName,@LastName,@Age)"));

        await db.InsertAsync(new Person { Id = 8, FirstName = "Tupac", LastName = "Shakur", Age = 25 },
            new Person { Id = 9, FirstName = "Tupac", LastName = "Shakur2", Age = 26 });

        Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO `Person` (`Id`,`FirstName`,`LastName`,`Age`) VALUES (@Id,@FirstName,@LastName,@Age)"));


        await db.InsertAllAsync(new[] { new Person { Id = 10, FirstName = "Biggie", LastName = "Smalls", Age = 24 } });
        Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO `Person` (`Id`,`FirstName`,`LastName`,`Age`) VALUES (@Id,@FirstName,@LastName,@Age)"));

        await db.InsertOnlyAsync(new PersonWithAutoId { FirstName = "Amy", Age = 27 }, p => new { p.FirstName, p.Age });
        Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO `PersonWithAutoId` (`FirstName`,`Age`) VALUES (@FirstName,@Age)"));

        await db.InsertOnlyAsync(() => new PersonWithAutoId { FirstName = "Amy", Age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO `PersonWithAutoId` (`FirstName`,`Age`) VALUES (@FirstName,@Age)"));

        await db.UpdateAsync(new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE `Person` SET `FirstName`=@FirstName, `LastName`=@LastName, `Age`=@Age WHERE `Id`=@Id"));

        await db.UpdateAsync(new Person { Id = 8, FirstName = "Tupac", LastName = "Shakur3", Age = 27 },
            new Person { Id = 9, FirstName = "Tupac", LastName = "Shakur4", Age = 28 });

        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE `Person` SET `FirstName`=@FirstName, `LastName`=@LastName, `Age`=@Age WHERE `Id`=@Id"));

        await db.UpdateAsync(new[] { new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 } });
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE `Person` SET `FirstName`=@FirstName, `LastName`=@LastName, `Age`=@Age WHERE `Id`=@Id"));

        await db.UpdateAllAsync(new[] { new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 } });
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE `Person` SET `FirstName`=@FirstName, `LastName`=@LastName, `Age`=@Age WHERE `Id`=@Id"));

        await db.UpdateAsync(new Person { Id = 1, FirstName = "JJ", Age = 27 }, p => p.LastName == "Hendrix");
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE `Person` SET `Id`=@Id, `FirstName`=@FirstName, `LastName`=@LastName, `Age`=@Age WHERE (`LastName` = @0)"));

        await db.UpdateAsync<Person>(new { FirstName = "JJ" }, p => p.LastName == "Hendrix");
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE `Person` SET `FirstName`=@FirstName WHERE (`LastName` = @0)"));

        await db.UpdateNonDefaultsAsync(new Person { FirstName = "JJ" }, p => p.LastName == "Hendrix");
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE `Person` SET `FirstName`=@FirstName WHERE (`LastName` = @0)"));

        await db.UpdateOnlyFieldsAsync(new Person { FirstName = "JJ" }, p => p.FirstName);
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE `Person` SET `FirstName`=@FirstName"));

        await db.UpdateOnlyAsync(() => new Person { FirstName = "JJ" });
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE `Person` SET `FirstName`=@FirstName"));

        await db.UpdateOnlyFieldsAsync(new Person { FirstName = "JJ" }, p => p.FirstName, p => p.LastName == "Hendrix");
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE `Person` SET `FirstName`=@FirstName WHERE (`LastName` = @0)"));

        await db.UpdateOnlyAsync(() => new Person { FirstName = "JJ" }, p => p.LastName == "Hendrix");
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE `Person` SET `FirstName`=@FirstName WHERE (`LastName` = @0)"));

        await db.UpdateOnlyFieldsAsync(new Person { FirstName = "JJ", LastName = "Hendo" }, db.From<Person>().Update(p => p.FirstName));
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE `Person` SET `FirstName`=@FirstName"));

        await db.UpdateOnlyFieldsAsync(new Person { FirstName = "JJ" }, db.From<Person>().Update(p => p.FirstName).Where(x => x.FirstName == "Jimi"));
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE `Person` SET `FirstName`=@FirstName WHERE (`FirstName` = @0)"));

        await db.UpdateAddAsync(() => new Person { Age = 3 });
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE `Person` SET `Age`=`Age`+@Age"));

        await db.UpdateAddAsync(() => new Person { Age = 5 }, where: x => x.LastName == "Presley");
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE `Person` SET `Age`=`Age`+@Age WHERE (`LastName` = @0)"));

        await db.DeleteAsync<Person>(new { FirstName = "Jimi", Age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM `Person` WHERE `FirstName`=@FirstName AND `Age`=@Age"));

        await db.DeleteAsync(new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM `Person` WHERE `Id`=@Id AND `FirstName`=@FirstName AND `LastName`=@LastName AND `Age`=@Age"));

        await db.DeleteNonDefaultsAsync(new Person { FirstName = "Jimi", Age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM `Person` WHERE `FirstName`=@FirstName AND `Age`=@Age"));

        await db.DeleteNonDefaultsAsync(new Person { FirstName = "Jimi", Age = 27 },
            new Person { FirstName = "Janis", Age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM `Person` WHERE `FirstName`=@FirstName AND `Age`=@Age"));

        await db.DeleteByIdAsync<Person>(1);
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM `Person` WHERE `Id` = @0"));

        await db.DeleteByIdsAsync<Person>(new[] { 1, 2, 3 });
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM `Person` WHERE `Id` IN (@0,@1,@2)"));

        await db.DeleteAsync<Person>("Age = @age", new { age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM `Person` WHERE Age = @age"));

        await db.DeleteAsync(typeof(Person), "Age = @age", new { age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM `Person` WHERE Age = @age"));

        await db.DeleteAsync<Person>(p => p.Age == 27);
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM `Person` WHERE (`Age` = @0)"));

        await db.DeleteAsync(db.From<Person>().Where(p => p.Age == 27));
        Assert.That(db.GetLastSql(), Is.EqualTo("DELETE FROM `Person` WHERE (`Age` = @0)"));

        await db.SaveAsync(new Person { Id = 11, FirstName = "Amy", LastName = "Winehouse", Age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("INSERT INTO `Person` (`Id`,`FirstName`,`LastName`,`Age`) VALUES (@Id,@FirstName,@LastName,@Age)"));
        await db.SaveAsync(new Person { Id = 11, FirstName = "Amy", LastName = "Winehouse", Age = 27 });
        Assert.That(db.GetLastSql(), Is.EqualTo("UPDATE `Person` SET `FirstName`=@FirstName, `LastName`=@LastName, `Age`=@Age WHERE `Id`=@Id"));

        await db.SaveAsync(new Person { Id = 12, FirstName = "Amy", LastName = "Winehouse", Age = 27 },
            new Person { Id = 13, FirstName = "Amy", LastName = "Winehouse", Age = 27 });

        await db.SaveAllAsync(new[]{ new Person { Id = 14, FirstName = "Amy", LastName = "Winehouse", Age = 27 },
            new Person { Id = 15, FirstName = "Amy", LastName = "Winehouse", Age = 27 } });

        db.Dispose();
    }
}