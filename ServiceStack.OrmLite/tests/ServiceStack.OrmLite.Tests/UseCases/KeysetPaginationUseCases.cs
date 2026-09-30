using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// SeekAfter() pages through results by continuing after the last row of the previous page (keyset pagination),
/// instead of skipping rows with Skip(). Pages are fast at any depth and don't skip or repeat rows when rows are
/// added or removed between requests. The query's ORDER BY should end with a unique column like the primary key.
/// </summary>
[TestFixtureOrmLite]
public class KeysetPaginationUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    const int PageSize = 100;

    [Test]
    public void Page_through_rows_in_primary_key_order()
    {
        using var db = OpenDbConnection();
        var allIds = Bookstore.SeedMany(db, 1000).Map(x => x.Id).OrderBy(x => x).ToList();

        var ids = new List<int>();
        Book last = null;
        while (true)
        {
            var q = db.From<Book>().OrderBy(x => x.Id).Take(PageSize);
            if (last != null)
                q.SeekAfter(last); // continue after the last row of the previous page

            var page = db.Select(q);
            if (page.Count == 0)
                break;

            ids.AddRange(page.Map(x => x.Id));
            last = page[page.Count - 1];
        }

        Assert.That(ids, Is.EqualTo(allIds));
    }

    [Test]
    public void Page_through_rows_sorted_by_a_non_unique_column()
    {
        using var db = OpenDbConnection();
        Bookstore.SeedMany(db, 1000); // many books share the same Year

        // Newest books first, using Id as a tie-breaker so every row has a unique position
        SqlExpression<Book> Query() => db.From<Book>().OrderByDescending(x => x.Year).ThenBy(x => x.Id);
        var expected = db.Select(Query()).Map(x => x.Id);

        var ids = new List<int>();
        Book last = null;
        while (true)
        {
            var q = Query().Take(PageSize);
            if (last != null)
                q.SeekAfter(last);

            var page = db.Select(q);
            if (page.Count == 0)
                break;
            ids.AddRange(page.Map(x => x.Id));
            last = page[page.Count - 1];
        }

        Assert.That(ids, Is.EqualTo(expected));
    }

    [Test]
    public void Seek_after_explicit_values_with_mixed_sort_directions()
    {
        using var db = OpenDbConnection();
        Bookstore.SeedMany(db, 500);

        // Cheapest first, then newest first, e.g. from a cursor in an API request: ?after=10,1985,42
        SqlExpression<Book> Query() => db.From<Book>()
            .OrderBy(x => x.Price).ThenByDescending(x => x.Year).ThenBy(x => x.Id);
        var all = db.Select(Query());
        var cursor = all[123];

        var page = db.Select(Query().SeekAfter(cursor.Price, cursor.Year, cursor.Id).Take(PageSize));

        Assert.That(page.Map(x => x.Id), Is.EqualTo(all.Skip(124).Take(PageSize).Map(x => x.Id)));
    }

    [Test]
    public void Combine_with_filters()
    {
        using var db = OpenDbConnection();
        Bookstore.SeedMany(db, 500);

        SqlExpression<Book> Query() => db.From<Book>()
            .Where(x => x.Available && x.Genre == Genre.Fiction)
            .OrderByDescending(x => x.Price).ThenBy(x => x.Id);
        var all = db.Select(Query());

        var page = db.Select(Query().SeekAfter(all[9]).Take(10));

        Assert.That(page.Map(x => x.Id), Is.EqualTo(all.Skip(10).Take(10).Map(x => x.Id)));
        Assert.That(page.All(x => x.Available && x.Genre == Genre.Fiction));
    }

    [Test]
    public void Pages_are_stable_when_rows_are_added()
    {
        using var db = OpenDbConnection();
        Bookstore.SeedMany(db, 50);

        var firstPage = db.Select(db.From<Book>().OrderBy(x => x.Id).Take(20));

        // A new book is added before the next page is requested
        db.Insert(new Book { Title = "New Book", Author = "New Author", Price = 1, Year = 2026 });

        var secondPage = db.Select(db.From<Book>().OrderBy(x => x.Id).SeekAfter(firstPage.Last()).Take(20));
        Assert.That(secondPage[0].Id, Is.EqualTo(firstPage.Last().Id + 1)); // continues exactly where it left off
    }

    [Test]
    public async Task Page_through_rows_Async()
    {
        using var db = await OpenDbConnectionAsync();
        var total = Bookstore.SeedMany(db, 300).Count;

        var count = 0;
        Book last = null;
        List<Book> page;
        do
        {
            var q = db.From<Book>().OrderBy(x => x.Id).Take(PageSize);
            if (last != null)
                q.SeekAfter(last);
            page = await db.SelectAsync(q);
            count += page.Count;
            if (page.Count > 0)
                last = page[page.Count - 1];
        } while (page.Count == PageSize);

        Assert.That(count, Is.EqualTo(total));
    }

    [Test]
    public void Invalid_usage_throws()
    {
        using var db = OpenDbConnection();
        var book = new Book { Id = 1, Year = 2000, Price = 10 };

        // Must be ordered first
        Assert.Throws<InvalidOperationException>(() => db.From<Book>().SeekAfter(book));
        // A value is required for each ORDER BY column
        Assert.Throws<ArgumentException>(() => db.From<Book>().OrderBy(x => x.Year).ThenBy(x => x.Id).SeekAfter(2000));
        // NULLs can't be compared
        Assert.Throws<ArgumentNullException>(() => db.From<Book>().OrderBy(x => x.Title).SeekAfter(book));
        // Ordering by column position isn't supported
        Assert.Throws<NotSupportedException>(() => db.From<Book>().OrderBy(1).SeekAfter(10));
    }
}
