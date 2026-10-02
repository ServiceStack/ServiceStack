using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// InsertAll(), UpdateAll(), UpsertAll() and SaveAll() send their statements together with an ADO.NET DbBatch when
/// the driver supports it (Npgsql, Microsoft.Data.SqlClient and MySqlConnector), instead of a round trip for each
/// row. They're used the same and return the same results on every driver.
/// </summary>
[TestFixtureOrmLite]
public class BatchWriteUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public class Sku
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }

    public class Member
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public int Points { get; set; }
    }

    public class Ticket
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public ulong RowVersion { get; set; }
    }

    static List<Sku> Skus(int from, int count) => Enumerable.Range(from, count)
        .Map(i => new Sku { Id = i, Name = "Sku " + i, Price = i * 1.5m, Stock = i });

    // Whether the driver of the connection can send statements together
    static bool CanBatch(IDbConnection db) => db.ToDbConnection() is DbConnection { CanCreateBatch: true };

    // The number of batches that are sent while running the action
    static int BatchesSent(Action action)
    {
        var hold = LogManager.LogFactory;
        var logs = new StringBuilderLogFactory();
        OrmLiteConfig.ResetLogFactory(logs);
        try
        {
            action();
        }
        finally
        {
            OrmLiteConfig.ResetLogFactory(hold);
        }
        return logs.GetLogs().Split('\n').Count(x => x.Contains("SQL BATCH"));
    }

    [Test]
    public void InsertAll_sends_its_rows_together()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Sku>();

        var batches = BatchesSent(() => db.InsertAll(Skus(1, 5)));

        Assert.That(db.Select<Sku>().Map(x => x.Id), Is.EquivalentTo(new[] { 1, 2, 3, 4, 5 }));
        Assert.That(db.SingleById<Sku>(3).Price, Is.EqualTo(4.5m));
        Assert.That(batches, Is.EqualTo(CanBatch(db) ? 1 : 0));
    }

    [Test]
    public void Rows_are_sent_in_batches_of_the_dialects_BatchSize()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Sku>();

        var dialect = db.GetDialectProvider();
        var hold = dialect.BatchSize;
        dialect.BatchSize = 2;
        try
        {
            var batches = BatchesSent(() => db.InsertAll(Skus(1, 5)));

            Assert.That(db.Count<Sku>(), Is.EqualTo(5));
            Assert.That(batches, Is.EqualTo(CanBatch(db) ? 3 : 0));
        }
        finally
        {
            dialect.BatchSize = hold;
        }
    }

    [Test]
    public void Rows_are_sent_one_at_a_time_without_UseDbBatch()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Sku>();

        var dialect = db.GetDialectProvider();
        dialect.UseDbBatch = false;
        try
        {
            var batches = BatchesSent(() => {
                db.InsertAll(Skus(1, 5));
                db.UpdateAll(Skus(1, 5).Map(x => { x.Stock = 100; return x; }));
            });

            Assert.That(db.Select<Sku>().Map(x => x.Stock), Is.All.EqualTo(100));
            Assert.That(batches, Is.EqualTo(0));
        }
        finally
        {
            dialect.UseDbBatch = true;
        }
    }

    [Test]
    public void UpdateAll_returns_the_rows_it_updated()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Sku>();
        db.InsertAll(Skus(1, 5));

        var rows = Skus(1, 3).Map(x => { x.Name = "Updated " + x.Id; x.Stock = 0; return x; });
        rows.Add(new Sku { Id = 99, Name = "Missing" });

        var updated = 0;
        var batches = BatchesSent(() => updated = db.UpdateAll(rows));

        Assert.That(updated, Is.EqualTo(3));
        Assert.That(db.Select<Sku>(x => x.Stock == 0).Map(x => x.Name), Is.EquivalentTo(new[] {
            "Updated 1", "Updated 2", "Updated 3" }));
        Assert.That(db.SingleById<Sku>(4).Name, Is.EqualTo("Sku 4"));
        Assert.That(batches, Is.EqualTo(CanBatch(db) ? 1 : 0));
    }

    [Test]
    public void UpdateAll_fails_when_a_row_version_is_stale()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Ticket>();
        db.InsertAll(new[] {
            new Ticket { Id = 1, Title = "One" },
            new Ticket { Id = 2, Title = "Two" },
            new Ticket { Id = 3, Title = "Three" },
        });

        var tickets = db.Select<Ticket>().OrderBy(x => x.Id).ToList();
        db.UpdateOnly(() => new Ticket { Title = "Changed" }, where: x => x.Id == 2); // by someone else

        tickets.Each(x => x.Title += "!");
        Assert.Throws<OptimisticConcurrencyException>(() => db.UpdateAll(tickets));

        // None of the rows are updated
        Assert.That(db.Select<Ticket>().OrderBy(x => x.Id).Map(x => x.Title), Is.EqualTo(new[] {
            "One", "Changed", "Three" }));

        // Rows with a current version are
        tickets = db.Select<Ticket>().OrderBy(x => x.Id).ToList();
        tickets.Each(x => x.Title += "!");
        Assert.That(db.UpdateAll(tickets), Is.EqualTo(3));
        Assert.That(db.Select<Ticket>().OrderBy(x => x.Id).Map(x => x.Title), Is.EqualTo(new[] {
            "One!", "Changed!", "Three!" }));
    }

    [Test]
    public void InsertAll_inserts_all_rows_or_none()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Sku>();
        db.Insert(new Sku { Id = 3, Name = "Existing" });

        // The 3rd row has an existing primary key
        Assert.That(() => db.InsertAll(Skus(1, 5)), Throws.Exception);

        Assert.That(db.Select<Sku>().Map(x => x.Name), Is.EqualTo(new[] { "Existing" }));
    }

    [Test]
    public async Task InsertAllAsync_inserts_all_rows_or_none()
    {
        using var db = await OpenDbConnectionAsync();
        db.DropAndCreateTable<Sku>();
        db.Insert(new Sku { Id = 3, Name = "Existing" });

        Assert.That(async () => await db.InsertAllAsync(Skus(1, 5)), Throws.Exception);

        Assert.That(db.Select<Sku>().Map(x => x.Name), Is.EqualTo(new[] { "Existing" }));
    }

    [Test]
    public void Rows_are_written_in_the_transaction_of_the_connection()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Sku>();
        db.InsertAll(Skus(1, 2));

        using (var trans = db.OpenTransaction())
        {
            db.InsertAll(Skus(3, 3));
            db.UpdateAll(Skus(1, 2).Map(x => { x.Stock = 100; return x; }));
            Assert.That(db.Count<Sku>(), Is.EqualTo(5));
            trans.Rollback();
        }

        Assert.That(db.Select<Sku>().OrderBy(x => x.Id).Map(x => x.Stock), Is.EqualTo(new[] { 1, 2 }));

        using (var trans = db.OpenTransaction())
        {
            db.InsertAll(Skus(3, 3));
            trans.Commit();
        }
        Assert.That(db.Count<Sku>(), Is.EqualTo(5));
    }

    [Test]
    public void UpsertAll_inserts_new_rows_and_updates_existing_rows()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Sku>();
        db.InsertAll(Skus(1, 3));

        var rows = Skus(2, 4).Map(x => { x.Name = "Upserted " + x.Id; x.Stock = 50; return x; });
        var batches = BatchesSent(() => db.UpsertAll(rows));

        Assert.That(db.Select<Sku>().OrderBy(x => x.Id).Map(x => x.Name), Is.EqualTo(new[] {
            "Sku 1", "Upserted 2", "Upserted 3", "Upserted 4", "Upserted 5" }));
        Assert.That(batches, Is.EqualTo(CanBatch(db) ? 1 : 0));

        // Only update the stock of existing rows
        db.UpsertAll(Skus(5, 2).Map(x => { x.Name = "Ignored"; x.Stock = 7; return x; }), updateOnly: [nameof(Sku.Stock)]);

        var sku5 = db.SingleById<Sku>(5);
        Assert.That(sku5.Name, Is.EqualTo("Upserted 5"));
        Assert.That(sku5.Stock, Is.EqualTo(7));
        Assert.That(db.SingleById<Sku>(6).Name, Is.EqualTo("Ignored"));
    }

    [Test]
    public void UpsertAll_assigns_the_ids_of_new_rows()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Member>();
        db.InsertAll(new[] { new Member { Name = "Ann", Points = 1 }, new Member { Name = "Bob", Points = 2 } });
        var existing = db.Select<Member>().OrderBy(x => x.Id).ToList();

        // Existing rows have their id, new rows don't
        var rows = new List<Member> {
            new() { Id = existing[0].Id, Name = "Ann", Points = 10 },
            new() { Name = "Cat", Points = 3 },
            new() { Id = existing[1].Id, Name = "Bob", Points = 20 },
            new() { Name = "Dan", Points = 4 },
        };
        db.UpsertAll(rows);

        Assert.That(rows.Map(x => x.Id), Is.All.GreaterThan(0));
        Assert.That(rows.Map(x => x.Id).Distinct().Count(), Is.EqualTo(4));
        var members = db.Select<Member>().OrderBy(x => x.Name).ToList();
        Assert.That(members.Map(x => x.Name), Is.EqualTo(new[] { "Ann", "Bob", "Cat", "Dan" }));
        Assert.That(members.Map(x => x.Points), Is.EqualTo(new[] { 10, 20, 3, 4 }));
        Assert.That(db.SingleById<Member>(rows[3].Id).Name, Is.EqualTo("Dan"));
    }

    [Test]
    public void SaveAll_inserts_new_rows_and_updates_existing_rows()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Sku>();
        db.InsertAll(Skus(1, 3));

        var rows = Skus(2, 4).Map(x => { x.Name = "Saved " + x.Id; return x; });
        var added = 0;
        var batches = BatchesSent(() => added = db.SaveAll(rows));

        Assert.That(added, Is.EqualTo(2));
        Assert.That(db.Select<Sku>().OrderBy(x => x.Id).Map(x => x.Name), Is.EqualTo(new[] {
            "Sku 1", "Saved 2", "Saved 3", "Saved 4", "Saved 5" }));
        Assert.That(batches, Is.EqualTo(CanBatch(db) ? 1 : 0));
    }

    [Test]
    public void SaveAll_assigns_the_ids_of_new_rows()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Member>();
        db.InsertAll(new[] { new Member { Name = "Ann", Points = 1 }, new Member { Name = "Bob", Points = 2 } });
        var existing = db.Select<Member>().OrderBy(x => x.Id).ToList();

        var rows = new List<Member> {
            new() { Id = existing[0].Id, Name = "Ann", Points = 10 },
            new() { Name = "Cat", Points = 3 },
            new() { Id = existing[1].Id, Name = "Bob", Points = 20 },
            new() { Name = "Dan", Points = 4 },
        };
        Assert.That(db.SaveAll(rows), Is.EqualTo(2));

        Assert.That(rows.Map(x => x.Id).Distinct().Count(), Is.EqualTo(4));
        var members = db.Select<Member>().OrderBy(x => x.Name).ToList();
        Assert.That(members.Map(x => x.Points), Is.EqualTo(new[] { 10, 20, 3, 4 }));
        Assert.That(db.SingleById<Member>(rows[1].Id).Name, Is.EqualTo("Cat"));
    }

    [Test]
    public void SaveAll_reads_back_row_versions()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Ticket>();

        var tickets = new List<Ticket> { new() { Id = 1, Title = "One" }, new() { Id = 2, Title = "Two" } };
        db.SaveAll(tickets);
        Assert.That(tickets.Map(x => x.RowVersion), Is.EqualTo(db.Select<Ticket>().OrderBy(x => x.Id).Map(x => x.RowVersion)));

        tickets.Each(x => x.Title += "!");
        db.SaveAll(tickets);
        Assert.That(db.Select<Ticket>().OrderBy(x => x.Id).Map(x => x.Title), Is.EqualTo(new[] { "One!", "Two!" }));
        Assert.That(tickets.Map(x => x.RowVersion), Is.EqualTo(db.Select<Ticket>().OrderBy(x => x.Id).Map(x => x.RowVersion)));
    }

    [Test]
    public async Task Write_rows_together_async()
    {
        using var db = await OpenDbConnectionAsync();
        db.DropAndCreateTable<Sku>();
        db.DropAndCreateTable<Member>();

        await db.InsertAllAsync(Skus(1, 3));
        Assert.That(await db.CountAsync<Sku>(), Is.EqualTo(3));

        var updated = await db.UpdateAllAsync(Skus(1, 3).Map(x => { x.Stock = 9; return x; }));
        Assert.That(updated, Is.EqualTo(3));
        Assert.That((await db.SelectAsync<Sku>()).Map(x => x.Stock), Is.All.EqualTo(9));

        await db.UpsertAllAsync(Skus(2, 4).Map(x => { x.Name = "Upserted " + x.Id; return x; }));
        Assert.That((await db.SelectAsync<Sku>()).OrderBy(x => x.Id).Map(x => x.Name), Is.EqualTo(new[] {
            "Sku 1", "Upserted 2", "Upserted 3", "Upserted 4", "Upserted 5" }));

        var added = await db.SaveAllAsync(Skus(4, 4).Map(x => { x.Name = "Saved " + x.Id; return x; }));
        Assert.That(added, Is.EqualTo(2));
        Assert.That((await db.SelectAsync<Sku>()).OrderBy(x => x.Id).Map(x => x.Name), Is.EqualTo(new[] {
            "Sku 1", "Upserted 2", "Upserted 3", "Saved 4", "Saved 5", "Saved 6", "Saved 7" }));

        var members = new List<Member> { new() { Name = "Ann" }, new() { Name = "Bob" } };
        await db.SaveAllAsync(members);
        members[0].Points = 5;
        members.Add(new Member { Name = "Cat" });
        await db.UpsertAllAsync(members);
        Assert.That(members.Map(x => x.Id).Distinct().Count(), Is.EqualTo(3));
        Assert.That((await db.SelectAsync<Member>()).OrderBy(x => x.Name).Map(x => x.Points), Is.EqualTo(new[] { 5, 0, 0 }));
    }

    [Test]
    public void Filters_are_run_for_each_row()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Sku>();

        var statements = new List<string>();
        var commandFilters = 0;
        OrmLiteConfig.BeforeExecFilter = dbCmd => statements.Add(dbCmd.CommandText);
        OrmLiteConfig.InsertFilter = (dbCmd, row) => { if (row is Sku sku) sku.Name = sku.Name.ToUpper(); };
        try
        {
            db.InsertAll(Skus(1, 3), dbCmd => commandFilters++);
        }
        finally
        {
            OrmLiteConfig.BeforeExecFilter = null;
            OrmLiteConfig.InsertFilter = null;
        }

        Assert.That(statements.Count(x => x.StartsWith("INSERT")), Is.EqualTo(3));
        Assert.That(commandFilters, Is.EqualTo(3));
        Assert.That(db.Select<Sku>().OrderBy(x => x.Id).Map(x => x.Name), Is.EqualTo(new[] { "SKU 1", "SKU 2", "SKU 3" }));
    }

    [Test]
    public void Results_filters_get_each_statement()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Sku>();

        using (var captured = new CaptureSqlFilter())
        {
            db.InsertAll(Skus(1, 3));
            Assert.That(captured.SqlStatements.Count(x => x.StartsWith("INSERT")), Is.EqualTo(3));
        }
        Assert.That(db.Count<Sku>(), Is.EqualTo(0));
    }
}
