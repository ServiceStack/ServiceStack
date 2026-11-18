#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    [Test]
    [NonParallelizable]
    public async Task Overlapping_migrations_capture_only_their_own_commands()
    {
        const string firstSql = "SELECT 'first migration'";
        const string secondSql = "SELECT 'second migration'";
        const string otherConnectionSql = "SELECT 'different connection in the migration context'";
        const string unrelatedSql = "SELECT 'unrelated command'";
        const string laterSql = "SELECT 'after both migrations'";
        var originalFilter = OrmLiteConfig.BeforeExecFilter;
        var observed = new ConcurrentQueue<string>();
        Action<IDbCommand> filter = cmd => observed.Enqueue(cmd.CommandText);

        OrmLiteConnectionFactory NewFactory() => new(":memory:", SqliteDialect.Provider, setGlobalDialectProvider: false);
        var timeout = TimeSpan.FromSeconds(10);
        using var firstEntered = new ManualResetEventSlim();
        using var secondEntered = new ManualResetEventSlim();
        using var finishFirst = new ManualResetEventSlim();
        using var finishSecond = new ManualResetEventSlim();
        using var unrelatedDb = NewFactory().Open();
        Task<MigrationBase>? firstRun = null;
        Task<MigrationBase>? secondRun = null;
        try
        {
            OrmLiteConfig.BeforeExecFilter = filter;
            firstRun = Task.Run(() => Migrator.Run(NewFactory(), typeof(Migration1000), migration =>
            {
                migration.Db!.ExecuteSqlAsync(firstSql).GetAwaiter().GetResult();
                using (var otherDb = NewFactory().Open())
                    otherDb.ExecuteSql(otherConnectionSql);
                firstEntered.Set();
                if (!finishFirst.Wait(timeout))
                    throw new TimeoutException("First migration was not released.");
            }));
            Assert.That(firstEntered.Wait(timeout), Is.True);

            secondRun = Task.Run(() => Migrator.Run(NewFactory(), typeof(Migration1000), migration =>
            {
                migration.Db!.ExecuteSql(secondSql);
                secondEntered.Set();
                if (!finishSecond.Wait(timeout))
                    throw new TimeoutException("Second migration was not released.");
            }));
            Assert.That(secondEntered.Wait(timeout), Is.True);

            unrelatedDb.ExecuteSql(unrelatedSql);
            finishFirst.Set();
            var first = await firstRun;
            finishSecond.Set();
            var second = await secondRun;
            unrelatedDb.ExecuteSql(laterSql);

            Assert.That(first.Error, Is.Null);
            Assert.That(second.Error, Is.Null);
            Assert.That(first.Log, Does.Contain(firstSql).And.Not.Contain(secondSql).And.Not.Contain(unrelatedSql));
            Assert.That(first.Log, Does.Not.Contain(otherConnectionSql));
            Assert.That(second.Log, Does.Contain(secondSql).And.Not.Contain(firstSql).And.Not.Contain(unrelatedSql));
            Assert.That(first.MigrationLog.ToString(), Does.Not.Contain(laterSql));
            Assert.That(OrmLiteConfig.BeforeExecFilter, Is.SameAs(filter));
            Assert.That(observed.ToArray(), Is.SupersetOf(new[] { firstSql, secondSql, otherConnectionSql, unrelatedSql, laterSql }));
        }
        finally
        {
            finishFirst.Set();
            finishSecond.Set();
            try
            {
                await Task.WhenAll(new[] { firstRun, secondRun }.OfType<Task<MigrationBase>>());
            }
            finally
            {
                OrmLiteConfig.BeforeExecFilter = originalFilter;
            }
        }
    }

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
        Migrator.Down(DbFactory, [typeof(Migration1000)]);
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