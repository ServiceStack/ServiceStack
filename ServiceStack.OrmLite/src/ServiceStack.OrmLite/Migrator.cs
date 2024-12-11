#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

public class Migration : IMeta
{
    [AutoIncrement]
    public long Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string ConnectionString { get; set; }
    public string? NamedConnection { get; set; }
    [StringLength(StringLengthAttribute.MaxText)]
    public string? Log { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    [StringLength(StringLengthAttribute.MaxText)]
    public string? ErrorStackTrace { get; set; }
    [StringLength(StringLengthAttribute.MaxText)]
    public Dictionary<string, string> Meta { get; set; }
}

public abstract class MigrationBase : IAppTask
{
    public IDbConnectionFactory? DbFactory { get; set; }
    public IDbConnection? Db { get; set; }
    public IDbTransaction? Transaction { get; set; }
    public string? Log { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedDate { get; set; }
    public Exception? Error { get; set; }
    
    /// <summary>
    /// Add additional logs to capture in Migration table
    /// </summary>
    public StringBuilder MigrationLog { get; set; } = new();

    public virtual void AfterOpen() {}
    public virtual void BeforeCommit() {}
    public virtual void BeforeRollback() {}
    public virtual void Up(){}
    public virtual void Down(){}
}

public class Migrator
{
    public const string All = "all";
    public const string Last = "last";
    public IDbConnectionFactory DbFactory { get; }
    public Type[] MigrationTypes { get; }
    
    public Migrator(IDbConnectionFactory dbFactory, params Assembly[] migrationAssemblies)
        : this(dbFactory, GetAllMigrationTypes(migrationAssemblies).ToArray()) {}
    
    public Migrator(IDbConnectionFactory dbFactory, params Type[] migrationTypes)
    {
        DbFactory = dbFactory;
        MigrationTypes = migrationTypes;
        JsConfig.InitStatics();
    }
    
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(10);
    
    public ILog Log { get; set; } = new ConsoleLogger(typeof(Migrator));

    Type? GetNextMigrationToRun(IDbConnection db, List<Type> migrationTypes)
    {
        var completedMigrations = new List<Type>();

        Type? nextRun = null;
        var q = db.From<Migration>()
            .OrderByDescending(x => x.Name);
        // Select all previously run migrations and find the last completed migration
        var runMigrations = db.Select(q);
        var lastRun = runMigrations.FirstOrDefault();
        if (lastRun != null)
        {
            var elapsedTime = DateTime.UtcNow - lastRun.CreatedDate;
            if (lastRun.CompletedDate == null)
            {
                if (elapsedTime < Timeout)
                    throw new InfoException($"Migration '{lastRun.Name}' is still in progress, timeout in {(Timeout - elapsedTime).TotalSeconds:N3}s.");
                
                Log.Info($"Migration '{lastRun.Name}' failed to complete {elapsedTime.TotalSeconds:N3}s ago, re-running...");
                db.DeleteById<Migration>(lastRun.Id);
            }

            // Re-run last migration
            if (lastRun.CompletedDate == null)
            {
                foreach (var migrationType in migrationTypes)
                {
                    if (migrationType.Name != lastRun.Name)
                    {
                        completedMigrations.Add(migrationType);
                    }
                    else
                    {
                        migrationTypes.RemoveAll(x => completedMigrations.Contains(x));
                        return migrationType;
                    }
                }
                return null;
            }
            
            
            // Remove completed migrations
            completedMigrations = migrationTypes.Any(x => x.Name == lastRun.Name) 
                ? migrationTypes.TakeWhile(x => x.Name != lastRun.Name).ToList()
                : new List<Type>();
            
            // Make sure we don't rerun any migrations that have already been run
            foreach (var migrationType in migrationTypes)
            {
                if (runMigrations.Any(x => x.Name == migrationType.Name && x.CompletedDate != null))
                {
                    completedMigrations.Add(migrationType);
                }
            }
            
            if (completedMigrations.Count > 0)
                migrationTypes.RemoveAll(x => completedMigrations.Contains(x));

            var nextMigration = migrationTypes.FirstOrDefault();
            if (nextMigration == null)
                return null;

            // Remove completed migration
            if (nextMigration.Name == lastRun.Name)
                migrationTypes.Remove(nextMigration);
        }
        
        // Return next migration
        return migrationTypes.FirstOrDefault();
    }

