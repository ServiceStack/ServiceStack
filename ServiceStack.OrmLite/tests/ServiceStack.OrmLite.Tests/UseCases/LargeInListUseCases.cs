using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.OrmLite.PostgreSQL;
using ServiceStack.OrmLite.SqlServer;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// Queries with thousands of values work on every RDBMS, even past limits like SQL Server's 2100 params
/// or Oracle's 1000 values per IN list. Above the dialect's MaxInListParams (default 1000):
///  - SelectByIds / DeleteByIds execute in batches (deletes in a single transaction)
///  - Contains() uses 1 array param in PostgreSQL, 1 JSON param in SQL Server 2016+, otherwise multiple OR'd IN lists
/// </summary>
[TestFixtureOrmLite]
[NonParallelizable]
public class LargeInListUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    const int ManyBooks = 2500;

    [Test]
    public void SelectByIds_with_thousands_of_ids()
    {
        using var db = OpenDbConnection();
        var ids = Bookstore.SeedMany(db, ManyBooks).Map(x => x.Id);

        var books = db.SelectByIds<Book>(ids);

        Assert.That(books.Count, Is.EqualTo(ManyBooks));
    }

    [Test]
    public async Task SelectByIds_with_thousands_of_ids_Async()
    {
        using var db = await OpenDbConnectionAsync();
        var ids = Bookstore.SeedMany(db, ManyBooks).Map(x => x.Id);

        var books = await db.SelectByIdsAsync<Book>(ids);

        Assert.That(books.Count, Is.EqualTo(ManyBooks));
    }

    [Test]
    public void Contains_with_thousands_of_values()
    {
        using var db = OpenDbConnection();
        var books = Bookstore.SeedMany(db, ManyBooks);

        var ids = books.Where(x => x.Id % 2 == 0).Map(x => x.Id);
        var q = db.From<Book>().Where(x => ids.Contains(x.Id));
        Assert.That(db.Count(q), Is.EqualTo(ids.Count));

        var titles = books.Take(1500).Map(x => x.Title).ToArray();
        Assert.That(db.Count<Book>(x => titles.Contains(x.Title)), Is.EqualTo(1500));

        // NOT IN
        Assert.That(db.Count<Book>(x => !titles.Contains(x.Title)), Is.EqualTo(ManyBooks - 1500));
    }

    [Test]
    public void Contains_uses_the_best_strategy_for_each_RDBMS()
    {
        using var db = OpenDbConnection();
        var ids = Enumerable.Range(1, ManyBooks).ToArray();
        var q = db.From<Book>().Where(x => ids.Contains(x.Id));

        var expected = DialectProvider switch {
            PostgreSqlDialectProvider => "= ANY(",
            SqlServer2016OrmLiteDialectProvider => "OPENJSON(",
            _ => " OR ",
        };
        Assert.That(q.WhereExpression, Does.Contain(expected));
    }

    [Test]
    public void DeleteByIds_with_thousands_of_ids()
    {
        using var db = OpenDbConnection();
        var ids = Bookstore.SeedMany(db, ManyBooks).Map(x => x.Id);

        var deleted = db.DeleteByIds<Book>(ids.Take(2000));

        Assert.That(deleted, Is.EqualTo(2000));
        Assert.That(db.Count<Book>(), Is.EqualTo(ManyBooks - 2000));
    }

    [Test]
    public async Task DeleteByIds_batches_are_part_of_an_existing_transaction()
    {
        using var db = await OpenDbConnectionAsync();
        var ids = Bookstore.SeedMany(db, ManyBooks).Map(x => x.Id);

        using (var trans = db.OpenTransaction())
        {
            await db.DeleteByIdsAsync<Book>(ids);
            Assert.That(await db.CountAsync<Book>(), Is.EqualTo(0));
            trans.Rollback();
        }

        Assert.That(await db.CountAsync<Book>(), Is.EqualTo(ManyBooks));
    }

    [Test]
    public void Configure_the_max_IN_list_size()
    {
        using var db = OpenDbConnection();
        var ids = Bookstore.SeedMany(db, 10).Map(x => x.Id);

        // Configured per dialect, e.g. in AppHost: SqliteDialect.Provider.MaxInListParams = 500;
        var dialect = db.GetDialectProvider();
        var hold = dialect.MaxInListParams;
        try
        {
            dialect.MaxInListParams = 3;
            Assert.That(db.SelectByIds<Book>(ids).Count, Is.EqualTo(10));
            Assert.That(db.Count<Book>(x => ids.Contains(x.Id)), Is.EqualTo(10));
            Assert.That(db.DeleteByIds<Book>(ids), Is.EqualTo(10));
        }
        finally
        {
            dialect.MaxInListParams = hold;
        }
    }
}
