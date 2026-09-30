using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.UseCases;

public class SalesOrder
{
    public int Id { get; set; }
    public string Customer { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedDate { get; set; }
}

/// <summary>
/// Results of queries with window functions
/// </summary>
public class SalesOrderStats
{
    public int Id { get; set; }
    public string Customer { get; set; }
    public decimal Total { get; set; }
    public long RowNumber { get; set; }
    public long Rank { get; set; }
    public long DenseRank { get; set; }
    public long Bucket { get; set; }
    public decimal RunningTotal { get; set; }
    public decimal CustomerTotal { get; set; }
    public long CustomerOrders { get; set; }
    public decimal? PreviousTotal { get; set; }
    public decimal? NextTotal { get; set; }
    public decimal FirstTotal { get; set; }
    public decimal LastTotal { get; set; }
    public double MovingAverage { get; set; }
}

public static class SalesOrders
{
    public static void Seed(IDbConnection db)
    {
        db.DropAndCreateTable<SalesOrder>();
        db.InsertAll(new List<SalesOrder> {
            new() { Id = 1, Customer = "Alice", Total = 100, CreatedDate = new DateTime(2026, 1, 1) },
            new() { Id = 2, Customer = "Bob",   Total = 80,  CreatedDate = new DateTime(2026, 1, 2) },
            new() { Id = 3, Customer = "Carol", Total = 500, CreatedDate = new DateTime(2026, 1, 3) },
            new() { Id = 4, Customer = "Alice", Total = 250, CreatedDate = new DateTime(2026, 1, 5) },
            new() { Id = 5, Customer = "Bob",   Total = 80,  CreatedDate = new DateTime(2026, 1, 6) },
            new() { Id = 6, Customer = "Alice", Total = 50,  CreatedDate = new DateTime(2026, 1, 9) },
            new() { Id = 7, Customer = "Bob",   Total = 120, CreatedDate = new DateTime(2026, 1, 10) },
            new() { Id = 8, Customer = "Alice", Total = 300, CreatedDate = new DateTime(2026, 1, 12) },
            new() { Id = 9, Customer = "Bob",   Total = 60,  CreatedDate = new DateTime(2026, 1, 14) },
        });
    }
}

/// <summary>
/// Window functions calculate a value for each row from the other rows in its window, e.g. its rank within a group or a
/// running total, without grouping the rows. TopPerGroup() returns the first rows of each group, e.g. each customer's
/// latest orders.
/// </summary>
[TestFixtureOrmLite]
public class WindowFunctionUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Rank_rows_within_each_group()
    {
        using var db = OpenDbConnection();
        SalesOrders.Seed(db);

        // Each customer's orders ranked from largest to smallest
        var q = db.From<SalesOrder>()
            .Select(x => new {
                x.Id,
                x.Customer,
                x.Total,
                Rank = Sql.Rank(w => w.PartitionBy(x.Customer).OrderByDescending(x.Total)),
                DenseRank = Sql.DenseRank(w => w.PartitionBy(x.Customer).OrderByDescending(x.Total)),
                RowNumber = Sql.RowNumber(w => w.PartitionBy(x.Customer).OrderByDescending(x.Total).ThenBy(x.Id)),
            });

        var rows = db.Select<SalesOrderStats>(q).ToDictionary(x => x.Id);

        Assert.That(rows[8].Rank, Is.EqualTo(1)); // Alice's largest order
        Assert.That(rows[6].Rank, Is.EqualTo(4)); // Alice's smallest order
        Assert.That(rows[3].Rank, Is.EqualTo(1)); // Carol's only order