    public AppTaskResult Run() => Run(throwIfError:true);
    public AppTaskResult Run(bool throwIfError)
    {
        using var db = DbFactory.Open();
        Init(db);
        var allLogs = new StringBuilder();

        var remainingMigrations = MigrationTypes.ToList();

        LogMigrationsFound(remainingMigrations);

        var startAt = DateTime.UtcNow;
        var migrationsRun = new List<IAppTask>();

        while (true)
        {
            Type? nextRun;
            try
            {
                nextRun = GetNextMigrationToRun(db, remainingMigrations);
                if (nextRun == null)
                    break;
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                if (throwIfError)
                    throw;
                return new AppTaskResult(migrationsRun) { Error = e };
            }
            
            var migrationStartAt = DateTime.UtcNow;

            var descFmt = AppTasks.GetDescFmt(nextRun);
            var namedConnections = nextRun.AllAttributes<NamedConnectionAttribute>().Select(x => x.Name ?? null).ToArray();
            if (namedConnections.Length == 0)
            {
                namedConnections = [null];
            }

            var failedExceptions = new List<Exception>();

            foreach (var namedConnection in namedConnections)
            {
                var namedDesc = namedConnection == null ? "" : $" ({namedConnection})";
                Log.Info($"Running {nextRun.Name}{descFmt}{namedDesc}...");
            
                var migration = new Migration
                {
                    Name = nextRun.Name,
                    Description = AppTasks.GetDesc(nextRun),
                    CreatedDate = DateTime.UtcNow,
                    ConnectionString = OrmLiteUtils.MaskPassword(((OrmLiteConnectionFactory)DbFactory).ConnectionString),
                    NamedConnection = namedConnection,
                };
                var id = db.Insert(migration, selectIdentity:true);

                var instance = Run(DbFactory, nextRun, x => x.Up(), namedConnection);
                migrationsRun.Add(instance);
                Log.Info(instance.Log);

                if (instance.Error == null)
                {
                    Log.Info($"Completed {nextRun.Name}{descFmt} in {(DateTime.UtcNow - migrationStartAt).TotalSeconds:N3}s" +
                             Environment.NewLine);

                    // Record completed migration run in DB
                    db.UpdateOnly(() => new Migration
                    {
                        Log = instance.Log,
                        CompletedDate = DateTime.UtcNow,
                    }, where: x => x.Id == id);
                    remainingMigrations.Remove(nextRun);
                }
                else
                {
                    var e = instance.Error;
                    Log.Error(e.Message, e);
                    
                    // Save Error in DB
                    db.UpdateOnly(() => new Migration
                    {
                        Log = instance.Log,
                        ErrorCode = e.GetType().Name,
                        ErrorMessage = e.Message,
                        ErrorStackTrace = e.StackTrace,
                    }, where: x => x.Id == id);

                    failedExceptions.Add(instance.Error);
                }
            }

            if (failedExceptions.Count > 0)
            {
                if (throwIfError)
                {
                    if (failedExceptions.Count > 1)
                        throw new AggregateException(failedExceptions);
                    
                    throw failedExceptions.First();
                }
                return new AppTaskResult(migrationsRun);
            }
        }

        var migrationsCompleted = migrationsRun.Count(x => x.Error == null);
        if (migrationsCompleted == 0)
        {
            Log.Info("No migrations to run.");
        }
        else
        {
            var migration = migrationsCompleted > 1 ? "migrations" : "migration";
            Log.Info($"{Environment.NewLine}Ran {migrationsCompleted} {migration} in {(DateTime.UtcNow - startAt).TotalSeconds:N3}s");
        }
        return new AppTaskResult(migrationsRun);
    }

    private void LogMigrationsFound(List<Type> remainingMigrations)
    {
        var sb = StringBuilderCache.Allocate()
            .AppendLine("Migrations Found:");
        remainingMigrations.Each(x => sb.AppendLine((string?)$" - {x.Name}"));
        Log.Info(StringBuilderCache.ReturnAndFree(sb));
    }

    public static List<Type> GetAllMigrationTypes(params Assembly[] migrationAssemblies)
    {
        var remainingMigrations = migrationAssemblies
            .SelectMany(x => x.GetTypes().Where(x => x.IsInstanceOf(typeof(MigrationBase)) && !x.IsAbstract))
            .OrderBy(x => x.Name)
            .ToList();
        return remainingMigrations;
    }

