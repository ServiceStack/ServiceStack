using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class OrmLiteContextTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Can_set_and_get_TS_ContextItems()
    {
        OrmLiteConfig.ResultsFilter = new CaptureSqlFilter();
        OrmLiteConfig.ResultsFilter = null;
    }

    [Test]
    public void Can_override_timeout_for_specific_command()
    {
        DbFactory.AutoDisposeConnection = true; //Turn off :memory: re-use of dbConn
        OrmLiteConfig.CommandTimeout = 100;

        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<Poco>();
            db.Exec(cmd => Assert.That(cmd.CommandTimeout, Is.EqualTo(100)));

            db.SetCommandTimeout(200);

            "{0}:{1}".Print(OrmLiteContext.OrmLiteState, Thread.CurrentThread.ManagedThreadId);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                using (var dbInner = OpenDbConnection())
                {
                    "inner {0}:{1}".Print(OrmLiteContext.OrmLiteState, Thread.CurrentThread.ManagedThreadId);
                    dbInner.SetCommandTimeout(1);
                }
            });
            Thread.Sleep(10);

            "{0}:{1}".Print(OrmLiteContext.OrmLiteState, Thread.CurrentThread.ManagedThreadId);

            db.Insert(new Poco { Name = "Foo" });
            db.Exec(cmd => Assert.That(cmd.CommandTimeout, Is.EqualTo(200)));
        }

        using (var db = OpenDbConnection())
        {
            db.CreateTableIfNotExists<Poco>(); //Sqlite :memory: AutoDisposeConnection = true
            db.Select<Poco>().PrintDump();
            db.Exec(cmd => Assert.That(cmd.CommandTimeout, Is.EqualTo(100)));
        }
    }

    [Test]
    public async Task Can_override_timeout_for_specific_command_Async()
    {
        DbFactory.AutoDisposeConnection = true; //Turn off :memory: re-use of dbConn

        OrmLiteConfig.CommandTimeout = 100;

        using var db = await OpenDbConnectionAsync();
        db.DropAndCreateTable<Poco>();
        db.Exec(cmd => Assert.That(cmd.CommandTimeout, Is.EqualTo(100)));

        db.SetCommandTimeout(666);

        "{0}:{1}".Print(OrmLiteContext.OrmLiteState, Thread.CurrentThread.ManagedThreadId);

        ThreadPool.QueueUserWorkItem(_ => {
            using var dbInner = OpenDbConnection();
            "inner {0}:{1}".Print(OrmLiteContext.OrmLiteState, Thread.CurrentThread.ManagedThreadId);
            dbInner.SetCommandTimeout(1);
        });
        Thread.Sleep(10);

        "{0}:{1}".Print(OrmLiteContext.OrmLiteState, Thread.CurrentThread.ManagedThreadId);

        await db.InsertAsync(new Poco { Name = "Foo" });
        db.Exec(cmd => Assert.That(cmd.CommandTimeout, Is.EqualTo(666)));

        using var db2 = await OpenDbConnectionAsync();
        db2.CreateTableIfNotExists<Poco>(); //Sqlite :memory: AutoDisposeConnection = true
        (await db2.SelectAsync<Poco>()).PrintDump();
        db2.Exec(cmd => Assert.That(cmd.CommandTimeout, Is.EqualTo(100)));
    }

}