        // Bob's two orders of 80 share a rank, RANK() skips the next rank, DENSE_RANK() doesn't
        Assert.That(new[] { rows[7].Rank, rows[2].Rank, rows[5].Rank, rows[9].Rank }, Is.EqualTo(new[] { 1, 2, 2, 4 }));
        Assert.That(new[] { rows[7].DenseRank, rows[2].DenseRank, rows[5].DenseRank, rows[9].DenseRank }, Is.EqualTo(new[] { 1, 2, 2, 3 }));
        // ROW_NUMBER() numbers every row, ties are ordered by ThenBy(x.Id)
        Assert.That(new[] { rows[7].RowNumber, rows[2].RowNumber, rows[5].RowNumber, rows[9].RowNumber }, Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void Running_totals_and_group_totals()
    {
        using var db = OpenDbConnection();
        SalesOrders.Seed(db);

        var q = db.From<SalesOrder>()
            .Where(x => x.Customer == "Alice")
            .OrderBy(x => x.CreatedDate)
            .Select(x => new {
                x.Id,
                x.Total,
                // With an ORDER BY, the window is every row up to the current row
                RunningTotal = Sql.Sum(x.Total, w => w.PartitionBy(x.Customer).OrderBy(x.CreatedDate)),
                // Without an ORDER BY, the window is the whole partition
                CustomerTotal = Sql.Sum(x.Total, w => w.PartitionBy(x.Customer)),
                CustomerOrders = Sql.Count("*", w => w.PartitionBy(x.Customer)),
            });

        var rows = db.Select<SalesOrderStats>(q);
        Assert.That(rows.Map(x => x.RunningTotal), Is.EqualTo(new[] { 100m, 350m, 400m, 700m }));
        Assert.That(rows.All(x => x.CustomerTotal == 700m && x.CustomerOrders == 4));

        // e.g. each order's share of the customer's total
        Assert.That(rows.Map(x => Math.Round(x.Total / x.CustomerTotal * 100)), Is.EqualTo(new[] { 14m, 36m, 7m, 43m }));
    }

    [Test]
    public void Compare_with_previous_and_next_rows()
    {
        using var db = OpenDbConnection();
        SalesOrders.Seed(db);

        var q = db.From<SalesOrder>()
            .Where(x => x.Customer == "Alice")
            .OrderBy(x => x.CreatedDate)
            .Select(x => new {
                x.Id,
                x.Total,
                PreviousTotal = Sql.Lag(x.Total, w => w.PartitionBy(x.Customer).OrderBy(x.CreatedDate)),
                NextTotal = Sql.Lead(x.Total, 2, w => w.PartitionBy(x.Customer).OrderBy(x.CreatedDate)),
                FirstTotal = Sql.FirstValue(x.Total, w => w.PartitionBy(x.Customer).OrderBy(x.CreatedDate)),
                // The window of the last value needs to include the rows after the current row
                LastTotal = Sql.LastValue(x.Total, w => w.PartitionBy(x.Customer).OrderBy(x.CreatedDate).RowsBetween(null, null)),
            });

        var rows = db.Select<SalesOrderStats>(q);
        Assert.That(rows.Map(x => x.PreviousTotal), Is.EqualTo(new decimal?[] { null, 100m, 250m, 50m }));
        Assert.That(rows.Map(x => x.NextTotal), Is.EqualTo(new decimal?[] { 50m, 300m, null, null }));
        Assert.That(rows.All(x => x.FirstTotal == 100m && x.LastTotal == 300m));

        // e.g. the change from the previous order
        Assert.That(rows.Map(x => x.Total - x.PreviousTotal), Is.EqualTo(new decimal?[] { null, 150m, -200m, 250m }));
    }

    [Test]
    public void Moving_average_over_a_frame_of_rows()
    {
        using var db = OpenDbConnection();
        SalesOrders.Seed(db);

        // The average of each order and the 2 orders before it
        var q = db.From<SalesOrder>()
            .OrderBy(x => x.CreatedDate)
            .Select(x => new {
                x.Id,
                MovingAverage = Sql.Avg(x.Total, w => w.OrderBy(x.CreatedDate).RowsBetween(2, 0)),
            });

        var rows = db.Select<SalesOrderStats>(q);
        Assert.That(rows[0].MovingAverage, Is.EqualTo(100).Within(0.01));
        Assert.That(rows[1].MovingAverage, Is.EqualTo(90).Within(0.01));
        Assert.That(rows[2].MovingAverage, Is.EqualTo(226.67).Within(0.01));
        Assert.That(rows[3].MovingAverage, Is.EqualTo(276.67).Within(0.01));
    }

    [Test]
    public void Divide_rows_into_buckets()
    {
        using var db = OpenDbConnection();
        SalesOrders.Seed(db);

        // Split orders into top and bottom halves by total
        var q = db.From<SalesOrder>()
            .Select(x => new {
                x.Id,
                Bucket = Sql.Ntile(2, w => w.OrderByDescending(x.Total).ThenBy(x.Id)),
            });

        var rows = db.Select<SalesOrderStats>(q);
        Assert.That(rows.Where(x => x.Bucket == 1).Map(x => x.Id), Is.EquivalentTo(new[] { 3, 8, 4, 7, 1 }));
        Assert.That(rows.Count(x => x.Bucket == 2), Is.EqualTo(4));
    }

    [Test]
    public void Select_the_top_rows_of_each_group()
    {
        using var db = OpenDbConnection();
        SalesOrders.Seed(db);

        // Each customer's 2 latest orders
        var q = db.From<SalesOrder>()
            .OrderByDescending(x => x.CreatedDate)
            .TopPerGroup(x => x.Customer, take: 2);

        var latest = db.Select(q);
        Assert.That(latest.Map(x => x.Id), Is.EqualTo(new[] { 9, 8, 7, 6, 3 }));
        Assert.That(db.Count(q), Is.EqualTo(5));

        // Order by the group first to return the rows of each group together
        var grouped = db.Select(db.From<SalesOrder>()
            .OrderBy(x => x.Customer).ThenByDescending(x => x.CreatedDate)
            .TopPerGroup(x => x.Customer, take: 2));
        Assert.That(grouped.Map(x => x.Id), Is.EqualTo(new[] { 8, 6, 9, 7, 3 }));
    }

    [Test]
    public void Select_the_largest_row_of_each_group()
    {
        using var db = OpenDbConnection();
        SalesOrders.Seed(db);

        // Where() filters rows before they're ranked: each customer's largest order under 300
        var q = db.From<SalesOrder>()
            .Where(x => x.Total < 300)
            .OrderByDescending(x => x.Total).ThenBy(x => x.Id)
            .TopPerGroup(x => x.Customer, take: 1);

        var largest = db.Select(q);
        Assert.That(largest.Map(x => x.Id), Is.EqualTo(new[] { 4, 7 })); // Carol has no orders under 300

        // Select only some columns, or page the results
        Assert.That(db.Column<int>(q.Clone().Select(x => x.Id)), Is.EqualTo(new[] { 4, 7 }));
        Assert.That(db.Select(q.Clone().Take(1)).Map(x => x.Id), Is.EqualTo(new[] { 4 }));
    }

    public class ReviewStats
    {
        public string Reviewer { get; set; }
        public string Title { get; set; }
        public long ReviewerReviews { get; set; }
        public long RatingRank { get; set; }
    }

    [Test]
    public void Window_functions_in_queries_with_joins()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Each review with how many reviews its reviewer wrote, and its rank among the reviews of its book
        var q = db.From<BookReview>()
            .Join<Book>((r, b) => r.BookId == b.Id)
            .OrderBy(r => r.Id)
            .Select<BookReview, Book>((r, b) => new {
                r.Reviewer,
                b.Title,
                ReviewerReviews = Sql.Count("*", w => w.PartitionBy(r.Reviewer)),
                RatingRank = Sql.Rank(w => w.PartitionBy(b.Title).OrderByDescending(r.Rating)),
            });

        var rows = db.Select<ReviewStats>(q);
        Assert.That(rows.Map(x => x.ReviewerReviews), Is.EqualTo(new[] { 2, 2, 2, 1, 2 })); // Alice, Bob, Alice, Carol, Bob
        Assert.That(rows.Map(x => x.RatingRank), Is.EqualTo(new[] { 1, 2, 1, 1, 1 }));        // Bob rated The Hobbit lower than Alice
    }

