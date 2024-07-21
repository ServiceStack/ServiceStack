using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class CaptureSqlFilterTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Can_capture_each_type_of_API()
    {
        using var captured = new CaptureSqlFilter();
        using var db = OpenDbConnection();
        db.CreateTable<Person>();
        db.Select<Person>(x => x.Age > 40);
        db.Single<Person>(x => x.Age == 42);
        db.Count<Person>(x => x.Age < 50);
        db.Insert(new Person { Id = 7, FirstName = "Amy", LastName = "Winehouse" });
        db.Update(new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix" });
        db.Delete<Person>(new { FirstName = "Jimi", Age = 27 });
        db.SqlColumn<string>("SELECT LastName FROM Person WHERE Age < @age", 
            new { age = 50 });
        db.SqlList<Person>("exec sp_name @firstName, @age", 
            new { firstName = "aName", age = 1 });
        db.ExecuteNonQuery("UPDATE Person SET LastName={0} WHERE Id={1}"
            .SqlFmt(DialectProvider, "WaterHouse", 7));

        var sql = string.Join(";\n\n", captured.SqlStatements.ToArray());
        sql.Print();
    }

    [Test]
    public void Can_capture_CreateTable_APIs()
    {
        using (var db = OpenDbConnection())
        {
            db.DropTable<Person>();
        }

        using (var captured = new CaptureSqlFilter())
        using (var db = OpenDbConnection())
        {
            int i = 0;
            i++; db.CreateTable<Person>();

            Assert.That(captured.SqlStatements.Last().NormalizeSql(),
                Does.Contain("create table person"));

            Assert.That(captured.SqlCommandHistory.Count, Is.EqualTo(i)
                .Or.EqualTo(i + 1)); //Check table if exists

            var sql = string.Join(";\n", captured.SqlStatements.ToArray());
            sql.Print();
        }
    }

    [Test]
    public void Can_capture_Select_APIs()
    {
        using var captured = new CaptureSqlFilter();
        using var db = OpenDbConnection();
        int i = 0;
        i++; db.Select<Person>(x => x.Age > 40);

        Assert.That(captured.SqlStatements.Last().NormalizeSql(),
            Is.EqualTo("select id, firstname, lastname, age  from person where (age > 40)").
                Or.EqualTo("select id, firstname, lastname, age  from person where (age > @0)"));

        i++; db.Select(db.From<Person>().Where(x => x.Age > 40));
        i++; db.Select<Person>("Age > 40");
        i++; db.Select<Person>("SELECT * FROM Person WHERE Age > 40");
        i++; db.Select<Person>("Age > @age", new { age = 40 });
        i++; db.Select<Person>("SELECT * FROM Person WHERE Age > @age", new { age = 40 });
        i++; db.Select<Person>("Age > @age", new Dictionary<string, object> { { "age", 40 } });
        i++; db.Where<Person>("Age", 27);
        i++; db.Where<Person>(new { Age = 27 });
        i++; db.SelectByIds<Person>(new[] { 1, 2, 3 });
        i++; db.SelectByIds<Person>(new[] { 1, 2, 3 });
        i++; db.SelectNonDefaults(new Person { Id = 1 });
        i++; db.SelectNonDefaults("Age > @Age", new Person { Age = 40 });
        i++; db.SelectLazy<Person>().ToList();
        i++; db.WhereLazy<Person>(new { Age = 27 }).ToList();
        i++; db.Select<Person>();
        i++; db.Single<Person>(x => x.Age == 42);
        i++; db.Single(db.From<Person>().Where(x => x.Age == 42));
        i++; db.Single<Person>(new { Age = 42 });
        i++; db.Single<Person>("Age = @age", new { age = 42 });
        i++; db.SingleById<Person>(1);
        i++; db.SingleWhere<Person>("Age", 42);
        i++; db.Exists<Person>(new { Age = 42 });
        i++; db.Exists<Person>("SELECT * FROM Person WHERE Age = @age", new { age = 42 });

        Assert.That(captured.SqlStatements.Count, Is.EqualTo(i));

        var sql = string.Join(";\n", captured.SqlStatements.ToArray());
        sql.Print();
    }

    [Test]
    public void Can_capture_all_Single_Apis()
    {
        using var captured = new CaptureSqlFilter();
        using var db = OpenDbConnection();
        int i = 0;
        i++; db.Single<Person>(x => x.Age == 42);

        var p = "@0";  //Normalized

        Assert.That(captured.SqlStatements.Last().NormalizeSql(),
            Is.EqualTo("select id, firstname, lastname, age  from person where (age = {0})".Fmt(p)). //sqlite
                Or.EqualTo("select top 1 id, firstname, lastname, age  from person where (age = {0})".Fmt(p)).   //SqlServer
                Or.EqualTo("select id, firstname, lastname, age  from person where (age = {0})".Fmt(p)).   //SqlServer 2012+
                Or.EqualTo("select id, firstname, lastname, age  from person where (age = {0})".Fmt(p)).  //Firebird
                Or.EqualTo("select * from (\r select ssormlite1.*, rownum rnum from (\r select id, firstname, lastname, age  from person where (age = {0}) order by person.id) ssormlite1\r where rownum <= 0 + 1) ssormlite2 where ssormlite2.rnum > 0".Fmt(p))  //Oracle
        );

        i++; db.Exists<Person>("Age = @age", new { age = 42 });
        i++; db.Single(db.From<Person>().Where(x => x.Age == 42));
        i++; db.Single<Person>(new { Age = 42 });
        i++; db.Single<Person>("Age = @age", new { age = 42 });
        i++; db.SingleById<Person>(1);
        i++; db.Exists<Person>("Age = @age", new { age = 42 });
        i++; db.SingleWhere<Person>("Age", 42);

        Assert.That(captured.SqlStatements.Count, Is.EqualTo(i));

        var sql = string.Join(";\n", captured.SqlStatements.ToArray());
        sql.Print();
    }

    [Test]
    public void Can_capture_all_Scalar_Apis()
    {
        using var captured = new CaptureSqlFilter();
        using var db = OpenDbConnection();
        int i = 0;
        i++; db.Scalar<Person, int>(x => Sql.Max(x.Age));

        Assert.That(captured.SqlStatements.Last().NormalizeSql(),
            Is.EqualTo("select max(age)  from person"));

        i++; db.Scalar<Person, int>(x => Sql.Max(x.Age));
        i++; db.Scalar<Person, int>(x => Sql.Max(x.Age), x => x.Age < 50);
        i++; db.Count<Person>(x => x.Age < 50);
        i++; db.Count(db.From<Person>().Where(x => x.Age < 50));
        i++; db.Scalar<int>("SELECT COUNT(*) FROM Person WHERE Age > @age", new { age = 40 });

        i++; db.SqlScalar<int>("SELECT COUNT(*) FROM Person WHERE Age < @age", new { age = 50 });
        i++; db.SqlScalar<int>("SELECT COUNT(*) FROM Person WHERE Age < @age", new Dictionary<string, object> { { "age", 50 } });

        Assert.That(captured.SqlStatements.Count, Is.EqualTo(i));

        var sql = string.Join(";\n", captured.SqlStatements.ToArray());
        sql.Print();
    }


    [Test]
    public void Can_capture_Update_Apis()
    {
        using var captured = new CaptureSqlFilter();
        using var db = OpenDbConnection();
        int i = 0;
        i++; db.Update(new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 });

        Assert.That(captured.SqlStatements.Last().NormalizeSql(),
            Does.StartWith("update person set firstname=@firstname, lastname=@lastname"));

        i++; db.Update(new[] { new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 } });
        i++; db.UpdateAll(new[] { new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 } });
        i++; db.Update(new Person { Id = 1, FirstName = "JJ", Age = 27 }, p => p.LastName == "Hendrix");
        i++; db.Update<Person>(new { FirstName = "JJ" }, p => p.LastName == "Hendrix");
        i++; db.UpdateNonDefaults(new Person { FirstName = "JJ" }, p => p.LastName == "Hendrix");
        i++; db.UpdateOnlyFields(new Person { FirstName = "JJ" }, p => p.FirstName);
        i++; db.UpdateOnlyFields(new Person { FirstName = "JJ" }, p => p.FirstName, p => p.LastName == "Hendrix");
        i++; db.UpdateOnlyFields(new Person { FirstName = "JJ", LastName = "Hendo" }, db.From<Person>().Update(p => p.FirstName));
        i++; db.UpdateOnlyFields(new Person { FirstName = "JJ" }, db.From<Person>().Update(p => p.FirstName).Where(x => x.FirstName == "Jimi"));

        Assert.That(captured.SqlStatements.Count, Is.EqualTo(i));

        var sql = string.Join(";\n", captured.SqlStatements.ToArray());
        sql.Print();
    }

    [Test]
    public void Can_capture_Delete_Apis()
    {
        using var captured = new CaptureSqlFilter();
        using var db = OpenDbConnection();
        int i = 0;
        i++; db.Delete<Person>(new { FirstName = "Jimi", Age = 27 });

        Assert.That(captured.SqlStatements.Last().NormalizeSql(),
            Is.EqualTo("delete from person where firstname=@firstname and age=@age"));

        i++; db.Delete<Person>(new { FirstName = "Jimi", Age = 27 });
        i++; db.Delete(new Person { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 });
        i++; db.DeleteNonDefaults(new Person { FirstName = "Jimi", Age = 27 });
        i++; db.DeleteById<Person>(1);
        i++; db.DeleteByIds<Person>(new[] { 1, 2, 3 });
        i++; db.Delete<Person>("Age = @age", new { age = 27 });
        i++; db.Delete(typeof(Person), "Age = @age", new { age = 27 });
        i++; db.Delete<Person>(p => p.Age == 27);
        i++; db.Delete(db.From<Person>().Where(p => p.Age == 27));

        Assert.That(captured.SqlStatements.Count, Is.EqualTo(i));

        var sql = string.Join(";\n", captured.SqlStatements.ToArray());
        sql.Print();
    }

    [Test]
    public void Can_capture_CustomSql_Apis()
    {
        using var captured = new CaptureSqlFilter();
        using var db = OpenDbConnection();
        int i = 0;
        i++; db.SqlColumn<string>("SELECT LastName FROM Person WHERE Age < @age", new { age = 50 });

        Assert.That(captured.SqlStatements.Last().NormalizeSql(),
            Is.EqualTo("select lastname from person where age < @age"));

        i++; db.SqlColumn<string>("SELECT LastName FROM Person WHERE Age < @age", new { age = 50 });
        i++; db.SqlColumn<string>("SELECT LastName FROM Person WHERE Age < @age", new Dictionary<string, object> { { "age", 50 } });
        i++; db.SqlScalar<int>("SELECT COUNT(*) FROM Person WHERE Age < @age", new { age = 50 });
        i++; db.SqlScalar<int>("SELECT COUNT(*) FROM Person WHERE Age < @age", new Dictionary<string, object> { { "age", 50 } });

        i++; db.ExecuteNonQuery("UPDATE Person SET LastName={0} WHERE Id={1}".SqlFmt(DialectProvider, "WaterHouse", 7));
        i++; db.ExecuteNonQuery("UPDATE Person SET LastName=@name WHERE Id=@id", new { name = "WaterHouse", id = 7 });

        i++; db.SqlList<Person>("exec sp_name @firstName, @age", new { firstName = "aName", age = 1 });
        i++; db.SqlScalar<Person>("exec sp_name @firstName, @age", new { firstName = "aName", age = 1 });

        Assert.That(captured.SqlStatements.Count, Is.EqualTo(i));

        var sql = string.Join(";\n", captured.SqlStatements.ToArray());
        sql.Print();
    }

    [Test]
    public void Can_capture_Insert_Apis()
    {
        using var captured = new CaptureSqlFilter();
        using var db = OpenDbConnection();
        int i = 0;
        i++; db.Insert(new Person { Id = 7, FirstName = "Amy", LastName = "Winehouse", Age = 27 });

        Assert.That(captured.SqlStatements.Last().NormalizeSql(),
            Does.Contain("insert into person (id,firstname,lastname,age) values"));

        i++; db.Insert(new Person { Id = 7, FirstName = "Amy", LastName = "Winehouse", Age = 27 });
        i++; db.InsertAll(new[] { new Person { Id = 10, FirstName = "Biggie", LastName = "Smalls", Age = 24 } });
        i++; db.InsertOnly(new PersonWithAutoId { FirstName = "Amy", Age = 27 }, p => new { p.FirstName, p.Age });

        Assert.That(captured.SqlStatements.Count, Is.EqualTo(i));

        var sql = string.Join(";\n", captured.SqlStatements.ToArray());
        sql.Print();
    }

}