    public static void Init(IDbConnection db)
    {
        db.CreateTableIfNotExists<Migration>();
    }

    public static void Recreate(IDbConnection db)
    {
        db.DropAndCreateTable<Migration>();
    }

    public static void Clear(IDbConnection db)
    {
        if (db.TableExists<Migration>())
            db.DeleteAll<Migration>();
        else
            Init(db);
    }
    
    Type? GetNextMigrationRevertToRun(IDbConnection db, List<Type> migrationTypes)
    {
        Type? nextRun = null;
        var q = db.From<Migration>()
            .OrderByDescending(x => x.Name).Limit(1);
        Migration? lastRun = null;
        while (true)
        {
            lastRun = db.Single(q);
            if (lastRun == null)
                return null;
            
            var elapsedTime = DateTime.UtcNow - lastRun.CreatedDate;
            if (lastRun.CompletedDate == null)
            {
                if (elapsedTime < Timeout)
                    throw new InfoException(
                        $"Migration '{lastRun.Name}' is still in progress, timeout in {(Timeout - elapsedTime).TotalSeconds:N3}s.");

                Log.Info($"Migration '{lastRun.Name}' failed to complete {elapsedTime.TotalSeconds:N3}s ago, ignoring...");
                db.DeleteById<Migration>(lastRun.Id);
                continue;
            }

            var nextMigration = migrationTypes.FirstOrDefault(x => x.Name == lastRun.Name);
            if (nextMigration == null)
                throw new InfoException($"Could not find Migration '{lastRun.Name}' to revert, aborting.");

            return nextMigration;
        }
    }

    public AppTaskResult Rerun(string? migrationName)
    {
        Revert(migrationName, throwIfError: true);
        if (migrationName is Last or All)
        {
            return Run(throwIfError: true);
        }

        var migrationType = MigrationTypes.FirstOrDefault(x => x.Name == migrationName);
        if (migrationType == null)
            throw new InfoException($"Could not find Migration '{migrationName}' to rerun, aborting.");
        var migration = Run(DbFactory, migrationType, x => x.Up());
        return new AppTaskResult([migration]);
    }

    public AppTaskResult Revert(string? migrationName) => Revert(migrationName, throwIfError:true);
    public AppTaskResult Revert(string? migrationName, bool throwIfError)
    {
        using var db = DbFactory.Open();
        Init(db);
        
        var allMigrationTypes = MigrationTypes.ToList();
        allMigrationTypes.Reverse();

        LogMigrationsFound(allMigrationTypes);

        var startAt = DateTime.UtcNow;
        var migrationsRun = new List<IAppTask>();

        Log.Info($"Reverting {migrationName}...");

        migrationName = migrationName switch {
            All => allMigrationTypes.LastOrDefault()?.Name,
            Last => allMigrationTypes.FirstOrDefault()?.Name,
            _ => migrationName
        };

        if (migrationName != null)
        {
            while (true)
            {
                Type? nextRun;
                try
                {
                    nextRun = GetNextMigrationRevertToRun(db, allMigrationTypes);
                    if (nextRun == null)
                        break;
                }
                catch (Exception e)
                {
                    Log.Error(e.Message);
                    if (throwIfError)
                        throw;
                    return new AppTaskResult(migrationsRun) { Error = e };
                }
            
                var migrationStartAt = DateTime.UtcNow;

                var namedConnections = nextRun.AllAttributes<NamedConnectionAttribute>().Select(x => x.Name ?? null).ToArray();
                if (namedConnections.Length == 0)
                {
                    namedConnections = [null];
                }

                var failedExceptions = new List<Exception>();

                foreach (var namedConnection in namedConnections)
                {
                    var descFmt = AppTasks.GetDescFmt(nextRun);
                    var namedDesc = namedConnection == null ? "" : $" ({namedConnection})";
                    Log.Info($"Reverting {nextRun.Name}{descFmt}{namedDesc}...");

                    var instance = Run(DbFactory, nextRun, x => x.Down(), namedConnection);
                    migrationsRun.Add(instance);
                    Log.Info(instance.Log);

                    if (instance.Error == null)
                    {
                        Log.Info($"Completed revert of {nextRun.Name}{descFmt} in {(DateTime.UtcNow - migrationStartAt).TotalSeconds:N3}s" +
                                 Environment.NewLine);

                        // Remove completed migration revert from DB
                        db.Delete<Migration>(x => x.Name == nextRun.Name && x.NamedConnection == namedConnection);
                    }
                    else
                    {
                        Log.Error(instance.Error.Message, instance.Error);
                        failedExceptions.Add(instance.Error);
                    }
                }

                if (failedExceptions.Count > 0)
                {
                    if (throwIfError)
                    {
                        if (failedExceptions.Count > 1)
                            throw new AggregateException(failedExceptions);
                    
                        throw failedExceptions.First();
                    }
                    return new AppTaskResult(migrationsRun);
                }

                if (migrationName == nextRun.Name)
                    break;
            }
        }
        
        var migrationsCompleted = migrationsRun.Count(x => x.Error == null);
        if (migrationsCompleted == 0)
        {
            Log.Info("No migrations were reverted.");
        }
        else
        {
            var migration = migrationsCompleted > 1 ? "migrations" : "migration";
            Log.Info($"{Environment.NewLine}Reverted {migrationsCompleted} {migration} in {(DateTime.UtcNow - startAt).TotalSeconds:N3}s");
        }
        return new AppTaskResult(migrationsRun);
    }