    [Test]
    public void Select_the_top_rows_of_each_group_with_joins()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // The cheapest book of each genre with a review rated 3 or more, joins only filter the rows
        var q = db.From<Book>()
            .Join<BookReview>((b, r) => b.Id == r.BookId)
            .Where<BookReview>(r => r.Rating >= 3)
            .OrderBy(x => x.Price)
            .TopPerGroup(x => x.Genre, take: 1);

        Assert.That(db.Select(q).Map(x => x.Title), Is.EqualTo(new[] { "Dune", "The Hobbit", "Cosmos" }));
    }

    [Test]
    public void Group_by_multiple_columns_and_page_the_results()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // The cheapest book of each genre, separately for available and unavailable books
        var q = db.From<Book>()
            .OrderBy(x => x.Price)
            .TopPerGroup(x => new { x.Genre, x.Available }, take: 1);

        Assert.That(db.Select(q).Map(x => x.Title), Is.EqualTo(new[] {
            "Neuromancer", "The Hobbit", "Cosmos", "The Silmarillion", "The Guns of August", "SPQR" }));
        Assert.That(db.Select(q.Clone().Skip(1).Take(2)).Map(x => x.Title), Is.EqualTo(new[] { "The Hobbit", "Cosmos" }));
    }

    [Test]
    public async Task Async_APIs()
    {
        using var db = await OpenDbConnectionAsync();
        SalesOrders.Seed(db);

        var q = db.From<SalesOrder>()
            .Where(x => x.Customer == "Bob")
            .OrderBy(x => x.CreatedDate)
            .Select(x => new {
                x.Id,
                RunningTotal = Sql.Sum(x.Total, w => w.OrderBy(x.CreatedDate)),
            });
        var rows = await db.SelectAsync<SalesOrderStats>(q);
        Assert.That(rows.Map(x => x.RunningTotal), Is.EqualTo(new[] { 80m, 160m, 280m, 340m }));

        var latest = await db.SelectAsync(db.From<SalesOrder>()
            .OrderByDescending(x => x.CreatedDate)
            .TopPerGroup(x => x.Customer, take: 1));
        Assert.That(latest.Map(x => x.Id), Is.EqualTo(new[] { 9, 8, 3 }));
    }

    [Test]
    public void Invalid_usage_throws()
    {
        using var db = OpenDbConnection();

        // The rows of each group need an order
        Assert.Throws<NotSupportedException>(() =>
            db.From<SalesOrder>().TopPerGroup(x => x.Customer, take: 1).ToSelectStatement());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            db.From<SalesOrder>().OrderBy(x => x.Id).TopPerGroup(x => x.Customer, take: 0));

        var locked = db.From<SalesOrder>().OrderBy(x => x.Id).TopPerGroup(x => x.Customer, take: 1).ForUpdate();
        Assert.Throws<NotSupportedException>(() => locked.ToSelectStatement());
    }
}
