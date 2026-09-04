using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class TestPocoWithNoPk
{
}

public class QueryTestRockstar : QueryDb<Rockstar>
{
    public int? Id { get; set; }
}

public class QueryTestRockstarInto : QueryDb<Rockstar, RockstarDto>
{
    public int? Id { get; set; }
}

public class RockstarDto
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class DummyAutoQueryService : AutoQueryServiceBase
{
    public DummyAutoQueryService(IAutoQueryDb autoQuery) : base(autoQuery) {}
}

[TestFixture]
public class ServerAutoQueryModernizationTests
{
    private IDbConnectionFactory dbFactory;
    private AutoQueryFeature autoQueryFeature;
    private AutoQuery autoQuery;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
        using var db = dbFactory.Open();
        db.CreateTableIfNotExists<Rockstar>();
        db.DeleteAll<Rockstar>();
        db.Insert(new Rockstar { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27 });
        db.Insert(new Rockstar { Id = 2, FirstName = "Kurt", LastName = "Cobain", Age = 27 });

        autoQueryFeature = new AutoQueryFeature();
        autoQuery = autoQueryFeature.CreateAutoQueryDb(dbFactory);
    }

    [Test]
    public void AutoQueryExtensions_CreateQuery_WithNullRequest_DoesNotThrow()
    {
        var q = autoQuery.CreateQuery(new QueryTestRockstar { Id = 1 }, (IRequest)null);
        Assert.That(q, Is.Not.Null);

        var sql = q.ToSelectStatement();
        Assert.That(sql, Does.Contain("Id"));
    }

    [Test]
    public void AutoQueryExtensions_CreateQuery_WithInto_WithNullRequest_DoesNotThrow()
    {
        var q = autoQuery.CreateQuery(new QueryTestRockstarInto { Id = 1 }, (IRequest)null);
        Assert.That(q, Is.Not.Null);

        var sql = q.ToSelectStatement();
        Assert.That(sql, Does.Contain("Id"));
    }

    [Test]
    public void AutoQuery_Filter_WithNullDto_ReturnsOriginalQuery()
    {
        using var db = dbFactory.Open();
        var q = db.From<Rockstar>();
        var filtered = autoQuery.Filter<Rockstar>(q, null, null);
        Assert.That(filtered, Is.SameAs(q));

        var untypedFiltered = autoQuery.Filter(q, null, null);
        Assert.That(untypedFiltered, Is.SameAs(q));
    }

    [Test]
    public void AutoQueryServiceBase_Exec_WithNullRequest_Succeeds()
    {
        var service = new DummyAutoQueryService(autoQuery) {
            Request = null
        };

        var response = service.Exec(new QueryTestRockstar { Id = 1 }) as QueryResponse<Rockstar>;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Results.Count, Is.EqualTo(1));
        Assert.That(response.Results[0].FirstName, Is.EqualTo("Jimi"));
    }

    [Test]
    public async Task AutoQueryServiceBase_ExecAsync_WithNullRequest_Succeeds()
    {
        var service = new DummyAutoQueryService(autoQuery) {
            Request = null
        };

        var response = await service.ExecAsync(new QueryTestRockstar { Id = 1 }) as QueryResponse<Rockstar>;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Results.Count, Is.EqualTo(1));
        Assert.That(response.Results[0].FirstName, Is.EqualTo("Jimi"));
    }

    [Test]
    public void AutoQueryServiceBase_Exec_WithInto_WithNullRequest_Succeeds()
    {
        var service = new DummyAutoQueryService(autoQuery) {
            Request = null
        };

        var response = service.Exec(new QueryTestRockstarInto { Id = 1 }) as QueryResponse<RockstarDto>;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Results.Count, Is.EqualTo(1));
        Assert.That(response.Results[0].FirstName, Is.EqualTo("Jimi"));
    }

    [Test]
    public async Task AutoQueryServiceBase_ExecAsync_WithInto_WithNullRequest_Succeeds()
    {
        var service = new DummyAutoQueryService(autoQuery) {
            Request = null
        };

        var response = await service.ExecAsync(new QueryTestRockstarInto { Id = 1 }) as QueryResponse<RockstarDto>;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Results.Count, Is.EqualTo(1));
        Assert.That(response.Results[0].FirstName, Is.EqualTo("Jimi"));
    }

    [Test]
    public void GenericAutoQuery_Execute_Untyped_CachesAndResolvesCorrectly()
    {
        using var db = dbFactory.Open();
        var q1 = autoQuery.CreateQuery(new QueryTestRockstar { Id = 1 }, new Dictionary<string, string>());
        var response1 = autoQuery.Execute(new QueryTestRockstar { Id = 1 }, q1, db);
        Assert.That(response1, Is.Not.Null);

        // Second call tests cache hit using requestDtoType
        var response2 = autoQuery.Execute(new QueryTestRockstar { Id = 2 }, q1, db);
        Assert.That(response2, Is.Not.Null);

        // Different DTO querying same From but into different Into type
        var qInto = autoQuery.CreateQuery(new QueryTestRockstarInto { Id = 1 }, new Dictionary<string, string>());
        var responseInto = autoQuery.Execute(new QueryTestRockstarInto { Id = 1 }, qInto, db);
        Assert.That(responseInto, Is.Not.Null);
        Assert.That(responseInto, Is.InstanceOf<QueryResponse<RockstarDto>>());
    }

    [Test]
    public async Task GenericAutoQuery_ExecuteAsync_Untyped_CachesAndResolvesCorrectly()
    {
        using var db = dbFactory.Open();
        var q1 = autoQuery.CreateQuery(new QueryTestRockstar { Id = 1 }, new Dictionary<string, string>());
        var response1 = await autoQuery.ExecuteAsync(new QueryTestRockstar { Id = 1 }, q1, db);
        Assert.That(response1, Is.Not.Null);

        var response2 = await autoQuery.ExecuteAsync(new QueryTestRockstar { Id = 2 }, q1, db);
        Assert.That(response2, Is.Not.Null);
    }

    [Test]
    public void CrudContext_RequestIdGetter_WhenModelHasNoPrimaryKey_ReturnsNull()
    {
        var modelDef = typeof(TestPocoWithNoPk).GetModelMetadata();
        Assert.That(modelDef.FieldDefinitions.FirstOrDefault(x => x.IsPrimaryKey), Is.Null);

        var ctx = new CrudContext {
            Operation = "Create",
            ModelDef = modelDef,
            RequestType = typeof(CreateRockstar)
        };
        // RequestIdGetter checks ModelDef?.PrimaryKey != null
        Assert.That(ctx.RequestIdGetter(), Is.Null);
    }

    [Test]
    public void CrudUtils_GetTables_ForwardsArgumentsCorrectly()
    {
        var tables = dbFactory.GetTables(
            includeTables: ["Rockstar"],
            excludeTables: ["NonExistent"]);

        Assert.That(tables, Is.Not.Null);
        Assert.That(tables.Any(x => x.Name.EqualsIgnoreCase("Rockstar")), Is.True);
    }

    [Test]
    public void CrudUtils_GetTables_WithNullTableColumns_DoesNotThrowNRE()
    {
        var fakeFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
        var resolver = new FakeTableResolverWithNullColumns();
        var tables = fakeFactory.GetTables(config: resolver);

        Assert.That(tables, Is.Not.Null);
        Assert.That(tables.Count, Is.EqualTo(1));
        Assert.That(tables[0].Columns, Is.Null);
    }

    [Test]
    public void CrudEvents_ToEvent_WithNullRequestAndIpMask_DoesNotThrow()
    {
        var events = new OrmLiteCrudEvents<CrudEvent>(dbFactory) {
            IpMask = null
        };

        var ctx = new CrudContext {
            Operation = "Create",
            ModelType = typeof(Rockstar),
            Id = 123,
        };

        // Even with null Request and null IpMask, ToEvent should safely populate
        var crudEvent = events.ToEvent(ctx);
        Assert.That(crudEvent, Is.Not.Null);
        Assert.That(crudEvent.EventType, Is.EqualTo("Create"));
        Assert.That(crudEvent.Model, Is.EqualTo("Rockstar"));
        Assert.That(crudEvent.ModelId, Is.EqualTo("123"));
        Assert.That(crudEvent.RemoteIp, Is.Null);
    }

    [Test]
    public void CrudEventsUtils_ClearAndInitSchema_WithNullEvents_DoesNotThrow()
    {
        ICrudEvents nullEvents = null;
        Assert.DoesNotThrow(() => nullEvents.InitSchema());
        Assert.DoesNotThrow(() => nullEvents.Clear());
    }

    [Test]
    public async Task AutoQueryServiceBase_BatchCrud_WithNullRequests_ReturnsEmptyList()
    {
        var service = new DummyAutoQueryService(autoQuery);

        var createResult = await service.BatchCreateAsync<Rockstar>(null);
        Assert.That(createResult, Is.Not.Null);
        Assert.That(createResult, Is.InstanceOf<System.Collections.IList>());

        var updateResult = await service.BatchUpdateAsync<Rockstar>(null);
        Assert.That(updateResult, Is.Not.Null);

        var patchResult = await service.BatchPatchAsync<Rockstar>(null);
        Assert.That(patchResult, Is.Not.Null);

        var deleteResult = await service.BatchDeleteAsync<Rockstar>(null);
        Assert.That(deleteResult, Is.Not.Null);

        var saveResult = await service.BatchSaveAsync<Rockstar>(null);
        Assert.That(saveResult, Is.Not.Null);
    }

    private class FakeTableResolverWithNullColumns : ITableResolver
    {
        public GetTableNamesDelegate GetTableNames { get; set; } = (db, schema) => ["FakeTable"];
        public GetTableColumnsDelegate GetTableColumns { get; set; } = (db, table, schema) => null;
    }
}
