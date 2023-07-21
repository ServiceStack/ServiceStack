#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.SqliteTests;

[NamedConnection("db1")]
[NamedConnection("db2")]
[NamedConnection("db3")]
[Notes("Create initial Database")]
public class Migration1000 : MigrationBase
{
    public class Booking
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string? OldName { get; set; }
        public double ToDelete { get; set; }
    }
    
    public override void Up()
    {
        Db.CreateTable<Booking>();
    }

    public override void Down()
    {
        Db.DropTable<Booking>();
    }
}

[NamedConnection("db1")]
[NamedConnection("db2")]
[NamedConnection("db3")]
[Notes("Update Bookings Columns")]
public class Migration1001 : MigrationBase
{
    public class Booking
    {
        [RenameColumn(nameof(Migration1000.Booking.OldName))]
        public string? Name { get; set; }
        
        public RoomType RoomType { get; set; }
        
        [RemoveColumn]
        public double ToDelete { get; set; }
    }
    public enum RoomType {} // Values not necessary (Enum's saved as string by default)

    public override void Up() => Db.Migrate<Booking>();

    public override void Down() => Db.Revert<Booking>();
}

[TestFixture]
public class NamedMigrationTests : OrmLiteTestBase
{
    private IDbConnection Create()
    {
        File.Delete("db1.sqlite");
        File.Delete("db2.sqlite");
        File.Delete("db3.sqlite");
        
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

    public List<Type> AllMigrations = 3.Times(_ => typeof(Migration1000)).Concat(3.Times(_ => typeof(Migration1001))).ToList();

    [Test]
    public void Can_run_multiple_named_connections()
    {
        using var db = Create();
        
        var migrator = new Migrator(DbFactory, typeof(Migration1000).Assembly);
        var result = migrator.Run();
        Assert.That(result.Succeeded);
        Assert.That(result.TypesCompleted, Is.EquivalentTo(AllMigrations));

        var migrations = db.Select<Migration>();
        Assert.That(migrations.Map(x => x.NamedConnection), Is.EquivalentTo(new[]{ "db1", "db2", "db3", "db1", "db2", "db3" }));
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
        Assert.That(migrations.Map(x => x.NamedConnection), Is.EquivalentTo(new[]{ "db1", "db2", "db3", "db1", "db2", "db3" }));

        result = migrator.Revert(Migrator.All);
        Assert.That(result.Succeeded);
        Assert.That(result.TypesCompleted, Is.EquivalentTo(AllMigrations.OrderByDescending(x => x.Name)));
        
        migrations = db.Select<Migration>();
        Assert.That(migrations.Map(x => x.NamedConnection), Is.EquivalentTo(new string[]{ }));
    }
    
    
    [Test]
    public void Can_run_and_revert_last_migration()
    {
        using var db = Create();

        var migrator = new Migrator(DbFactory, typeof(Migration1000).Assembly);
        var result = migrator.Run();
        Assert.That(result.Succeeded);
        Assert.That(result.TypesCompleted, Is.EquivalentTo(AllMigrations));
        
        result = migrator.Revert(Migrator.Last);
        Assert.That(result.Succeeded);
        Assert.That(result.TypesCompleted, Is.EquivalentTo(3.Times(_ => AllMigrations.Last())));
    }
}