#if NET8_0_OR_GREATER
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ServiceStack.Data;
using ServiceStack.Jobs;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Dapper;

namespace ServiceStack;

public class DbJobsProvider
{
    public IDbConnectionFactory DbFactory { get; set; }
    public string? NamedConnection { get; set; }
    public IOrmLiteDialectProvider Dialect { get; set; }
    public Action<IDbConnection>? ConfigureDb { get; set; }
    public void DefaultConfigureDb(IDbConnection db) => db.WithTag(GetType().Name);

    public DbJobsProvider()
    {
        ConfigureDb = DefaultConfigureDb;
    }
    
    public virtual void InitSchema()
    {
        using var db = OpenDb();
        InitSchema(db);
    }

    public virtual void InitSchema(IDbConnection db)
    {
        BackgroundJobSchema.UpgradeMainDb(db);

        using var monthDb = OpenMonthDb(DateTime.UtcNow);
        InitMonthDbSchema(monthDb);
    }
    
    public virtual void InitMonthDbSchema(IDbConnection db)
    {
        BackgroundJobSchema.UpgradeArchiveDb(db);
    }

    public virtual IDbConnection OpenDb()
    {
        var db = NamedConnection != null
            ? DbFactory.OpenDbConnection(NamedConnection, ConfigureDb)
            : DbFactory.OpenDbConnection(ConfigureDb);
        return db;
    }
    
    public virtual IDbConnection OpenMonthDb(DateTime createdDate)
    {
        return OpenDb();
    }

    public static DbJobsProvider Create(IDbConnectionFactory dbFactory, string? namedConnection = null)
    {
        var dialect = dbFactory.GetDialectProvider(namedConnection: namedConnection);
        var dbProvider = dialect.Kind switch
        {
            DbKind.PostgreSql => new PostgresDbJobsProvider(),
            DbKind.MySql => new MySqlDbJobsProvider(),
            DbKind.SqlServer => new SqlServerDbJobsProvider(),
            _ => new DbJobsProvider()
        };
        dbProvider.DbFactory = dbFactory;
        dbProvider.NamedConnection = namedConnection;
        dbProvider.Dialect = dialect;
        dbProvider.ConfigureDb ??= dbProvider.DefaultConfigureDb;
        return dbProvider;
    }

    public virtual List<DateTime> GetTableMonths(IDbConnection db)
    {
        var completed = db.From<CompletedJob>();
        var failed = db.From<FailedJob>();
        var months = db.SqlColumn<string>(completed.SelectDistinct(
                SqlDateFormat(completed.Column<CompletedJob>(x => x.CreatedDate), "%Y-%m")))
            .Concat(db.SqlColumn<string>(failed.SelectDistinct(
                SqlDateFormat(failed.Column<FailedJob>(x => x.CreatedDate), "%Y-%m"))));

        var ret = months
            .Select(x => {
                var str = x.StripDbQuotes();
                if (str.Contains('_'))
                {
                    str = str.RightPart('_').LeftPart('.').Replace('_', '-');
                }
                return DateTime.TryParse(str + "-01", out var date) ? date : (DateTime?)null;
            })
            .Where(x => x != null)
            .Select(x => x!.Value)
            .Distinct()
            .OrderDescending()
            .ToList();
        
        return ret;
    }

    public virtual string SqlDateFormat(string quotedColumn, string format) => SqliteUtils.SqlDateFormat(quotedColumn, format);

    public virtual string SqlChar(int charCode) => $"CHAR({charCode})";

    /// <summary>
    /// Whether this RDBMS can skip rows another transaction has locked, which lets competing nodes
    /// select disjoint Jobs in one query instead of contending on the same rows.
    /// </summary>
    public virtual bool SupportsSkipLocked => false;

    /// <summary>
    /// Appends the row-locking clause used to claim Jobs. Only called when SupportsSkipLocked.
    /// </summary>
    public virtual string SqlSkipLocked() => "";

    /// <summary>
    /// Deletes expired history in shared tables without changing the Jobs schema or operational data.
    /// Partitioned providers override this to remove only whole expired archives.
    /// </summary>
    public virtual void PurgeArchives(DateTime cutoffUtc)
    {
        using var db = OpenDb();
        using var trans = db.OpenTransaction();
        db.Delete<CompletedJob>(x => x.CreatedDate < cutoffUtc);
        db.Delete<FailedJob>(x => x.CreatedDate < cutoffUtc);
        trans.Commit();
    }

    /// <summary>Deletes only the history created in the specified month.</summary>
    public virtual void DropArchive(DateTime month)
    {
        var start = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        using var db = OpenDb();
        using var trans = db.OpenTransaction();
        db.Delete<CompletedJob>(x => x.CreatedDate >= start && x.CreatedDate < end);
        db.Delete<FailedJob>(x => x.CreatedDate >= start && x.CreatedDate < end);
        trans.Commit();
    }

