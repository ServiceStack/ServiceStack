using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.MySql;
using ServiceStack.OrmLite.MySql.Tests;

namespace ServiceStack.OrmLite.Tests;

[TestFixture]
public class SqlMapperTests
{
    [Alias("Users")]
    public class User 
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }

    OrmLiteConnectionFactory dbFactory;

    [SetUp]
    public void SetUp()
    {
        //Setup SQL Server Connection Factory
        dbFactory = new OrmLiteConnectionFactory(MySqlConfig.ConnectionString, MySqlConfig.DialectProvider);

        using (var db = dbFactory.Open())
            db.DropAndCreateTable<User>();
    }
        
    [Test]
    public void BuilderSelectClause()
    {
        using var db = dbFactory.OpenDbConnection();
        var rand = new Random(8675309);
        var data = new List<User>();
        for (var i = 0; i < 100; i++)
        {
            var nU = new User { Age = rand.Next(70), Id = i, Name = Guid.NewGuid().ToString() };
            data.Add(nU);
            db.Insert(nU);
            nU.Id = (int) db.LastInsertId();
        }

        var builder = new SqlBuilder();
        var justId = builder.AddTemplate("SELECT /**select**/ FROM Users");
        var all = builder.AddTemplate("SELECT /**select**/, Name, Age FROM Users");

        builder.Select("Id");

        var ids = db.Select<int>(justId.RawSql, justId.Parameters);
        var users = db.Select<User>(all.RawSql, all.Parameters);

        foreach (var u in data)
        {
            Assert.That(ids.Any(i => u.Id == i), "Missing ids in select");
            Assert.That(users.Any(a => a.Id == u.Id && a.Name == u.Name && a.Age == u.Age), "Missing users in select");
        }
    }

    [Test]
    public void BuilderTemplateWOComposition()
    {
        var builder = new SqlBuilder();
        var template = builder.AddTemplate("SELECT COUNT(*) FROM Users WHERE Age = @age", new { age = 5 });

        if (template.RawSql == null) throw new Exception("RawSql null");
        if (template.Parameters == null) throw new Exception("Parameters null");

        using var db = dbFactory.OpenDbConnection();
        db.Insert(new User { Age = 5, Name = "Testy McTestington" });

        Assert.That(db.Scalar<int>(template.RawSql, template.Parameters), Is.EqualTo(1));
    }         
}