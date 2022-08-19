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

namespace ServiceStack.OrmLite.Tests.Migrations;

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
    public string? Log { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorStackTrace { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}

public abstract class MigrationBase
{
    public IDbConnectionFactory? DbFactory { get; set; }
    public IDbConnection? Db { get; set; }
    public IDbTransaction? Transaction { get; set; }
    public string? Log { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedDate { get; set; }
    public Exception? Error { get; set; }

    public virtual void AfterOpen() {}
    public virtual void BeforeCommit() {}
    public virtual void BeforeRollback() {}
    public virtual void Up(){}
    public virtual void Down(){}
}

public class MigrationResult
{
    public MigrationResult(List<MigrationBase> migrationsRun)
    {
        MigrationsRun = migrationsRun;
        TypesCompleted = migrationsRun.Where(x => x.Error == null).Map(x => x.GetType());
    }

    public string GetLogs()
    {
        var sb = StringBuilderCache.Allocate();
        foreach (var instance in MigrationsRun)
        {
            var migrationType = instance.GetType();
            var descFmt = Migrator.GetDescFmt(migrationType);
            sb.AppendLine($"# {migrationType.Name}{descFmt}");
            sb.AppendLine(instance.Log);
            sb.AppendLine();
        }
        return StringBuilderCache.ReturnAndFree(sb);
    }

    public Exception? Error { get; set; }
    public List<Type> TypesCompleted { get; }
    public List<MigrationBase> MigrationsRun { get; }
    public bool Succeeded => Error == null && MigrationsRun.All(x => x.Error == null);
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
    }
    
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(10);
    
    public ILog Log { get; set; } = new ConsoleLogger(typeof(Migrator));

    Type? GetNextMigrationToRun(IDbConnection db, List<Type> migrationTypes)
    {
        var completedMigrations = new List<Type>();

        Type? nextRun = null;
        var q = db.From<Migration>()
            .OrderByDescending(x => x.Name).Limit(1);
        var lastRun = db.Single(q);
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

    public MigrationResult Run() => Run(throwIfError:true);
    public MigrationResult Run(bool throwIfError)
    {
        using var db = DbFactory.Open();
        Init(db);
        var allLogs = new StringBuilder();

        var remainingMigrations = MigrationTypes.ToList();

        LogMigrationsFound(remainingMigrations);

        var startAt = DateTime.UtcNow;
        var migrationsRun = new List<MigrationBase>();

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
                return new MigrationResult(migrationsRun) { Error = e };
            }
            
            var migrationStartAt = DateTime.UtcNow;

            var descFmt = GetDescFmt(nextRun);
            Log.Info($"Running {nextRun.Name}{descFmt}...");
            
            var migration = new Migration
            {
                Name = nextRun.Name,
                Description = GetDesc(nextRun),
                CreatedDate = DateTime.UtcNow,
                ConnectionString = ((OrmLiteConnectionFactory)DbFactory).ConnectionString,
                NamedConnection = nextRun.FirstAttribute<NamedConnectionAttribute>()?.Name,
            };
            var id = db.Insert(migration, selectIdentity:true);

            var instance = Run(DbFactory, nextRun, x => x.Up());
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

                if (throwIfError)
                    throw instance.Error;
                return new MigrationResult(migrationsRun);
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
        return new MigrationResult(migrationsRun);
    }

    private void LogMigrationsFound(List<Type> remainingMigrations)
    {
        var sb = StringBuilderCache.Allocate()
            .AppendLine("Migrations Found:");
        remainingMigrations.Each(x => sb.AppendLine((string?)$" - {x.Name}"));
        Log.Info(StringBuilderCache.ReturnAndFree(sb));
    }

    public static List<Type> GetAllMigrationTypes(Assembly[] migrationAssemblies)
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
    
    public MigrationResult Revert(string? migrationName) => Revert(migrationName, throwIfError:true);
    public MigrationResult Revert(string? migrationName, bool throwIfError)
    {
        using var db = DbFactory.Open();
        Init(db);
        
        var allMigrationTypes = MigrationTypes.ToList();
        allMigrationTypes.Reverse();

        LogMigrationsFound(allMigrationTypes);

        var startAt = DateTime.UtcNow;
        var migrationsRun = new List<MigrationBase>();

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
                    return new MigrationResult(migrationsRun) { Error = e };
                }
            
                var migrationStartAt = DateTime.UtcNow;

                var descFmt = GetDescFmt(nextRun);
                Log.Info($"Reverting {nextRun.Name}{descFmt}...");

                var instance = Run(DbFactory, nextRun, (MigrationBase x) => x.Down());
                migrationsRun.Add(instance);
                Log.Info(instance.Log);

                if (instance.Error == null)
                {
                    Log.Info($"Completed revert of {nextRun.Name}{descFmt} in {(DateTime.UtcNow - migrationStartAt).TotalSeconds:N3}s" +
                             Environment.NewLine);

                    // Remove completed migration revert from DB
                    db.Delete<Migration>(x => x.Name == nextRun.Name);
                }
                else
                {
                    Log.Error(instance.Error.Message, instance.Error);
                    if (throwIfError)
                        throw instance.Error;
                    
                    return new MigrationResult(migrationsRun);
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
        return new MigrationResult(migrationsRun);
    }

    public static MigrationResult Down(IDbConnectionFactory dbFactory, Type migrationType) => Down(dbFactory, new[] { migrationType });
    public static MigrationResult Down(IDbConnectionFactory dbFactory, Type[] migrationTypes) =>
        RunAll(dbFactory, migrationTypes, x => x.Down());
    public static MigrationResult Up(IDbConnectionFactory dbFactory, Type migrationType) => Up(dbFactory, new[] { migrationType });
    public static MigrationResult Up(IDbConnectionFactory dbFactory, Type[] migrationTypes) =>
        RunAll(dbFactory, migrationTypes, x => x.Up());

    public static MigrationBase Run(IDbConnectionFactory dbFactory, Type nextRun, Action<MigrationBase> migrateAction)
    {
        var sb = StringBuilderCache.Allocate();
        var holdFilter = OrmLiteConfig.BeforeExecFilter;
        OrmLiteConfig.BeforeExecFilter = dbCmd => sb.AppendLine(dbCmd.GetDebugString());

        var namedConnection = nextRun.FirstAttribute<NamedConnectionAttribute>()?.Name;

        IDbConnection? useDb = null;
        IDbTransaction? trans = null;
        var instance = nextRun.CreateInstance<MigrationBase>();

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
            instance.Log = sb.ToString();

            instance.BeforeCommit();
            trans.Commit();
            trans.Dispose();
            trans = null;
        }
        catch (Exception e)
        {
            instance.CompletedDate = DateTime.UtcNow;
            instance.Error = e;
            instance.Log = sb.ToString();
            instance.BeforeRollback();
            trans?.Rollback();
            trans?.Dispose();
        }
        finally
        {
            instance.Db = null;
            instance.Transaction = null;
            OrmLiteConfig.BeforeExecFilter = holdFilter;
            useDb?.Dispose();
            StringBuilderCache.Free(sb);
        }
        return instance;        
    }

    internal static string GetDescFmt(Type nextRun)
    {
        var desc = GetDesc(nextRun);
        return desc != null ? " '" + desc + "'" : "";
    }

    private static string? GetDesc(Type nextRun)
    {
        var desc = nextRun.GetDescription() ?? nextRun.FirstAttribute<NotesAttribute>()?.Notes;
        return desc;
    }

    public static MigrationResult RunAll(IDbConnectionFactory dbFactory, IEnumerable<Type> migrationTypes, Action<MigrationBase> migrateAction)
    {
        var migrationsRun = new List<MigrationBase>();
        foreach (var nextRun in migrationTypes)
        {
            var instance = Run(dbFactory, nextRun, migrateAction);
            migrationsRun.Add(instance);
            if (instance.Error != null)
                break;
        }
        return new MigrationResult(migrationsRun);
    }
    
}