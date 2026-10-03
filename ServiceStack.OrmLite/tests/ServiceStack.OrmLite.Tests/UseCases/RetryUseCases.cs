using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// A RetryPolicy on the dialect runs statements again that fail with a temporary error, e.g. a deadlock, throttling
/// or a lost connection. Statements the database confirmed weren't applied are always retried, statements that may
/// have been applied only if they're reads. Statements in a transaction aren't retried, db.RunInTransaction() runs
/// the whole transaction again instead.
/// </summary>
[TestFixtureOrmLite]
public class RetryUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public class RetryItem
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class RetryAccount
    {
        public int Id { get; set; }
        public int Balance { get; set; }
    }

    // A temporary error that the dialect doesn't recognize, which tests register with Handle()
    public class FakeTransientException() : Exception("Fake transient error");

    private readonly List<int> retries = [];

    private OrmLiteRetryPolicy CreatePolicy(int maxRetries = 3) =>
        OrmLiteRetry.Exponential(maxRetries, delay: TimeSpan.FromMilliseconds(1), maxDelay: TimeSpan.FromMilliseconds(10))
            .OnRetry((ex, retry, delay) => retries.Add(retry));

    // Throw a temporary error for the next INSERTs
    private void FailInserts(int times)
    {
        DialectProvider.OnBeforeExecuteNonQuery = cmd => {
            if (times > 0 && cmd.CommandText.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
            {
                times--;
                throw new FakeTransientException();
            }
        };
    }

    [SetUp]
    public void SetUp()
    {
        retries.Clear();
        using var db = OpenDbConnection();
        db.DropAndCreateTable<RetryItem>();
        db.DropAndCreateTable<RetryAccount>();
    }

    [TearDown]
    public void TearDown()
    {
        DialectProvider.RetryPolicy = null;
        DialectProvider.OnBeforeExecuteNonQuery = null;
    }

    [Test]
    public void Waits_twice_as_long_after_each_retry_up_to_the_max_delay()
    {
        var policy = OrmLiteRetry.Exponential(maxRetries: 5, delay: TimeSpan.FromMilliseconds(50), maxDelay: TimeSpan.FromMilliseconds(300));
        policy.Jitter = false;
        Assert.That(Enumerable.Range(1, 5).Select(x => policy.GetDelay(x).TotalMilliseconds),
            Is.EqualTo(new[] { 50, 100, 200, 300, 300 }));

        // a random 50-100% of the delay
        policy.Jitter = true;
        var delays = Enumerable.Range(1, 100).Select(_ => policy.GetDelay(3).TotalMilliseconds).ToList();
        Assert.That(delays.All(x => x is >= 100 and <= 200));
    }

    [Test]
    public void Retries_a_statement_that_the_database_did_not_apply()
    {
        DialectProvider.RetryPolicy = CreatePolicy()
            .Handle(e => e is FakeTransientException, TransientError.NotApplied);
        FailInserts(2);

        using var db = OpenDbConnection();
        db.Insert(new RetryItem { Name = "A" });

        Assert.That(retries, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(db.Select<RetryItem>().Map(x => x.Name), Is.EqualTo(new[] { "A" }));
    }

    [Test]
    public void Throws_the_error_after_the_last_retry()
    {
        DialectProvider.RetryPolicy = CreatePolicy(maxRetries: 2)
            .Handle(e => e is FakeTransientException, TransientError.NotApplied);
        FailInserts(10);

        using var db = OpenDbConnection();
        Assert.Throws<FakeTransientException>(() => db.Insert(new RetryItem { Name = "A" }));
        Assert.That(retries, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void Does_not_retry_a_write_that_may_have_been_applied()
    {
        DialectProvider.RetryPolicy = CreatePolicy()
            .Handle(e => e is FakeTransientException); // MaybeApplied
        FailInserts(1);

        using var db = OpenDbConnection();
        Assert.Throws<FakeTransientException>(() => db.Insert(new RetryItem { Name = "A" }));
        Assert.That(retries, Is.Empty);
    }

    [Test]
    public void Does_not_retry_statements_without_a_policy_or_in_a_transaction()
    {
        FailInserts(1);
        using var db = OpenDbConnection();
        Assert.Throws<FakeTransientException>(() => db.Insert(new RetryItem { Name = "A" }));

        DialectProvider.RetryPolicy = CreatePolicy()
            .Handle(e => e is FakeTransientException, TransientError.NotApplied);
        FailInserts(1);

        // The database rolls back the whole transaction, so retrying only this statement would lose the others
        using (var trans = db.OpenTransaction())
        {
            Assert.Throws<FakeTransientException>(() => db.Insert(new RetryItem { Name = "A" }));
        }
        Assert.That(retries, Is.Empty);
    }

    [Test]
    public void RunInTransaction_runs_the_whole_transaction_again()
    {
        DialectProvider.RetryPolicy = CreatePolicy()
            .Handle(e => e is FakeTransientException, TransientError.NotApplied);

        using var db = OpenDbConnection();
        var attempts = 0;
        var result = db.RunInTransaction(() => {
            attempts++;
            db.Insert(new RetryItem { Name = "A" });
            if (attempts == 1)
                FailInserts(1); // B fails the first time, after A was inserted
            db.Insert(new RetryItem { Name = "B" });
            return attempts;
        });

        Assert.That(attempts, Is.EqualTo(2));
        Assert.That(retries, Is.EqualTo(new[] { 1 }));
        // A of the first attempt was rolled back
        Assert.That(db.Select<RetryItem>().OrderBy(x => x.Name).Map(x => x.Name), Is.EqualTo(new[] { "A", "B" }));
        Assert.That(result, Is.EqualTo(2));
        Assert.That(db.InTransaction(), Is.False);
    }

    [Test]
    public async Task RunInTransactionAsync_runs_the_whole_transaction_again()
    {
        DialectProvider.RetryPolicy = CreatePolicy()
            .Handle(e => e is FakeTransientException, TransientError.NotApplied);

        using var db = await OpenDbConnectionAsync();
        var attempts = 0;
        await db.RunInTransactionAsync(async () => {
            attempts++;
            await db.InsertAsync(new RetryItem { Name = "A" });
            if (attempts == 1)
                throw new FakeTransientException();
            await db.InsertAsync(new RetryItem { Name = "B" });
        });

        Assert.That(attempts, Is.EqualTo(2));
        Assert.That((await db.SelectAsync<RetryItem>()).OrderBy(x => x.Name).Map(x => x.Name), Is.EqualTo(new[] { "A", "B" }));
    }

    [Test]
    public void RunInTransaction_rolls_back_and_throws_errors_that_are_not_temporary()
    {
        DialectProvider.RetryPolicy = CreatePolicy();

        using var db = OpenDbConnection();
        var attempts = 0;
        Assert.Throws<ArgumentException>(() => db.RunInTransaction(() => {
            attempts++;
            db.Insert(new RetryItem { Name = "A" });
            throw new ArgumentException("Invalid");
        }));

        Assert.That(attempts, Is.EqualTo(1));
        Assert.That(db.Count<RetryItem>(), Is.EqualTo(0));
        Assert.That(db.InTransaction(), Is.False);
    }

    [Test]
    public void RunInTransaction_commits_without_a_policy_and_joins_a_transaction_that_is_open()
    {
        using var db = OpenDbConnection();
        db.RunInTransaction(() => db.Insert(new RetryItem { Name = "A" }));
        Assert.That(db.Count<RetryItem>(), Is.EqualTo(1));

        // It's part of the outer transaction, which is rolled back
        using (db.OpenTransaction())
        {
            db.RunInTransaction(() => db.Insert(new RetryItem { Name = "B" }));
            Assert.That(db.InTransaction());
        }
        Assert.That(db.Count<RetryItem>(), Is.EqualTo(1));
    }

    [Test]
    public void Retries_opening_a_connection()
    {
        if (!DbFactory.AutoDisposeConnection)
            Assert.Ignore("Shared connections are only opened once");

        DialectProvider.RetryPolicy = CreatePolicy()
            .Handle(e => e is FakeTransientException);

        var failures = 2;
        var connectionFilter = DbFactory.ConnectionFilter;
        DbFactory.ConnectionFilter = conn => failures-- > 0 ? throw new FakeTransientException() : conn;
        try
        {
            using var db = OpenDbConnection();
            Assert.That(db.State, Is.EqualTo(ConnectionState.Open));
            Assert.That(retries, Is.EqualTo(new[] { 1, 2 }));
        }
        finally
        {
            DbFactory.ConnectionFilter = connectionFilter;
        }
    }

    private string GetSessionId(IDbConnection db) => Dialect switch {
        _ when (Dialect & Dialect.AnyPostgreSql) != 0 => db.Scalar<int>("SELECT pg_backend_pid()").ToString(),
        _ when (Dialect & Dialect.AnySqlServer) != 0 => db.Scalar<short>("SELECT @@SPID").ToString(),
        _ when (Dialect & Dialect.AnyMySql) != 0 => db.Scalar<long>("SELECT CONNECTION_ID()").ToString(),
        _ => null,
    };

    // End the connection's session from another connection, as a failover or network reset would
    private void KillSession(string sessionId)
    {
        using var admin = DbFactory.OpenDbConnection();
        if ((Dialect & Dialect.AnyPostgreSql) != 0)
            admin.ExecuteSql($"SELECT pg_terminate_backend({sessionId})");
        else
            admin.ExecuteSql($"KILL {sessionId}");
    }

    private void IgnoreIfNotServer()
    {
        if ((Dialect & (Dialect.AnyPostgreSql | Dialect.AnySqlServer | Dialect.AnyMySql)) == 0)
            Assert.Ignore("Only applies to database servers");
    }

    private void IgnoreIfSessionCantBeKilled()
    {
        IgnoreIfNotServer();
        // SqlClient reconnects idle connections itself (ConnectRetryCount), and a statement on a session that was
        // killed after it wrote waits until it times out
        if ((Dialect & Dialect.AnySqlServer) != 0)
            Assert.Ignore("A killed session isn't a lost connection in SqlClient");
    }

    [Test]
    public void Reads_are_retried_after_the_connection_is_lost()
    {
        IgnoreIfSessionCantBeKilled();
        DialectProvider.RetryPolicy = CreatePolicy();

        using var db = OpenDbConnection();
        db.Insert(new RetryItem { Name = "A" });
        var sessionId = GetSessionId(db);

        KillSession(sessionId);
        Assert.That(db.Select<RetryItem>().Map(x => x.Name), Is.EqualTo(new[] { "A" }));
        Assert.That(retries, Is.Not.Empty);
        Assert.That(GetSessionId(db), Is.Not.EqualTo(sessionId)); // reopened
    }

    [Test]
    public async Task Reads_are_retried_after_the_connection_is_lost_Async()
    {
        IgnoreIfSessionCantBeKilled();
        DialectProvider.RetryPolicy = CreatePolicy();

        using var db = await OpenDbConnectionAsync();
        await db.InsertAsync(new RetryItem { Name = "A" });
        var sessionId = GetSessionId(db);

        KillSession(sessionId);
        Assert.That((await db.SelectAsync<RetryItem>()).Map(x => x.Name), Is.EqualTo(new[] { "A" }));
        Assert.That(await db.CountAsync<RetryItem>(), Is.EqualTo(1));
        Assert.That(retries, Is.Not.Empty);
    }

    [Test]
    public void Writes_are_not_retried_after_the_connection_is_lost()
    {
        IgnoreIfSessionCantBeKilled();
        DialectProvider.RetryPolicy = CreatePolicy();

        using var db = OpenDbConnection();
        KillSession(GetSessionId(db));

        // It's not known if a write was applied when its connection is lost
        Assert.That(() => db.Insert(new RetryItem { Name = "A" }), Throws.Exception);
        Assert.That(retries, Is.Empty);
    }

    [Test]
    public async Task Deadlocked_transactions_are_run_again()
    {
        IgnoreIfNotServer();
        DialectProvider.RetryPolicy = CreatePolicy();

        using (var db = OpenDbConnection())
        {
            db.InsertAll([new RetryAccount { Id = 1, Balance = 100 }, new RetryAccount { Id = 2, Balance = 100 }]);
        }

        // Each transfer locks one account, then waits for the other to lock its account before locking the other
        // account, so one of them is chosen as the deadlock victim
        using var bothLocked = new Barrier(2);
        async Task Transfer(int from, int to, int amount)
        {
            using var db = await OpenDbConnectionAsync();
            var attempts = 0;
            await db.RunInTransactionAsync(async () => {
                attempts++;
                await db.UpdateAddAsync(() => new RetryAccount { Balance = -amount }, x => x.Id == from);
                if (attempts == 1)
                    bothLocked.SignalAndWait(TimeSpan.FromSeconds(10));
                await db.UpdateAddAsync(() => new RetryAccount { Balance = amount }, x => x.Id == to);
            });
        }

        await Task.WhenAll(
            Task.Run(() => Transfer(1, 2, 10)),
            Task.Run(() => Transfer(2, 1, 30)));

        using (var db = OpenDbConnection())
        {
            var balances = db.Select<RetryAccount>().OrderBy(x => x.Id).Map(x => x.Balance);
            Assert.That(balances, Is.EqualTo(new[] { 120, 80 }));
        }
        Assert.That(retries, Is.EqualTo(new[] { 1 }));
    }
}
