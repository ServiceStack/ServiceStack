using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// SelectLazyAsync() / ColumnLazyAsync() return IAsyncEnumerable streams to process large result sets
/// one row at a time with bounded memory, without blocking threads.
/// </summary>
[TestFixtureOrmLite]
public class StreamingUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public async Task Stream_rows_from_a_typed_query()
    {
        using var db = await OpenDbConnectionAsync();
        Bookstore.Seed(db);

        var titles = new List<string>();
        await foreach (var book in db.SelectLazyAsync(db.From<Book>().Where(x => x.Available).OrderBy(x => x.Year)))
        {
            titles.Add(book.Title);
        }

        Assert.That(titles, Is.EqualTo(new[] {
            "The Hobbit", "Dune", "Cosmos", "Neuromancer", "A Brief History of Time", "SPQR" }));
    }

    [Test]
    public async Task Stream_rows_from_a_parameterized_query()
    {
        using var db = await OpenDbConnectionAsync();
        Bookstore.Seed(db);

        var count = 0;
        await foreach (var book in db.SelectLazyAsync<Book>("Author = @author", new { author = "J.R.R. Tolkien" }))
            count++;
        Assert.That(count, Is.EqualTo(2));

        // Or with interpolated SQL where each value is a db param
        var genre = Genre.History;
        count = 0;
        await foreach (var book in db.SelectLazyAsync<Book>(Sql.Fmt($"Genre = {genre}")))
            count++;
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public async Task Stream_a_single_column()
    {
        using var db = await OpenDbConnectionAsync();
        Bookstore.Seed(db);

        var total = 0m;
        await foreach (var price in db.ColumnLazyAsync<decimal>(db.From<Book>().Select(x => x.Price)))
            total += price;

        Assert.That(total, Is.EqualTo(108.98m).Within(0.001m));
    }

    [Test]
    public async Task Export_a_large_table_in_batches()
    {
        using var db = await OpenDbConnectionAsync();
        Bookstore.SeedMany(db, 1000);

        // e.g. write rows to a file or queue in batches of 100 without loading the whole table in memory
        var batch = new List<Book>();
        var batches = 0;
        await foreach (var book in db.SelectLazyAsync(db.From<Book>().OrderBy(x => x.Id)))
        {
            batch.Add(book);
            if (batch.Count == 100)
            {
                batches++;
                batch.Clear();
            }
        }

        Assert.That(batches, Is.EqualTo(10));
    }

    [Test]
    public async Task Stop_reading_early()
    {
        using var db = await OpenDbConnectionAsync();
        Bookstore.Seed(db);

        // Breaking out of the loop closes the reader, so the connection can be used straight away
        Book first = null;
        await foreach (var book in db.SelectLazyAsync(db.From<Book>().OrderByDescending(x => x.Price)))
        {
            first = book;
            break;
        }

        Assert.That(first.Title, Is.EqualTo("SPQR"));
        Assert.That(await db.CountAsync<Book>(), Is.EqualTo(8));
    }

    [Test]
    public async Task Cancel_a_stream()
    {
        using var db = await OpenDbConnectionAsync();
        Bookstore.Seed(db);

        using var cts = new CancellationTokenSource();
        var read = 0;
        Assert.CatchAsync<OperationCanceledException>(async () => {
            await foreach (var book in db.SelectLazyAsync(db.From<Book>(), cts.Token))
            {
                if (++read == 2)
                    cts.Cancel();
            }
        });
        Assert.That(read, Is.EqualTo(2));

        // Connection is still usable after cancelling
        Assert.That(await db.CountAsync<Book>(), Is.EqualTo(8));
    }
}
