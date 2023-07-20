using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.SqliteTests;

[NamedConnection("db1")]
[NamedConnection("db2")]
[NamedConnection("db3")]
public class Migration1000 : MigrationBase
{
    public class Contact
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
    }
    
    public override void Up()
    {
        Db.CreateTable<Contact>();
    }

    public override void Down()
    {
        Db.DropTable<Contact>();
    }
}


[TestFixture]
public class NamedMigrationTests : OrmLiteTestBase
{
    private IDbConnection Create()
    {
        DbFactory.RegisterConnection("db1", 
            new OrmLiteConnectionFactory("db1.sqlite", SqliteDialect.Provider));
        DbFactory.RegisterConnection("db2", 
            new OrmLiteConnectionFactory("db2.sqlite", SqliteDialect.Provider));
        DbFactory.RegisterConnection("db3", 
            new OrmLiteConnectionFactory("db3.sqlite", SqliteDialect.Provider));
        
        var db = DbFactory.Open();
        Migrator.Recreate(db);
        Migrator.Clear(db);
        Migrator.Down(DbFactory, new[]{ typeof(Migration1000) });
        return db;
    }

    public List<Type> AllMigrations = 3.Times(_ => typeof(Migration1000));

    [Test]
    public void Can_run_multiple_named_connections()
    {
        using var db = Create();
        
        var migrator = new Migrator(DbFactory, typeof(Migration1000).Assembly);
        var result = migrator.Run();
        Assert.That(result.Succeeded);
        Assert.That(result.TypesCompleted, Is.EquivalentTo(AllMigrations));

        var migrations = db.Select<Migration>();
        Assert.That(migrations.Map(x => x.NamedConnection), Is.EquivalentTo(new[]{ "db1", "db2", "db3" }));
    }

    [Test]
    public void Can_revert_multiple_named_connections()
    {
        using var db = Create();
        
        var migrator = new Migrator(DbFactory, typeof(Migration1000).Assembly);
        var result = migrator.Run();
        Assert.That(result.Succeeded);
        Assert.That(result.TypesCompleted, Is.EquivalentTo(AllMigrations));

        var migrations = db.Select<Migration>();
        Assert.That(migrations.Map(x => x.NamedConnection), Is.EquivalentTo(new[]{ "db1", "db2", "db3" }));

        result = migrator.Revert(Migrator.All);
        Assert.That(result.Succeeded);
        Assert.That(result.TypesCompleted, Is.EquivalentTo(AllMigrations.OrderByDescending(x => x.Name)));
        
        migrations = db.Select<Migration>();
        Assert.That(migrations.Map(x => x.NamedConnection), Is.EquivalentTo(new string[]{ }));
    }
    
}