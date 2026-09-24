#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Data;
using ServiceStack.Jobs;
using ServiceStack.Logging;
using ServiceStack.OrmLite;

namespace ServiceStack;

/// <summary>Additive schema updates shared by the SQLite and RDBMS job providers.</summary>
public static class BackgroundJobSchema
{
    private static ILog Log => LogManager.GetLogger(typeof(BackgroundJobSchema));

    private static readonly string[] JobColumns =
    [
        nameof(BackgroundJob.Queue), nameof(BackgroundJob.Priority),
        nameof(BackgroundJob.RetryBackoff), nameof(BackgroundJob.RetryDelayMs),
        nameof(BackgroundJob.MaxRetryDelayMs),
        nameof(BackgroundJob.LogsTruncated),
        nameof(BackgroundJob.CancelRequestedDate),
        nameof(BackgroundJob.ExpiresAt),
        nameof(BackgroundJob.LeaseOwner),
        nameof(BackgroundJob.DependsOnBatch),
        nameof(BackgroundJob.ConcurrencyKey),
        nameof(BackgroundJob.DependsOnPolicy),
        nameof(BackgroundJob.TenantId),
        nameof(BackgroundJob.TraceId),
    ];

    private static readonly string[] SummaryColumns =
    [
        nameof(JobSummary.Queue), nameof(JobSummary.Priority), nameof(JobSummary.RunAfter),
        nameof(JobSummary.LogsTruncated),
        nameof(JobSummary.CancelRequestedDate),
        nameof(JobSummary.ExpiresAt),
        nameof(JobSummary.ConcurrencyKey),
        nameof(JobSummary.SingletonKey),
        nameof(JobSummary.TenantId),
        nameof(JobSummary.TraceId),
        nameof(JobSummary.LeaseOwner),
        nameof(JobSummary.Meta),
    ];

    private static readonly string[] ActiveJobColumns =
    [
        nameof(BackgroundJob.SingletonKey),
        nameof(BackgroundJob.LeaseToken), nameof(BackgroundJob.LeaseExpiresAt),
    ];

    private static readonly string[] QueueColumns =
    [
        nameof(JobQueue.RateLimit), nameof(JobQueue.RateLimitSecs),
        nameof(JobQueue.RateLimitWindowStart), nameof(JobQueue.RateLimitCount),
        nameof(JobQueue.Meta),
    ];

    private static readonly string[] BatchColumns =
    [
        nameof(JobBatch.OnSuccess), nameof(JobBatch.ParentBatchId), nameof(JobBatch.CancelledDate),
    ];

    private static readonly string[] NodeColumns =
    [
        nameof(JobNode.Draining), nameof(JobNode.Queues), nameof(JobNode.Meta),
    ];

    private static readonly string[] ScheduledTaskColumns =
    [
        nameof(ScheduledTask.Enabled), nameof(ScheduledTask.NextRun), nameof(ScheduledTask.TimeZoneId),
        nameof(ScheduledTask.MisfirePolicy), nameof(ScheduledTask.OverlapPolicy),
        nameof(ScheduledTask.LastErrorCode), nameof(ScheduledTask.LastErrorMessage),
        nameof(ScheduledTask.LastRunState), nameof(ScheduledTask.LastRunDurationMs),
        nameof(ScheduledTask.CreatedDate), nameof(ScheduledTask.ModifiedDate),
        nameof(ScheduledTask.StartDate), nameof(ScheduledTask.EndDate),
        nameof(ScheduledTask.MaxRuns), nameof(ScheduledTask.RunCount),
        nameof(ScheduledTask.Meta),
    ];

