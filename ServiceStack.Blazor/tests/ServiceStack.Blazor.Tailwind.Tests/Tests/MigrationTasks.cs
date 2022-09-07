using NUnit.Framework;
using ServiceStack.OrmLite;
using MyApp.Migrations;

namespace MyApp.Tests;

[TestFixture, Category(nameof(MigrationTasks)), Explicit]
public class MigrationTasks
{
    OrmLiteConnectionFactory ResolveDbFactory() => new ConfigureDb().ResolveDbFactory();
    Migrator CreateMigrator() => new(ResolveDbFactory(), typeof(Migration1000).Assembly); 
    
    [Test]
    public void Migrate()
    {
        var migrator = CreateMigrator();
        var result = migrator.Run();
        Assert.That(result.Succeeded);
    }

    [Test]
    public void Revert_All()
    {
        var migrator = CreateMigrator();
        var result = migrator.Revert(Migrator.All);
        Assert.That(result.Succeeded);
    }

    [Test]
    public void Revert_Last()
    {
        var migrator = CreateMigrator();
        var result = migrator.Revert(Migrator.Last);
        Assert.That(result.Succeeded);
    }

    [Test]
    public void Rerun_Last_Migration()
    {
        Revert_Last();
        Migrate();
    }
}