    /// <summary>Explicit schema teardown. Must never be used for history retention.</summary>
    public virtual void DropTables(DateTime? createdDate=null)
    {
        using var db = OpenDb();
        db.DropTable<BackgroundJob>();
        db.DropTable<JobSummary>();
        db.DropTable<ScheduledTask>();

        createdDate ??= DateTime.UtcNow;
        using var dbMonth = OpenMonthDb(createdDate.Value);
        dbMonth.DropTable<CompletedJob>();
        dbMonth.DropTable<FailedJob>();
    }
}

public class MySqlDbJobsProvider : DbJobsProvider
{
    // MySql 8.0+
    public override bool SupportsSkipLocked => true;
    public override string SqlSkipLocked() => " FOR UPDATE SKIP LOCKED";

    public override string SqlDateFormat(string quotedColumn, string format) => MySqlUtils.SqlDateFormat(quotedColumn, format);
}

public class SqlServerDbJobsProvider : DbJobsProvider
{
    public override string SqlDateFormat(string quotedColumn, string format) => SqlServerUtils.SqlDateFormat(quotedColumn, format);
}

public class PostgresDbJobsProvider : DbJobsProvider
{
    public override bool SupportsSkipLocked => true;
    public override string SqlSkipLocked() => " FOR UPDATE SKIP LOCKED";

    ConcurrentDictionary<string, bool> monthDbs = new();

    public override void InitSchema(IDbConnection db)
    {
        BackgroundJobSchema.UpgradeMainDb(db);

        var completedSql = PostgresUtils.CreatePartitionTableSql(Dialect, typeof(CompletedJob), nameof(CompletedJob.CreatedDate));
        db.Execute(completedSql);

        var failedSql = PostgresUtils.CreatePartitionTableSql(Dialect, typeof(FailedJob), nameof(FailedJob.CreatedDate));
        db.Execute(failedSql);

        BackgroundJobSchema.UpgradeArchiveDb(db);

        using var monthDb = OpenMonthDb(DateTime.UtcNow);
    }

    public override void InitMonthDbSchema(IDbConnection db)
    {
        // Do nothing, already handled in InitSchema() + OpenMonthDb()
    }

    public override IDbConnection OpenMonthDb(DateTime createdDate)
    {
        var partTableName = PostgresUtils.GetMonthTableName(Dialect, typeof(CompletedJob), createdDate);
        var db = OpenDb();
        ConfigureDb?.Invoke(db);
        lock (monthDbs)
        {
            if (!monthDbs.ContainsKey(partTableName))
            {
                db.ExecuteSql(PostgresUtils.CreatePartitionSql(Dialect, typeof(CompletedJob), createdDate));
                db.ExecuteSql(PostgresUtils.CreatePartitionSql(Dialect, typeof(FailedJob), createdDate));
                monthDbs[partTableName] = true;
            }
        }
        return db;
    }

    public override List<DateTime> GetTableMonths(IDbConnection db)
    {
        return PostgresUtils.GetTableMonths(Dialect, db, typeof(CompletedJob))
            .Concat(PostgresUtils.GetTableMonths(Dialect, db, typeof(FailedJob)))
            .Distinct().OrderDescending().ToList();
    }
    
    public override void PurgeArchives(DateTime cutoffUtc)
    {
        using var db = OpenDb();
        foreach (var month in GetTableMonths(db))
        {
            if (month.AddMonths(1) <= cutoffUtc)
                DropArchive(month);
        }
    }

    public override void DropArchive(DateTime month)
    {
        // Do not open a month DB here: that would create partitions during cleanup.
        var completed = PostgresUtils.GetMonthTableName(Dialect, typeof(CompletedJob), month);
        var failed = PostgresUtils.GetMonthTableName(Dialect, typeof(FailedJob), month);
        lock (monthDbs)
        {
            using var db = OpenDb();
            using var trans = db.OpenTransaction();
            db.ExecuteSql($"DROP TABLE IF EXISTS {Dialect.QuoteTable(completed)}");
            db.ExecuteSql($"DROP TABLE IF EXISTS {Dialect.QuoteTable(failed)}");
            trans.Commit();
            monthDbs.TryRemove(completed, out _);
        }
    }

    public override void DropTables(DateTime? createdDate=null)
    {
        using var db = OpenDb();
        db.DropTable<BackgroundJob>();
        db.DropTable<JobSummary>();
        db.DropTable<ScheduledTask>();

        createdDate ??= DateTime.UtcNow;
        var dbMonth = db;
        dbMonth.DropTable<CompletedJob>();
        dbMonth.DropTable<FailedJob>();

        monthDbs.Clear();
    }
    
    public override string SqlDateFormat(string quotedColumn, string format) => PostgresUtils.SqlDateFormat(quotedColumn, format);

    public override string SqlChar(int charCode) => $"CHR({charCode})";
}
#endif