    public static void UpgradeMainDb(IDbConnection db)
    {
        var hasExistingJobs = db.TableExists<BackgroundJob>();
        var needsUpgrade = hasExistingJobs && !db.ColumnExists<BackgroundJob>(x => x.SingletonKey);
        var hasExistingSchedules = db.TableExists<ScheduledTask>();
        var needsScheduleUpgrade = hasExistingSchedules && !db.ColumnExists<ScheduledTask>(x => x.Enabled);

        db.CreateTableIfNotExists<BackgroundJob>();
        db.CreateTableIfNotExists<JobSummary>();
        db.CreateTableIfNotExists<ScheduledTask>();
        db.CreateTableIfNotExists<JobBatch>();
        db.CreateTableIfNotExists<JobQueue>();
        db.CreateTableIfNotExists<JobConcurrencyLock>();
        db.CreateTableIfNotExists<JobNode>();
        db.CreateTableIfNotExists<JobAttempt>();

        if (needsUpgrade)
        {
            // This release intentionally starts with an empty execution queue. Preserve
            // history, but make the fate of incomplete jobs visible to customers.
            using var transaction = db.OpenTransaction();
            var dialect = db.GetDialectProvider();
            var jobs = dialect.GetQuotedTableName(typeof(BackgroundJob));
            var summaries = dialect.GetQuotedTableName(typeof(JobSummary));
            var id = dialect.GetQuotedColumnName(nameof(BackgroundJob.Id));
            var state = dialect.GetQuotedColumnName(nameof(JobSummary.State));
            var completed = dialect.GetQuotedColumnName(nameof(JobSummary.CompletedDate));
            var errorCode = dialect.GetQuotedColumnName(nameof(JobSummary.ErrorCode));
            var errorMessage = dialect.GetQuotedColumnName(nameof(JobSummary.ErrorMessage));
            db.ExecuteSql($"UPDATE {summaries} SET {state}=@state, {completed}=@completed, " +
                $"{errorCode}=@errorCode, {errorMessage}=@errorMessage WHERE {id} IN (SELECT {id} FROM {jobs})",
                new {
                    state = BackgroundJobState.Cancelled.ToString(),
                    completed = DateTime.UtcNow,
                    errorCode = JobErrorCodes.QueueClearedOnUpgrade,
                    errorMessage = "Incomplete job cleared when Background Jobs was upgraded",
                });
            db.DeleteAll<BackgroundJob>();
            transaction.Commit();
        }

        AddMissingColumns<BackgroundJob>(db, JobColumns);
        AddMissingColumns<BackgroundJob>(db, ActiveJobColumns);
        AddMissingColumns<JobSummary>(db, SummaryColumns);
        AddMissingColumns<ScheduledTask>(db, ScheduledTaskColumns);
        AddMissingColumns<JobQueue>(db, QueueColumns);
        AddMissingColumns<JobBatch>(db, BatchColumns);
        AddMissingColumns<JobNode>(db, NodeColumns);
        if (needsScheduleUpgrade)
            db.UpdateOnly(() => new ScheduledTask { Enabled = true }, where: x => !x.Enabled);

        AddMissingIndexes(db);
    }

    public static void UpgradeArchiveDb(IDbConnection db)
    {
        db.CreateTableIfNotExists<CompletedJob>();
        db.CreateTableIfNotExists<FailedJob>();
        AddMissingColumns<CompletedJob>(db, JobColumns);
        AddMissingColumns<FailedJob>(db, JobColumns);
    }

    /// <summary>
    /// Indexes are only created by CreateTableIfNotExists() so they need to be added explicitly
    /// for existing databases that are upgraded in place.
    /// </summary>
    private static void AddMissingIndexes(IDbConnection db)
    {
        // Claim scan: the Job selected next is ordered by Priority within everything that's due
        CreateIndex(db, "idx_backgroundjob_claim", typeof(BackgroundJob),
            [nameof(BackgroundJob.State), nameof(BackgroundJob.RunAfter), nameof(BackgroundJob.Priority)]);
        // Failover scan: finding Jobs whose lease has expired
        CreateIndex(db, "idx_backgroundjob_lease", typeof(BackgroundJob),
            [nameof(BackgroundJob.LeaseExpiresAt)]);
        // "What is this server running?"
        CreateIndex(db, "idx_backgroundjob_owner", typeof(BackgroundJob),
            [nameof(BackgroundJob.LeaseOwner)]);
        // Enforces at most 1 active Job per SingletonKey
        CreateIndex(db, "uidx_backgroundjob_singleton", typeof(BackgroundJob),
            [nameof(BackgroundJob.SingletonKey)], unique:true, ignoreNulls:true, required:true);
        // Admin UI filters
        CreateIndex(db, "idx_jobsummary_state", typeof(JobSummary),
            [nameof(JobSummary.State), nameof(JobSummary.CreatedDate)]);
        CreateIndex(db, "idx_jobsummary_queue", typeof(JobSummary),
            [nameof(JobSummary.Queue)]);
        // Batch progress is counted by grouping JobSummary on BatchId
        CreateIndex(db, "idx_jobsummary_batch", typeof(JobSummary),
            [nameof(JobSummary.BatchId)]);
        CreateIndex(db, "idx_jobsummary_tag", typeof(JobSummary),
            [nameof(JobSummary.Tag)]);
        CreateIndex(db, "idx_jobsummary_singleton", typeof(JobSummary),
            [nameof(JobSummary.SingletonKey)]);
        CreateIndex(db, "idx_jobsummary_tenant", typeof(JobSummary),
            [nameof(JobSummary.TenantId)]);
        CreateIndex(db, "idx_jobbatch_parent", typeof(JobBatch),
            [nameof(JobBatch.ParentBatchId)]);
        // Expiry sweep
        CreateIndex(db, "idx_backgroundjob_expires", typeof(BackgroundJob),
            [nameof(BackgroundJob.ExpiresAt)]);
        // Serialising Jobs that share a ConcurrencyKey
        CreateIndex(db, "idx_backgroundjob_concurrency", typeof(BackgroundJob),
            [nameof(BackgroundJob.ConcurrencyKey)]);
        // Jobs waiting on a whole Batch
        CreateIndex(db, "idx_backgroundjob_dependsonbatch", typeof(BackgroundJob),
            [nameof(BackgroundJob.DependsOnBatch)]);
        // Reclaiming concurrency slots held by a node that died
        CreateIndex(db, "idx_jobconcurrencylock_expires", typeof(JobConcurrencyLock),
            [nameof(JobConcurrencyLock.ExpiresAt)]);
        CreateIndex(db, "idx_jobnode_heartbeat", typeof(JobNode),
            [nameof(JobNode.LastHeartbeat)]);
        // Scheduler scan
        CreateIndex(db, "idx_scheduledtask_due", typeof(ScheduledTask),
            [nameof(ScheduledTask.Enabled), nameof(ScheduledTask.NextRun)]);
    }

