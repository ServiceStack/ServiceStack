using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCases;

public class BankAccount
{
    public int Id { get; set; }
    public string Owner { get; set; }
    public decimal Balance { get; set; }
}

public class WorkItem
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Queue { get; set; }
    public string Status { get; set; }
    public string ClaimedBy { get; set; }
}

/// <summary>
/// ForUpdate() locks the rows a query selects until the end of the transaction, so other transactions can't change
/// them in between reading and updating them. ForUpdate(skipLocked: true) skips rows locked by other transactions,
/// letting multiple workers take different items from the same queue table.
/// </summary>
[TestFixtureOrmLite]
public class ForUpdateUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    const string NoRowLocks = "SQLite only has database-level locks, ForUpdate() is ignored";

    static readonly TimeSpan BlockedFor = TimeSpan.FromMilliseconds(500);
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    void SeedAccounts()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<BankAccount>();
        db.Insert(new BankAccount { Id = 1, Owner = "Alice", Balance = 100 });
        db.Insert(new BankAccount { Id = 2, Owner = "Bob", Balance = 100 });
    }

    void SeedWorkItems()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<WorkItem>();
        db.InsertAll(Enumerable.Range(1, 6).Map(i => new WorkItem { Queue = "emails", Status = "Queued" }));
    }

    [Test]
    public void Lock_a_row_while_updating_it()
    {
        SeedAccounts();
        using var db = OpenDbConnection();

        using (var trans = db.OpenTransaction())
        {
            // No other transaction can update or lock this row until this transaction ends
            var account = db.Single(db.From<BankAccount>().Where(x => x.Id == 1).ForUpdate());
            account.Balance -= 30;
            db.Update(account);
            trans.Commit();
        }

        Assert.That(db.SingleById<BankAccount>(1).Balance, Is.EqualTo(70m));
    }

    [Test]
    [IgnoreDialect(Dialect.Sqlite, NoRowLocks)]
    public void Other_transactions_wait_for_the_lock()
    {
        SeedAccounts();

        using var db = OpenDbConnection();
        Task otherDeposit;
        using (var trans = db.OpenTransaction())
        {
            var account = db.Single(db.From<BankAccount>().Where(x => x.Id == 1).ForUpdate());

            // Another deposit into the same account waits until this transaction ends
            otherDeposit = Task.Run(() => {
                using var otherDb = OpenDbConnection();
                using var otherTrans = otherDb.OpenTransaction();
                var sameAccount = otherDb.Single(otherDb.From<BankAccount>().Where(x => x.Id == 1).ForUpdate());
                sameAccount.Balance += 10;
                otherDb.Update(sameAccount);
                otherTrans.Commit();
            });
            Assert.That(otherDeposit.Wait(BlockedFor), Is.False, "should wait for the lock");

            account.Balance += 10;
            db.Update(account);
            trans.Commit();
        }

        // It then reads the updated balance, so neither deposit is lost
        Assert.That(otherDeposit.Wait(Timeout));
        Assert.That(db.SingleById<BankAccount>(1).Balance, Is.EqualTo(120m));
    }

    [Test]
    [IgnoreDialect(Dialect.Sqlite, NoRowLocks)]
    public void Other_rows_are_not_locked()
    {
        SeedAccounts();

        using var db = OpenDbConnection();
        using (var trans = db.OpenTransaction())
        {
            db.Single(db.From<BankAccount>().Where(x => x.Id == 1).ForUpdate());

            // Only Alice's account is locked, Bob's can be locked and updated by other transactions
            var otherUpdate = Task.Run(() => {
                using var otherDb = OpenDbConnection();
                using var otherTrans = otherDb.OpenTransaction();
                var bob = otherDb.Single(otherDb.From<BankAccount>().Where(x => x.Id == 2).ForUpdate());
                otherDb.UpdateOnly(() => new BankAccount { Balance = bob.Balance + 5 }, where: x => x.Id == 2);
                otherTrans.Commit();
            });
            Assert.That(otherUpdate.Wait(Timeout));
            trans.Commit();
        }

        Assert.That(db.SingleById<BankAccount>(2).Balance, Is.EqualTo(105m));
    }

    [Test]
    [IgnoreDialect(Dialect.Sqlite, NoRowLocks)]
    public void Workers_take_different_items_from_a_queue()
    {
        SeedWorkItems();

        List<WorkItem> TakeItems(System.Data.IDbConnection db) => db.Select(db.From<WorkItem>()
            .Where(x => x.Queue == "emails" && x.Status == "Queued")
            .OrderBy(x => x.Id)
            .Take(3)
            .ForUpdate(skipLocked: true));

        // Update each locked row by its primary key, as SQL Server may otherwise scan and wait on rows locked by others
        void Claim(System.Data.IDbConnection db, List<int> ids, string worker)
        {
            foreach (var id in ids)
                db.UpdateOnly(() => new WorkItem { Status = "Processing", ClaimedBy = worker }, where: x => x.Id == id);
        }

        using var db1 = OpenDbConnection();
        List<int> ids1;
        List<WorkItem> worker2Items = null;
        using (var trans1 = db1.OpenTransaction())
        {
            ids1 = TakeItems(db1).Map(x => x.Id);

            // A second worker skips the items locked by the first instead of waiting for them
            var worker2 = Task.Run(() => {
                using var db2 = OpenDbConnection();
                using var trans2 = db2.OpenTransaction();
                worker2Items = TakeItems(db2);
                Claim(db2, worker2Items.Map(x => x.Id), "worker2");
                trans2.Commit();
            });
            Assert.That(worker2.Wait(Timeout), "should not wait for the locked items");

            Claim(db1, ids1, "worker1");
            trans1.Commit();
        }

        Assert.That(ids1, Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(worker2Items.Map(x => x.Id), Is.EqualTo(new[] { 4, 5, 6 }));
        Assert.That(db1.Count<WorkItem>(x => x.ClaimedBy == "worker1"), Is.EqualTo(3));
        Assert.That(db1.Count<WorkItem>(x => x.ClaimedBy == "worker2"), Is.EqualTo(3));
    }

    [Test]
    public void Lock_rows_of_a_query_with_joins()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Only rows of the query's table (Book) are locked
        using var trans = db.OpenTransaction();
        var books = db.Select(db.From<Book>()
            .Join<BookReview>((b, r) => b.Id == r.BookId)
            .Where<BookReview>(r => r.Reviewer == "Bob")
            .ForUpdate());
        Assert.That(books.Map(x => x.Title), Is.EquivalentTo(new[] { "The Hobbit", "SPQR" }));
    }

    [Test]
    public async Task Async_APIs()
    {
        SeedAccounts();
        using var db = await OpenDbConnectionAsync();

        using (var trans = db.OpenTransaction())
        {
            var account = await db.SingleAsync(db.From<BankAccount>().Where(x => x.Owner == "Bob").ForUpdate());
            await db.UpdateOnlyAsync(() => new BankAccount { Balance = account.Balance * 2 }, where: x => x.Id == account.Id);
            trans.Commit();
        }

        Assert.That((await db.SingleByIdAsync<BankAccount>(2)).Balance, Is.EqualTo(200m));
    }

    [Test]
    public void Unsupported_queries_throw()
    {
        using var db = OpenDbConnection();

        var union = db.From<BankAccount>().Where(x => x.Id == 1)
            .Union(db.From<BankAccount>().Where(x => x.Id == 2))
            .ForUpdate();
        Assert.Throws<NotSupportedException>(() => union.ToSelectStatement());

        var descendants = db.From<Subject>()
            .WithRecursive(db.From<Subject>().Where(x => x.Id == 1), (parent, child) => child.ParentId == parent.Id)
            .ForUpdate();
        Assert.Throws<NotSupportedException>(() => descendants.ToSelectStatement());
    }
}