    public static AppTaskResult Down(IDbConnectionFactory dbFactory, Type migrationType) => Down(dbFactory, new[] { migrationType });
    public static AppTaskResult Down(IDbConnectionFactory dbFactory, Type[] migrationTypes) =>
        RunAll(dbFactory, migrationTypes, x => x.Down());
    public static AppTaskResult Up(IDbConnectionFactory dbFactory, Type migrationType) => Up(dbFactory, new[] { migrationType });
    public static AppTaskResult Up(IDbConnectionFactory dbFactory, Type[] migrationTypes) =>
        RunAll(dbFactory, migrationTypes, x => x.Up());

    public static MigrationBase Run(IDbConnectionFactory dbFactory, Type nextRun, Action<MigrationBase> migrateAction, string? namedConnection = null)
    {
        var holdFilter = OrmLiteConfig.BeforeExecFilter;
        var instance = nextRun.CreateInstance<MigrationBase>();
        OrmLiteConfig.BeforeExecFilter = dbCmd => instance.MigrationLog.AppendLine(dbCmd.GetDebugString());

        IDbConnection? useDb = null;
        IDbTransaction? trans = null;

        try
        {
            useDb = namedConnection == null
                ? dbFactory.OpenDbConnection()
                : dbFactory.OpenDbConnection(namedConnection);

            instance.DbFactory = dbFactory;
            instance.Db = useDb;
            trans = useDb.OpenTransaction();
            instance.AfterOpen();

            instance.Transaction = trans;
            instance.StartedAt = DateTime.UtcNow;

            // Run Migration
            migrateAction(instance);

            instance.CompletedDate = DateTime.UtcNow;
            instance.Log = instance.MigrationLog.ToString();

            instance.BeforeCommit();
            trans.Commit();
            trans.Dispose();
            trans = null;
        }
        catch (Exception e)
        {
            instance.CompletedDate = DateTime.UtcNow;
            instance.Error = e;
            instance.Log = instance.MigrationLog.ToString();
            try
            {
                instance.BeforeRollback();
                trans?.Rollback();
                trans?.Dispose();
            }
            catch (Exception exRollback)
            {
                instance.Log += Environment.NewLine + exRollback.Message;
            }
        }
        finally
        {
            instance.Db = null;
            instance.Transaction = null;
            OrmLiteConfig.BeforeExecFilter = holdFilter;
            try
            {
                useDb?.Dispose();
            }
            catch (Exception exRollback)
            {
                instance.Log += Environment.NewLine + exRollback.Message;
            }
        }
        return instance;        
    }

    public static AppTaskResult RunAll(IDbConnectionFactory dbFactory, IEnumerable<Type> migrationTypes, Action<MigrationBase> migrateAction)
    {
        var migrationsRun = new List<IAppTask>();
        foreach (var nextRun in migrationTypes)
        {
            var instance = Run(dbFactory, nextRun, migrateAction);
            migrationsRun.Add(instance);
            if (instance.Error != null)
                break;
        }
        return new AppTaskResult(migrationsRun);
    }
    
}