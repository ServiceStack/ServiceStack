using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// Connections opened with OpenReadOnlyDbConnection() can't write, even when they're opened on the primary, e.g. in
/// development without a read replica, so a write fails the same way it would on a replica. The database makes their
/// session read-only, or OrmLite rejects their writes on databases that don't have read-only sessions, e.g. SQL Server.
/// </summary>
[TestFixtureOrmLite]
public class ReadOnlyConnectionUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public class ReadOnlyItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [SetUp]
    public void SetUp()
    {
        if (!DbFactory.AutoDisposeConnection)
            Assert.Ignore("Shared connections, e.g. SQLite :memory:, are the same connection for every open");
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ReadOnlyItem>();
        db.Insert(new ReadOnlyItem { Id = 1, Name = "A" });
    }

    [Test]
    public void Read_only_connections_on_the_primary_reject_writes()
    {
        using (var db = DbFactory.OpenReadOnlyDbConnection())
        {
            var conn = (OrmLiteConnection)db;
            Assert.That(conn.IsReadOnly, Is.True);
            Assert.That(conn.IsReadReplica, Is.False);

            Assert.That(db.SingleById<ReadOnlyItem>(1).Name, Is.EqualTo("A"));
            Assert.That(db.Count<ReadOnlyItem>(), Is.EqualTo(1));
            Assert.Catch(() => db.Insert(new ReadOnlyItem { Id = 2, Name = "B" }));
            Assert.Catch(() => db.UpdateOnly(() => new ReadOnlyItem { Name = "B" }, x => x.Id == 1));
            Assert.Catch(() => db.DeleteById<ReadOnlyItem>(1));
            Assert.That(conn.HasWrites, Is.False);
        }

        // The session of the pooled connection is read-write again
        using (var db = OpenDbConnection())
        {
            db.Insert(new ReadOnlyItem { Id = 2, Name = "B" });
            Assert.That(((OrmLiteConnection)db).HasWrites, Is.True);
            Assert.That(db.Count<ReadOnlyItem>(), Is.EqualTo(2));
        }
    }

    [Test]
    public async Task Async_read_only_connections_on_the_primary_reject_writes()
    {
        using (var db = await DbFactory.OpenReadOnlyDbConnectionAsync())
        {
            Assert.That((await db.SingleByIdAsync<ReadOnlyItem>(1)).Name, Is.EqualTo("A"));
            Assert.CatchAsync(async () => await db.InsertAsync(new ReadOnlyItem { Id = 2, Name = "B" }));
            Assert.CatchAsync(async () => await db.InsertAllAsync(new[] {
                new ReadOnlyItem { Id = 3, Name = "C" }, new ReadOnlyItem { Id = 4, Name = "D" },
            }));
        }
        using (var db = await OpenDbConnectionAsync())
        {
            await db.InsertAsync(new ReadOnlyItem { Id = 2, Name = "B" });
            Assert.That(await db.CountAsync<ReadOnlyItem>(), Is.EqualTo(2));
        }
    }

    [Test]
    public void Connections_record_when_they_write()
    {
        using var db = OpenDbConnection();
        var conn = (OrmLiteConnection)db;
        db.Select<ReadOnlyItem>();
        db.SingleById<ReadOnlyItem>(1);
        Assert.That(conn.HasWrites, Is.False);

        // Including from the connections returned by WithoutFilters(), which are the same connection
        db.WithoutFilters().UpdateOnly(() => new ReadOnlyItem { Name = "B" }, x => x.Id == 1);
        Assert.That(conn.HasWrites, Is.True);
    }
}
