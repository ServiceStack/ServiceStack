using System.Linq;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.UseCase;

namespace ServiceStack.OrmLite.Tests.Migrations;

/// <summary>
/// Migrations declare their own copies of the tables they create or change. GetMigrationTables() finds the App's
/// model of each table, which is the latest version of the table, by the table name of the copies.
/// </summary>
public class MigrationTablesTests
{
    [Test]
    public void Finds_the_Apps_models_of_the_tables_migrations_create_or_change()
    {
        var assembly = typeof(Migration1000).Assembly;
        var tables = Migrator.GetMigrationTables([assembly], [assembly]).ToDictionary(x => x.Table);

        Assert.That(tables["Player"].ModelType, Is.EqualTo(typeof(Player)));
        Assert.That(tables["Level"].ModelType, Is.EqualTo(typeof(Level)));
        Assert.That(tables["GameItem"].ModelType, Is.EqualTo(typeof(GameItem)));
        // The table name of the copy is its [Alias]
        Assert.That(tables["PlayerProfile"].ModelType, Is.EqualTo(typeof(Profile)));
        Assert.That(tables["PlayerProfile"].Copies, Is.EqualTo(new[] { typeof(Migration1002.Profile) }));

        // Every migration that changes the table, including copies of only the columns they change
        Assert.That(tables["Booking"].Copies.Map(x => x.DeclaringType!.Name),
            Is.EqualTo(new[] { "Migration1000", "Migration1001", "Migration1003", "Migration1004" }));
        // More than one model of the App has the name of the table, so it isn't chosen
        Assert.That(tables["Booking"].ModelType, Is.Null);
        Assert.That(tables["Booking"].Candidates.Count, Is.EqualTo(2));

        // Classes stored in a column of a table aren't tables, e.g. Player's List<Phone>
        Assert.That(tables.ContainsKey("Phone"), Is.False);
        // Nor are the classes the compiler generates, e.g. for lambdas
        Assert.That(tables.Keys.Any(x => x.StartsWith("<")), Is.False);
        Assert.That(tables.Values.All(x => x.NamedConnection == null));
    }
}
