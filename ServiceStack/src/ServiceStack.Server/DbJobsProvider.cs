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
    public void DefaultConfigureDb(IDbConnection db) => db.WithName(GetType().Name);

    public virtual void InitSchema()
    {
        using var db = OpenDb();
        InitSchema(db);
    }

    public virtual void InitSchema(IDbConnection db)
    {
        db.CreateTableIfNotExists<BackgroundJob>();
        db.CreateTableIfNotExists<JobSummary>();
        db.CreateTableIfNotExists<ScheduledTask>();

        using var monthDb = OpenMonthDb(DateTime.UtcNow);
        InitMonthDbSchema(monthDb);
    }
    
    public virtual void InitMonthDbSchema(IDbConnection db)
    {
        db.CreateTableIfNotExists<CompletedJob>();
        db.CreateTableIfNotExists<FailedJob>();
    }

    public virtual IDbConnection OpenDb()
    {
        var db = NamedConnection != null
            ? DbFactory.OpenDbConnection(NamedConnection)
            : DbFactory.OpenDbConnection();
        ConfigureDb?.Invoke(db);
        return db;
    }
    
    public virtual IDbConnection OpenMonthDb(DateTime createdDate)
    {
        return OpenDb();
    }

    public static DbJobsProvider Create(IDbConnectionFactory dbFactory, string? namedConnection = null)
    {
        var dialect = dbFactory.GetDialectProvider(namedConnection: namedConnection);
        var typeName = dialect.GetType().Name;
        var dbProvider = typeName.StartsWith("Postgre")
            ? new PostgresDbJobsProvider()
            :  typeName.StartsWith("MySql") || typeName.StartsWith("Maria")
                ? new MySqlDbJobsProvider()
                : typeName.StartsWith("SqlServer")
                    ? new SqlServerDbJobsProvider()
                    : new DbJobsProvider();
        dbProvider.DbFactory = dbFactory;
        dbProvider.NamedConnection = namedConnection;
        dbProvider.Dialect = dialect;
        dbProvider.ConfigureDb = dbProvider.DefaultConfigureDb;
        return dbProvider;
    }

    public virtual List<DateTime> GetTableMonths(IDbConnection db)
    {
        var q = db.From<CompletedJob>();
        var dateTimeColumn = q.Column<CompletedJob>(c => c.CreatedDate);
        var months = db.SqlColumn<string>(q
            .Select(x => new {
                Month = SqlDateFormat(dateTimeColumn, "%Y-%m"),
            }));

        var ret = months
            .Where(x => x.Contains('_'))
            .Select(x => 
                DateTime.TryParse(x.RightPart('_').LeftPart('.') + "-01", out var date) ? date : (DateTime?)null)
            .Where(x => x != null)
            .Select(x => x!.Value)
            .OrderDescending()
            .ToList();
        
        return ret;
    }

    public virtual string SqlDateFormat(string quotedColumn, string format) => SqliteUtils.SqlDateFormat(quotedColumn, format);

    public virtual string SqlChar(int charCode) => $"CHAR({charCode})";

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
    public override string SqlDateFormat(string quotedColumn, string format) => MySqlUtils.SqlDateFormat(quotedColumn, format);
}

public class SqlServerDbJobsProvider : DbJobsProvider
{
    public override string SqlDateFormat(string quotedColumn, string format) => SqlServerUtils.SqlDateFormat(quotedColumn, format);
}

public class PostgresDbJobsProvider : DbJobsProvider
{
    ConcurrentDictionary<string, bool> monthDbs = new();

    public override void InitSchema(IDbConnection db)
    {
        db.CreateTableIfNotExists<BackgroundJob>();
        db.CreateTableIfNotExists<JobSummary>();
        db.CreateTableIfNotExists<ScheduledTask>();

        var completedSql = PostgresUtils.CreatePartitionTableSql(Dialect, typeof(CompletedJob), nameof(CompletedJob.CreatedDate));
        db.Execute(completedSql);

        var failedSql = PostgresUtils.CreatePartitionTableSql(Dialect, typeof(FailedJob), nameof(FailedJob.CreatedDate));
        db.Execute(failedSql);

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
        if (monthDbs.TryAdd(partTableName, true))
        {
            db.ExecuteSql(PostgresUtils.CreatePartitionSql(Dialect, typeof(CompletedJob), createdDate));
            db.ExecuteSql(PostgresUtils.CreatePartitionSql(Dialect, typeof(FailedJob), createdDate));
        }
        return db;
    }

    public override List<DateTime> GetTableMonths(IDbConnection db)
    {
        return PostgresUtils.GetTableMonths(Dialect, db, typeof(CompletedJob));
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