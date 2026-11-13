#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ServiceStack.Jobs;
using ServiceStack.OrmLite;

namespace ServiceStack.Extensions.Tests;

[NonParallelizable]
public class DbJobsRetentionTests
{
    IOrmLiteDialectProvider? originalDialect;
    [SetUp]
    public void SaveDialect()
    {
        try { originalDialect = OrmLiteConfig.DialectProvider; }
        catch (ArgumentNullException) { originalDialect = null; }
    }
    [TearDown]
    public void RestoreDialect() => OrmLiteConfig.DialectProvider = originalDialect!;

    sealed class RetentionJobs(DatabaseJobFeature feature, IServiceProvider services)
        : DbJobs(NullLogger<DbJobs>.Instance, feature, services,
            services.GetRequiredService<IServiceScopeFactory>())
    {
        public void Purge() => PurgeExpiredArchives();
    }

    // Integration connections must point to EMPTY, DISPOSABLE databases. No runner is started.
    static OrmLiteConnectionFactory Factory(string provider)
    {
        if (provider == "sqlite")
            return new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider, setGlobalDialectProvider: false);
        var connection = Environment.GetEnvironmentVariable("SS_JOBS_RETENTION_" + provider.ToUpperInvariant());
        if (string.IsNullOrEmpty(connection))
            Assert.Ignore("Set SS_JOBS_RETENTION_" + provider.ToUpperInvariant() + " to an empty disposable database to run this integration case.");
        var dialect = provider switch {
            "sqlserver" => SqlServerDialect.Provider,
            "mysql" => MySqlDialect.Provider,
            "postgres" => PostgreSqlDialect.Provider,
            _ => throw new ArgumentException(provider),
        };
        return new OrmLiteConnectionFactory(connection!, dialect, setGlobalDialectProvider: false);
    }

    [TestCase("sqlite")]
    [TestCase("sqlserver")]
    [TestCase("mysql")]
    [TestCase("postgres")]
    public void Retention_preserves_operational_data_and_recent_history(string provider)
    {
        var factory = Factory(provider);
        using var db = factory.OpenDbConnection();
        OrmLiteConfig.DialectProvider = factory.DialectProvider;
        var tables = new[] { typeof(BackgroundJob), typeof(JobSummary), typeof(ScheduledTask),
            typeof(CompletedJob), typeof(FailedJob), typeof(JobAttempt) };
        foreach (var table in tables)
            Assert.That(db.TableExists(table.Name), Is.False, "Use an empty disposable database");
        var jobsProvider = DbJobsProvider.Create(factory);
        var cutoff = new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc);
        var expired = cutoff.AddMonths(-2);
        var failedOnly = cutoff.AddMonths(-1);
        var overlap = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        try
        {
            db.CreateTable<BackgroundJob>();
            db.CreateTable<JobSummary>();
            db.CreateTable<ScheduledTask>();
            db.CreateTable<JobAttempt>();
            if (provider == "postgres")
            {
                db.ExecuteSql(PostgresUtils.CreatePartitionTableSql(factory.DialectProvider, typeof(CompletedJob), nameof(CompletedJob.CreatedDate)));
                db.ExecuteSql(PostgresUtils.CreatePartitionTableSql(factory.DialectProvider, typeof(FailedJob), nameof(FailedJob.CreatedDate)));
                foreach (var date in new[] { expired, failedOnly, overlap })
                {
                    using var monthDb = jobsProvider.OpenMonthDb(date);
                }
                // A failed-only partition must be discovered independently of completed history.
                db.ExecuteSql("DROP TABLE " + factory.DialectProvider.QuoteTable(
                    PostgresUtils.GetMonthTableName(factory.DialectProvider, typeof(CompletedJob), failedOnly)));
            }
            else
            {
                db.CreateTable<CompletedJob>();
                db.CreateTable<FailedJob>();
            }
            db.Insert(new BackgroundJob { RefId = "active", CreatedDate = expired, RequestType = "CMD" });
            db.Insert(new JobSummary { Id = 1, RefId = "summary", CreatedDate = expired, RequestType = "CMD" });
            db.Insert(new ScheduledTask { Name = "schedule", CreatedDate = expired });
            db.Insert(new JobAttempt { JobId = 1, CreatedDate = expired });
            db.Insert(new CompletedJob { Id = 10, RefId = "expired", CreatedDate = expired, RequestType = "CMD" });
            db.Insert(new FailedJob { Id = 11, RefId = "failed-only", CreatedDate = failedOnly, RequestType = "CMD" });
            db.Insert(new CompletedJob { Id = 12, RefId = "overlap", CreatedDate = overlap, RequestType = "CMD" });
            db.Insert(new CompletedJob { Id = 13, RefId = "boundary", CreatedDate = cutoff, RequestType = "CMD" });
            db.Insert(new FailedJob { Id = 14, RefId = "recent-failure", CreatedDate = cutoff.AddDays(1), RequestType = "CMD" });

            Assert.That(jobsProvider.GetTableMonths(db), Does.Contain(new DateTime(2026, 7, 1)));
            jobsProvider.PurgeArchives(cutoff);
            jobsProvider.PurgeArchives(cutoff); // repeatable
            Assert.That(db.Column<long>(db.From<CompletedJob>().Select(x => x.Id)),
                Is.EquivalentTo(provider == "postgres" ? new long[] { 12, 13 } : new long[] { 13 }));
            Assert.That(db.Column<long>(db.From<FailedJob>().Select(x => x.Id)), Is.EquivalentTo(new long[] { 14 }));
            Assert.That(db.Count<BackgroundJob>(), Is.EqualTo(1));
            Assert.That(db.Single<JobSummary>(x => x.Id == 1).RefId, Is.EqualTo("summary"));
            Assert.That(db.Single<ScheduledTask>(x => x.Name == "schedule").Enabled, Is.True);
            Assert.That(db.Count<JobAttempt>(), Is.EqualTo(1));

            if (provider == "postgres")
            {
                // Exactly the end of the overlap month is eligible, including at equality.
                jobsProvider.PurgeArchives(overlap.AddMonths(1));
                Assert.That(db.Count<CompletedJob>(), Is.Zero);
                Assert.That(db.Count<FailedJob>(), Is.Zero);
                // Cleanup must invalidate the local cache so a late write can recreate the partition.
                using var recreated = jobsProvider.OpenMonthDb(expired);
                recreated.Insert(new CompletedJob { Id = 15, RefId = "late", CreatedDate = expired, RequestType = "CMD" });
                Assert.That(recreated.Count<CompletedJob>(), Is.EqualTo(1));
            }
        }
        finally
        {
            db.DropTable<CompletedJob>();
            db.DropTable<FailedJob>();
            db.DropTable<BackgroundJob>();
            db.DropTable<JobSummary>();
            db.DropTable<ScheduledTask>();
            db.DropTable<JobAttempt>();
        }
    }

    [TestCase(null)]
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(90)]
    public void Native_retention_only_expires_history_when_enabled(int? retentionDays)
    {
        var factory = Factory("sqlite");
        using var db = factory.OpenDbConnection();
        db.CreateTable<CompletedJob>();
        db.CreateTable<FailedJob>();
        db.CreateTable<BackgroundJob>();
        db.CreateTable<JobSummary>();
        db.CreateTable<ScheduledTask>();
        db.Insert(new BackgroundJob { RefId = "active" });
        db.Insert(new JobSummary { Id = 1, RefId = "summary" });
        db.Insert(new ScheduledTask { Name = "schedule" });
        var old = DateTime.UtcNow.AddYears(-3);
        // No completed rows: exercises the original failed-only discovery gap through the native path.
        db.Insert(new FailedJob { Id = 1, CreatedDate = old });
        db.Insert(new FailedJob { Id = 2, CreatedDate = DateTime.UtcNow });
        using var services = new ServiceCollection().BuildServiceProvider();
        var feature = new DatabaseJobFeature {
            DbProvider = DbJobsProvider.Create(factory),
            ArchiveRetention = retentionDays.HasValue ? TimeSpan.FromDays(retentionDays.Value) : null,
        };
        var jobs = new RetentionJobs(feature, services);
        jobs.Purge();
        Assert.That(db.Count<FailedJob>(), Is.EqualTo(retentionDays > 0 ? 1 : 2));
        db.Insert(new CompletedJob { Id = 3, CreatedDate = old });
        db.Insert(new CompletedJob { Id = 4, CreatedDate = DateTime.UtcNow });
        // A fresh instance runs immediately, while the first instance is throttled for an hour.
        new RetentionJobs(feature, services).Purge();
        jobs.Purge();
        Assert.That(db.Count<CompletedJob>(), Is.EqualTo(retentionDays > 0 ? 1 : 2));
        Assert.That(db.Count<BackgroundJob>(), Is.EqualTo(1));
        Assert.That(db.Count<JobSummary>(), Is.EqualTo(1));
        Assert.That(db.Count<ScheduledTask>(), Is.EqualTo(1));
    }
    sealed class MonthlyJobs(BackgroundsJobFeature feature, IServiceProvider services)
        : BackgroundJobs(NullLogger<BackgroundJobs>.Instance, feature, feature.DbFactory, services,
            services.GetRequiredService<IServiceScopeFactory>())
    {
        public readonly List<DateTime> Dropped = new();
        protected override List<DateTime> GetArchiveMonths(IDbConnection db) =>
            [new(2026, 6, 1), new(2026, 7, 1), new(2026, 8, 1), new(2026, 9, 1)];
        protected override void DropArchive(DateTime month) => Dropped.Add(month);
        public void PurgeAt(DateTime cutoff) => PurgeArchives(cutoff);
    }

    [TestCase(1)]
    [TestCase(15)]
    public void Monthly_archives_are_removed_only_when_the_whole_month_has_expired(int cutoffDay)
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var feature = new BackgroundsJobFeature { DbFactory = Factory("sqlite"), DialectProvider = (ServiceStack.OrmLite.Sqlite.SqliteOrmLiteDialectProviderBase)SqliteDialect.Provider, ResolveAppDb = factory => factory.OpenDbConnection() };
        var jobs = new MonthlyJobs(feature, services);
        jobs.PurgeAt(new DateTime(2026, 8, cutoffDay, 0, 0, 0, DateTimeKind.Utc));
        Assert.That(jobs.Dropped, Is.EqualTo(new[] { new DateTime(2026, 6, 1), new DateTime(2026, 7, 1) }));
    }

}
