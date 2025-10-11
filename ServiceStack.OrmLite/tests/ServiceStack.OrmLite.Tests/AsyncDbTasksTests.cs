using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class AsyncDbTasksTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public async Task Sequential_Async_APIs_example()
    {
        using var Db = await OpenDbConnectionAsync();
        AutoQueryTests.RecreateTables(Db);

        var rockstars = await Db.SelectAsync<Rockstar>();
        var albums = await Db.SelectAsync<RockstarAlbum>();
        var departments = await Db.SelectAsync<Department2>();
        var employees = await Db.SelectAsync<DeptEmployee>();
        
        Assert.That(rockstars.Count, Is.EqualTo(AutoQueryTests.SeedRockstars.Length));
        Assert.That(albums.Count, Is.EqualTo(AutoQueryTests.SeedAlbums.Length));
        Assert.That(departments.Count, Is.EqualTo(AutoQueryTests.SeedDepartments.Length));
        Assert.That(employees.Count, Is.EqualTo(AutoQueryTests.SeedEmployees.Length));
    }
    
    [Test]
    public async Task Can_execute_4_async_queries_in_parallel_manually()
    {
        using var Db = await OpenDbConnectionAsync();
        AutoQueryTests.RecreateTables(Db);

        var connections = await Task.WhenAll(
            DbFactory.OpenDbConnectionAsync(),
            DbFactory.OpenDbConnectionAsync(),
            DbFactory.OpenDbConnectionAsync(),
            DbFactory.OpenDbConnectionAsync()
        );

        using var dbRockstars = connections[0];
        using var dbAlbums = connections[1];
        using var dbDepartments = connections[2];
        using var dbEmployees = connections[3];
        
        var tasks = new List<Task>
        {
            dbRockstars.SelectAsync<Rockstar>(),
            dbAlbums.SelectAsync<RockstarAlbum>(),
            dbDepartments.SelectAsync<Department2>(),
            dbEmployees.SelectAsync<DeptEmployee>()
        };
        await Task.WhenAll(tasks);
        
        var rockstars = ((Task<List<Rockstar>>)tasks[0]).Result;
        var albums = ((Task<List<RockstarAlbum>>)tasks[1]).Result;
        var departments = ((Task<List<Department2>>)tasks[2]).Result;
        var employees = ((Task<List<DeptEmployee>>)tasks[3]).Result;
        
        Assert.That(rockstars.Count, Is.EqualTo(AutoQueryTests.SeedRockstars.Length));
        Assert.That(albums.Count, Is.EqualTo(AutoQueryTests.SeedAlbums.Length));
        Assert.That(departments.Count, Is.EqualTo(AutoQueryTests.SeedDepartments.Length));
        Assert.That(employees.Count, Is.EqualTo(AutoQueryTests.SeedEmployees.Length));
    }

    [Test]
    public async Task Can_execute_4_async_queries_in_parallel()
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

    [Test]
    public async Task Can_Execute_void_Async_DbTasks2()
    {
        using var Db = await OpenDbConnectionAsync();
        AutoQueryTests.RecreateTables(Db, seed: false);

        var builder = DbFactory.AsyncDbTasksBuilder()
            .Add(db => db.InsertAsync(
                AutoQueryTests.SeedRockstars[0],
                AutoQueryTests.SeedRockstars[1])
            )
            .Add(db => db.SelectAsync<Rockstar>())
            .Add(db => db.InsertAsync(
                AutoQueryTests.SeedRockstars[2],
                AutoQueryTests.SeedRockstars[3])
            )
            .Add(db => db.SelectAsync<RockstarAlbum>())
            .Add(db => db.InsertAsync([AutoQueryTests.SeedRockstars[4]]))
            .Add(db => db.SelectAsync<Department2>())
            .Add(db => db.InsertAsync([AutoQueryTests.SeedRockstars[5]]))
            .Add(db => db.SelectAsync<DeptEmployee>());

        (bool r1, 
         List<Rockstar> r2, 
         bool r3, 
         List<RockstarAlbum> r4, 
         bool r5, 
         List<Department2> r6, 
         bool r7, 
         List<DeptEmployee> r8) = await builder.RunAsync();
    }

    [Test]
    public async Task Can_Execute_void_Async_DbTasks()
    {
        using var Db = await OpenDbConnectionAsync();
        Db.DropAndCreateTable<Rockstar>();
        Db.DropAndCreateTable<RockstarAlbum>();

        var builder = DbFactory.AsyncDbTasksBuilder()
            .Add(db => db.InsertAsync(
                AutoQueryTests.SeedRockstars[0],
                AutoQueryTests.SeedRockstars[1])
            )
            .Add(db => db.InsertAsync(
                AutoQueryTests.SeedRockstars[2],
                AutoQueryTests.SeedRockstars[3])
            )
            .Add(db => db.InsertAsync([AutoQueryTests.SeedRockstars[4]]))
            .Add(db => db.InsertAsync([AutoQueryTests.SeedRockstars[5]]))
            .Add(db => db.InsertAsync([AutoQueryTests.SeedRockstars[6]]))
            .Add(db => db.InsertAsync([AutoQueryTests.SeedAlbums[0]]))
            .Add(db => db.InsertAsync([AutoQueryTests.SeedAlbums[1]]))
            .Add(db => db.InsertAsync([AutoQueryTests.SeedAlbums[2]]));

        var results = await builder.RunAsync();
        Assert.That(results, Is.EqualTo((true, true, true, true, true, true, true, true)));
        Assert.That(Db.Count<Rockstar>(), Is.EqualTo(7));
        Assert.That(Db.Count<RockstarAlbum>(), Is.EqualTo(3));
    }

    [Test]
    public async Task Only_throws_Exceptions_when_awaited()
    {
        using var Db = await OpenDbConnectionAsync();
        Db.DropAndCreateTable<Rockstar>();

        var builder = DbFactory.AsyncDbTasksBuilder()
            .Add(db => db.InsertAsync(AutoQueryTests.SeedRockstars[0]))
            .Add(db => db.InsertAsync(AutoQueryTests.SeedRockstars[0])); // <-- Duplicate PK Exception

        // Later tasks are run in parallel
        await Db.InsertAsync(AutoQueryTests.SeedRockstars[1]);

        // Tasks are run in parallel as soon as they're added
        await ExecUtils.WaitUntilTrueAsync(async () =>
            await Db.CountAsync<Rockstar>() == 2, TimeSpan.FromSeconds(1));

        // Exceptions are not thrown until the task is awaited
        var task = builder.RunAsync();

        // Duplicate key exception only thrown when awaited
        Assert.That(async () => await task, Throws.Exception);
    }
}