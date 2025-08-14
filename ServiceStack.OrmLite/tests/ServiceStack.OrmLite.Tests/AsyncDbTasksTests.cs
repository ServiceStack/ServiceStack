using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class AsyncDbTasksTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public async Task Can_execute_3_async_queries_in_parallel()
    {
        using var Db = await OpenDbConnectionAsync();
        AutoQueryTests.RecreateTables(Db);

        var (rockstars, albums, departments, employees) = await DbFactory.AsyncDbTasksBuilder()
            .Add(db => db.SelectAsync<Rockstar>())
            .Add(db => db.SelectAsync<RockstarAlbum>())
            .Add(db => db.SelectAsync<Department2>())
            .Add(db => db.SelectAsync<DeptEmployee>())
            .RunAsync();
        
        Assert.That(rockstars.Count, Is.EqualTo(AutoQueryTests.SeedRockstars.Length));
        Assert.That(albums.Count, Is.EqualTo(AutoQueryTests.SeedAlbums.Length));
        Assert.That(departments.Count, Is.EqualTo(AutoQueryTests.SeedDepartments.Length));
        Assert.That(employees.Count, Is.EqualTo(AutoQueryTests.SeedEmployees.Length));
    }
}