    /// <param name="ignoreNulls">
    /// Whether more than 1 row without a value is allowed in a UNIQUE index, see GetCreateIndexSql().
    /// </param>
    /// <param name="required">
    /// Whether a failure to create the index should fail startup. Only true for indexes that
    /// enforce a correctness guarantee rather than just improving performance.
    /// </param>
    private static void CreateIndex(IDbConnection db, string indexName, Type modelType, string[] fieldNames,
        bool unique = false, bool ignoreNulls = false, bool required = false)
    {
        var dialect = db.GetDialectProvider();
        var model = modelType.GetModelMetadata();
        var table = new TableRef(model);
        var columns = new List<string>();
        foreach (var fieldName in fieldNames)
        {
            var field = model.GetFieldDefinition(fieldName);
            columns.Add(dialect.GetQuotedColumnName(field.FieldName));
        }

        try
        {
            if (IndexExists(db, indexName, table))
                return;
            db.ExecuteSql(GetCreateIndexSql(dialect, indexName, model, columns, unique, ignoreNulls));
        }
        catch (Exception e)
        {
            if (IndexExists(db, indexName, table))
                return; // A second application instance created the same index
            if (required)
                throw;
            // Indexes that only improve performance shouldn't prevent the app from starting,
            // e.g. when the connection's user doesn't have DDL permissions.
            Log.Warn($"Could not create index {indexName} on {table.Name}: {e.Message}", e);
        }
    }

    public static string GetCreateIndexSql(IOrmLiteDialectProvider dialect, string indexName,
        ModelDefinition model, List<string> columns, bool unique, bool ignoreNulls)
    {
        var uniqueSql = unique ? "UNIQUE " : "";
        var tableSql = dialect.GetQuotedTableName(model);
        var columnsSql = string.Join(", ", columns);
        return dialect.Kind switch
        {
            // Only SQLite and PostgreSQL support IF NOT EXISTS, the rest are guarded by IndexExists()
            DbKind.Sqlite or DbKind.PostgreSql =>
                $"CREATE {uniqueSql}INDEX IF NOT EXISTS {indexName} ON {tableSql} ({columnsSql})",
            // SQL Server treats NULLs as equal in a UNIQUE index, so it needs a filtered index to
            // allow more than 1 row without a value
            DbKind.SqlServer when unique && ignoreNulls =>
                $"CREATE {uniqueSql}INDEX {indexName} ON {tableSql} ({columnsSql}) " +
                $"WHERE {columns[0]} IS NOT NULL",
            _ => $"CREATE {uniqueSql}INDEX {indexName} ON {tableSql} ({columnsSql})",
        };
    }

    private static bool IndexExists(IDbConnection db, string indexName, TableRef table)
    {
        try
        {
            var dialect = db.GetDialectProvider();
            var count = dialect.Kind switch
            {
                DbKind.Sqlite => db.SqlScalar<int>(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND lower(name)=lower(@indexName)",
                    new { indexName }),
                DbKind.PostgreSql => db.SqlScalar<int>(
                    "SELECT COUNT(*) FROM pg_indexes WHERE lower(indexname)=lower(@indexName)",
                    new { indexName }),
                DbKind.SqlServer => db.SqlScalar<int>(
                    "SELECT COUNT(*) FROM sys.indexes WHERE name=@indexName",
                    new { indexName }),
                DbKind.MySql => db.SqlScalar<int>(
                    "SELECT COUNT(*) FROM information_schema.statistics " +
                    "WHERE table_schema=DATABASE() AND table_name=@tableName AND index_name=@indexName",
                    new { tableName = table.Name, indexName }),
                _ => 0,
            };
            return count > 0;
        }
        catch (Exception e)
        {
            Log.Warn($"Could not check if index {indexName} exists: {e.Message}", e);
            return false;
        }
    }

    private static void AddMissingColumns<T>(IDbConnection db, string[] fieldNames)
    {
        var model = typeof(T).GetModelMetadata();
        var dialect = db.GetDialectProvider();
        foreach (var fieldName in fieldNames)
        {
            var field = model.GetFieldDefinition(fieldName);
            var columnName = field.Alias != null
                ? dialect.NamingStrategy.GetAlias(field.Alias)
                : dialect.NamingStrategy.GetColumnName(field.Name);
            var table = new TableRef(model);
            if (db.ColumnExists(columnName, table))
                continue;
            try
            {
                db.AddColumn(typeof(T), field);
            }
            catch (Exception e)
            {
                // Checked in the catch body, not a `catch when` filter: a filter runs before the
                // failed statement's transaction has been unwound.
                if (db.ColumnExists(columnName, table))
                    continue; // A second application instance made the same additive change
                Log.Error($"Could not add column {columnName} to {table.Name}: {e.Message}", e);
                throw;
            }
        }
    }
}
#endif
