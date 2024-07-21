using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.OrmLite.Tests.Shared;

namespace ServiceStack.OrmLite.Tests;

public class ReplayOrmLiteExecFilter : OrmLiteExecFilter
{
    public int ReplayTimes { get; set; }

    public override T Exec<T>(IDbConnection dbConn, Func<IDbCommand, T> filter)
    {
        var dbCmd = CreateCommand(dbConn);
        try
        {
            var ret = default(T);
            for (var i = 0; i < ReplayTimes; i++)
            {
                ret = filter(dbCmd);
            }
            return ret;
        }
        finally
        {
            DisposeCommand(dbCmd, dbConn);
        }
    }
}

public class MockStoredProcExecFilter : OrmLiteExecFilter
{
    public override T Exec<T>(IDbConnection dbConn, Func<IDbCommand, T> filter)
    {
        try
        {
            return base.Exec(dbConn, filter);
        }
        catch (Exception)
        {
            var sql = dbConn.GetLastSql();
            if (sql == "exec sp_name @firstName, @age")
            {
                return (T)(object)new Person { FirstName = "Mocked" };
            }
            throw;
        }
    }

    public override async Task<T> Exec<T>(IDbConnection dbConn, Func<IDbCommand, Task<T>> filter)
    {
        try
        {
            return await base.Exec(dbConn, filter);
        }
        catch (Exception)
        {
            var sql = dbConn.GetLastSql();
            if (sql == "exec sp_name @firstName, @age")
            {
                return (T)(object)new Person { FirstName = "Mocked" };
            }
            throw;
        }
    }
}

[TestFixtureOrmLite]
public class OrmLiteExecFilterTests : OrmLiteProvidersTestBase
{
    public OrmLiteExecFilterTests(DialectContext context) : base(context) {}

    [Test]
    [IgnoreDialect(Dialect.AnyOracle, "Can't run this with Oracle until use trigger for AutoIncrement primary key insertion")]
    public void Can_add_replay_logic()
    {
        var holdExecFilter = DialectProvider.ExecFilter;
        DialectProvider.ExecFilter = new ReplayOrmLiteExecFilter { ReplayTimes = 3 };

        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<ModelWithIdAndName>();
            db.Insert(new ModelWithIdAndName { Name = "Multiplicity" });

            var rowsInserted = db.Count<ModelWithIdAndName>(q => q.Name == "Multiplicity");
            Assert.That(rowsInserted, Is.EqualTo(3));
        }

        DialectProvider.ExecFilter = holdExecFilter;
    }

    [Test]
    public void Can_mock_store_procedure()
    {
        var holdExecFilter = DialectProvider.ExecFilter;
        DialectProvider.ExecFilter = new MockStoredProcExecFilter();

        using (var db = OpenDbConnection())
        {
            var person = db.SqlScalar<Person>("exec sp_name @firstName, @age",
                new { firstName = "aName", age = 1 });

            Assert.That(person.FirstName, Is.EqualTo("Mocked"));
        }

        DialectProvider.ExecFilter = holdExecFilter;
    }

    [Test]
    public async Task Can_mock_store_procedure_Async()
    {
        var holdExecFilter = DialectProvider.ExecFilter;
        DialectProvider.ExecFilter = new MockStoredProcExecFilter();

        using (var db = OpenDbConnection())
        {
            var person = await db.SqlScalarAsync<Person>("exec sp_name @firstName, @age",
                new { firstName = "aName", age = 1 });

            Assert.That(person.FirstName, Is.EqualTo("Mocked"));
        }

        DialectProvider.ExecFilter = holdExecFilter;
    }

    [Test]
    public void Does_use_StringFilter()
    {
        OrmLiteConfig.StringFilter = s => s.TrimEnd();

        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<Poco>();

            db.Insert(new Poco { Name = "Value with trailing   " });
            var row = db.Select<Poco>().First();

            Assert.That(row.Name, Is.EqualTo("Value with trailing"));
        }

        OrmLiteConfig.StringFilter = null;
    }

    [Test]
    public void Does_use_StringFilter_on_null_strings()
    {
        OrmLiteConfig.OnDbNullFilter = fieldDef => 
            fieldDef.FieldType == typeof(string)
                ? "NULL"
                : null;

        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<ModelWithIdAndName>();

            db.Insert(new ModelWithIdAndName { Name = null });
            var row = db.Select<ModelWithIdAndName>().First();

            Assert.That(row.Name, Is.EqualTo("NULL"));
        }

        OrmLiteConfig.OnDbNullFilter = null;
    }
}