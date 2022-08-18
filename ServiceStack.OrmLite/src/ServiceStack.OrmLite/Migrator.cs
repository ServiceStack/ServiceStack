#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
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
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorStackTrace { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}

public abstract class MigrationBase
{
    public IDbConnectionFactory DbFactory { get; set; }
    public IDbConnection Db { get; set; }
    public IDbTransaction Transaction { get; set; }

    public virtual void AfterOpen() {}
    public virtual void BeforeCommit() {}
    public virtual void BeforeRollback() {}
    public virtual void Up(){}
    public virtual void Down(){}
}

public class MigrationResult
{
    public MigrationResult(Exception? error) => Error = error;
    public MigrationResult(List<Type> tasksRun) => TasksRun = tasksRun;

    public MigrationResult(List<Type> tasksRun, Exception? error)
    {
        TasksRun = tasksRun;
        Error = error;
    }

    public Exception? Error { get; }
    public List<Type> TasksRun { get; }
    public bool Succeeded => Error == null;
}

public class Migrator
{
    public IDbConnectionFactory DbFactory { get; }
    public Assembly[] MigrationAssemblies { get; }
    public Migrator(IDbConnectionFactory dbFactory, params Assembly[] migrationAssemblies)
    {
        DbFactory = dbFactory;
        MigrationAssemblies = migrationAssemblies;
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
                    throw new Exception($"Migration '{lastRun.Name}' is still in progress, timeout in {(Timeout - elapsedTime).TotalSeconds:N3}s.");
                
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
        
        var remainingMigrations = MigrationAssemblies
            .SelectMany(x => x.GetTypes().Where(x => x.IsInstanceOf(typeof(MigrationBase)) && !x.IsAbstract))
            .OrderBy(x => x.Name)
            .ToList();

        var sb = StringBuilderCache.Allocate()
            .AppendLine("Migrations Found:");
        remainingMigrations.Each(x => sb.AppendLine($" - {x.Name}"));
        Log.Info(StringBuilderCache.ReturnAndFree(sb));

        var startAt = DateTime.UtcNow;
        var migrationsRun = new List<Type>();

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
                return new MigrationResult(migrationsRun, e);
            }
            
            var migrationStartAt = DateTime.UtcNow;

            var namedConnection = nextRun.FirstAttribute<NamedConnectionAttribute>()?.Name;
            var desc = nextRun.GetDescription() ?? nextRun.FirstAttribute<NotesAttribute>()?.Notes;
            var descFmt = desc != null ? " '" + desc + "'" : "";
            
            Log.Info($"Running {nextRun.Name}{descFmt}...");

            var migration = new Migration
            {
                Name = nextRun.Name,
                Description = desc,
                CreatedDate = DateTime.UtcNow,
                ConnectionString = ((OrmLiteConnectionFactory)DbFactory).ConnectionString,
                NamedConnection = namedConnection,
            };
            var id = db.Insert(migration, selectIdentity:true);

            IDbConnection? useDb = null;
            IDbTransaction? trans = null;
            MigrationBase? instance = null;
            try
            {
                useDb = namedConnection == null
                    ? DbFactory.OpenDbConnection()
                    : DbFactory.OpenDbConnection(namedConnection);

                instance = nextRun.CreateInstance<MigrationBase>();
                instance.DbFactory = DbFactory;
                instance.Db = useDb;
                trans = useDb.OpenTransaction();
                instance.AfterOpen();

                instance.Transaction = trans;

                instance.Up();
                
                instance.BeforeCommit();
                trans.Commit();
                trans.Dispose();
                trans = null;

                // Run Migration
                Log.Info($"Completed {nextRun.Name}{descFmt} in {(DateTime.UtcNow - migrationStartAt).TotalSeconds:N3}s" +
                         Environment.NewLine);

                // Record completed migration run in DB
                db.UpdateOnly(() => new Migration { CompletedDate = DateTime.UtcNow }, where: x => x.Id == id);
                remainingMigrations.Remove(nextRun);
                migrationsRun.Add(nextRun);
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);

                instance?.BeforeRollback();
                trans?.Rollback();
                trans?.Dispose();

                // Save Error in DB
                db.UpdateOnly(() => new Migration
                {
                    ErrorCode = e.GetType().Name,
                    ErrorMessage = e.Message,
                    ErrorStackTrace = e.StackTrace,
                }, where: x => x.Id == id);

                if (throwIfError)
                    throw;
                return new MigrationResult(migrationsRun, e);
            }
            finally
            {
                useDb?.Dispose();
            }
        }

        if (migrationsRun.Count == 0)
        {
            Log.Info("No migrations to run.");
        }
        else
        {
            var migration = migrationsRun.Count > 1 ? "migrations" : "migration";
            Log.Info($"{Environment.NewLine}Ran {migrationsRun.Count} {migration} in {(DateTime.UtcNow - startAt).TotalSeconds:N3}s");
        }
        return new MigrationResult(migrationsRun);
    }

    public static void Init(IDbConnection db)
    {
        db.CreateTableIfNotExists<Migration>();
    }

    public static void Reset(IDbConnection db)
    {
        db.DropAndCreateTable<Migration>();
    }

    public static void Clean(IDbConnection db)
    {
        if (db.TableExists<Migration>())
            db.DeleteAll<Migration>();
        else
            Init(db);
    }

    public void Revert(string migrationName) => Revert(migrationName, throwIfError:true);
    public void Revert(string migrationName, bool throwIfError)
    {
        throw new NotImplementedException();
    }
}