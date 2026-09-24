using Microsoft.Extensions.DependencyInjection;
using System.Data;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;

namespace ServiceStack.Jobs;

public class BackgroundsJobFeature : IPlugin, Model.IHasStringId, IConfigureServices, IRequiresSchema, IPreInitPlugin, IBackgroundJobsOptions
{
    public string Id => Plugins.BackgroundJobs;
    /// <summary>
    /// Limit API access to users in role
    /// </summary>
    public string AccessRole { get; set; } = RoleNames.Admin;

    public string DbDir { get; set; } = "App_Data/jobs";
    public string DbFile { get; set; } = "jobs.db";
    public Func<DateTime, string> DbMonthFile { get; set; } = DefaultDbMonthFile;
    public Func<IDbConnectionFactory, IDbConnection> ResolveAppDb { get; set; }
    public Func<IDbConnectionFactory, DateTime, IDbConnection> ResolveMonthDb { get; set; }
    public Action<SqliteOrmLiteDialectProviderBase>? ConfigureDialectProvider { get; set; }
    public SqliteOrmLiteDialectProviderBase DialectProvider { get; set; }
    public Action<IDbConnection>? ConfigureDb { get; set; }
    public Action<IDbConnection>? ConfigureMonthDb { get; set; }
    public bool AutoInitSchema { get; set; } = true;
    public bool EnableAdmin { get; set; } = true;
    public bool EnableWriterLock { get; set; } = true;
    public IDbConnectionFactory DbFactory { get; set; } = null!;
    public IAppHostNetCore AppHost { get; set; } = null!;
    public CommandsFeature CommandsFeature { get; set; } = null!;
    public IBackgroundJobs Jobs { get; set; } = null!;
    public AutoQueryFeature? AutoQueryFeature { get; set; }
    
    public IAutoQueryDb? AutoQuery { get; set; }
    public int DefaultRetryLimit { get; set; } = 2;
    public RetryBackoff DefaultRetryBackoff { get; set; } = RetryBackoff.ExponentialJitter;
    public int DefaultRetryDelayMs { get; set; } = 5_000;
    public int DefaultMaxRetryDelayMs { get; set; } = 300_000;
    public int MaxJobLogChars { get; set; } = 100_000;
    /// <summary>
    /// Max number of Jobs a queue executes concurrently. Used for any queue without an explicit
    /// entry in QueueConcurrency, including the default queue.
    /// </summary>
    public int MaxConcurrentJobs { get; set; } = Math.Max(1, Environment.ProcessorCount);
    /// <summary>Max concurrent Jobs per named queue, e.g. `{ ["emails"] = 2 }`</summary>
    public Dictionary<string, int> QueueConcurrency { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>How often Scheduled Tasks are reloaded from the database</summary>
    public int ReloadScheduledTasksSecs { get; set; } = 60;
    /// <summary>How often Job Queue pause/concurrency controls are reloaded from the database</summary>
    public int ReloadJobQueuesSecs { get; set; } = 10;
    /// <summary>How long to wait for running Jobs to finish when the App shuts down</summary>
    public int ShutdownTimeoutSecs { get; set; } = 30;
    /// <summary>How often this node records that it's alive in the JobNode table</summary>
    public int NodeHeartbeatSecs { get; set; } = 15;
    /// <summary>A node with no heartbeat within this window is reported as unreachable</summary>
    public int NodeTimeoutSecs { get; set; } = 60;
    /// <summary>
    /// Max size of a Job's serialized Request. Queueing a larger payload is rejected: the database
    /// is a work queue, not a blob store, so pass a reference instead.
    /// </summary>
    public int MaxRequestBodyChars { get; set; } = 1_000_000;
    /// <summary>
    /// Max size of a Job's serialized Response to persist. A larger result is dropped rather than
    /// stored, recorded in the Job's Meta, so one big result can't bloat the archive.
    /// </summary>
    public int MaxResponseBodyChars { get; set; } = 1_000_000;
    /// <summary>
    /// How long monthly CompletedJob/FailedJob archives are kept, null to keep them indefinitely
    /// </summary>
    public TimeSpan? ArchiveRetention { get; set; }
    /// <summary>
    /// Delivers a completed Job's result to its ReplyTo address. The default posts to an
    /// http:// or https:// URL, otherwise publishes to ReplyTo as an MQ Queue Name.
    /// </summary>
    public Func<JobReplyToContext, Task> OnJobReplyTo { get; set; } = JobReplyTo.SendAsync;
    /// <summary>
    /// Whether a Job's ReplyTo may be used, checked when it's queued and again before its result is
    /// sent. Null allows any ReplyTo. Restrict it when a ReplyTo can come from an end user, e.g.
    /// `ValidateReplyTo = JobReplyTo.AllowUrlPrefixes(["https://hooks.example.org/"])`
    /// </summary>
    public Func<BackgroundJobBase, bool>? ValidateReplyTo { get; set; }
    /// <summary>
    /// How long JobSummary rows are retained for, null to keep them indefinitely. Note a Job's
    /// RefId can be reused once its JobSummary has been deleted.
    /// </summary>
    public TimeSpan? JobSummaryRetention { get; set; }
    /// <summary>Identifies this App Server in the JobNode registry</summary>
    public string ServerId { get; set; } = $"{Environment.MachineName}:{Environment.ProcessId}";
    public int DefaultTimeoutSecs { get; set; } = 10 * 60; // 10 mins
    public TimeSpan DefaultTimeout
    {
        get => TimeSpan.FromSeconds(DefaultTimeoutSecs);
        set => DefaultTimeoutSecs = (int)value.TotalSeconds;
    }
    public Func<BackgroundJob,Exception,bool> ShouldRetry { get; set; } = (_,ex) => ex is not OperationCanceledException;

    public BackgroundsJobFeature()
    {
        ResolveAppDb = DefaultResolveAppDb;
        ResolveMonthDb = DefaultResolveMonthDb;
        ConfigureDb = ConfigureMonthDb = DefaultConfigureDb;
    }

    public void Configure(IServiceCollection services)
    {
        services.AddSingleton(this);
        services.AddSingleton<IBackgroundJobs,BackgroundJobs>();
        services.AddHostedService<JobsShutdownHostedService>();

        if (EnableAdmin)
        {
            services.RegisterService<AdminJobServices>();
            AutoQueryFeature ??= new() { MaxLimit = 1000 };
            AutoQueryFeature.RegisterAutoQueryDbIfNotExists();
        }
    }
    
    public void Register(IAppHost appHost)
    {
        if (appHost == null)
            return;

        DialectProvider = SqliteConfiguration.Configure(SqliteDialect.Create());

        CommandsFeature ??= appHost.GetPlugin<CommandsFeature>()
                            ?? throw new Exception($"{nameof(CommandsFeature)} is required to use {nameof(BackgroundsJobFeature)}");
        Jobs ??= appHost.TryResolve<IBackgroundJobs>() 
            ?? throw new Exception($"{nameof(IBackgroundJobs)} is not registered");
        DbFactory ??= appHost.TryResolve<IDbConnectionFactory>() 
            ?? new OrmLiteConnectionFactory("Data Source=:memory:", DialectProvider);

        var dateConverter = DialectProvider.GetDateTimeConverter();
        if (dateConverter.DateStyle == DateTimeKind.Unspecified)
            dateConverter.DateStyle = DateTimeKind.Utc;

        DialectProvider.EnableWriterLock = EnableWriterLock;
        ConfigureDialectProvider?.Invoke(DialectProvider);

        AppHost ??= appHost as IAppHostNetCore;
        var fullDirPath = GetDbDir();

        DbFactory.RegisterConnection(DbFile, fullDirPath.AssertDir().CombineWith(DbFile), DialectProvider);
        
        // If DbFile has changed, replace the namedConnection lock with Locks.JobsDb
        if (DbFile != Workers.JobsDb)
            Locks.NamedConnections[DbFile] = Locks.JobsDb;

        if (AutoInitSchema)
        {
            InitSchema();
            using var monthDb = OpenMonthDb(DateTime.UtcNow);
            InitMonthDbSchema(monthDb);
        }
    }

    public void BeforePluginsLoaded(IAppHost appHost)
    {
        if (EnableAdmin)
        {
            appHost.ConfigurePlugin<UiFeature>(feature =>
            {
                feature.AddAdminLink(AdminUiFeature.BackgroundJobs, new LinkInfo {
                    Id = "backgroundjobs",
                    Label = "Background Jobs",
                    Icon = Svg.ImageSvg(SvgIcons.Tasks),
                    Show = $"role:{AccessRole}",
                });
            });
        }
    }
    
    public static string DefaultDbMonthFile(DateTime createdDate) => $"jobs_{createdDate.Year}-{createdDate.Month:00}.db";

    public void DefaultConfigureDb(IDbConnection db) => db.WithTag(GetType().Name);

    public IDbConnection DefaultResolveAppDb(IDbConnectionFactory dbFactory) =>
        ((IDbConnectionFactoryExtended)dbFactory).OpenDbConnection(DbFile, ConfigureDb);

    public IDbConnection DefaultResolveMonthDb(IDbConnectionFactory dbFactory, DateTime createdDate)
    {
        var factory = (IDbConnectionFactoryExtended)dbFactory;
        var monthDb = DbMonthFile(createdDate);
        lock (this)
        {
            if (!OrmLiteConnectionFactory.NamedConnections.ContainsKey(monthDb))
            {
                var dataSource =  GetDbDir(monthDb);
                dbFactory.RegisterConnection(monthDb, $"DataSource={dataSource};Cache=Shared", DialectProvider);
                var db = factory.OpenDbConnection(monthDb, ConfigureMonthDb);
                InitMonthDbSchema(db);
                return db;
            }
        }
        return factory.OpenDbConnection(monthDb, ConfigureMonthDb);
    }

    public IDbConnection OpenDb() => ResolveAppDb(DbFactory);
    public IDbConnection OpenMonthDb(DateTime createdDate) => ResolveMonthDb(DbFactory, createdDate);

    public void InitSchema()
    {
        using var db = OpenDb();
        InitSchema(db);
    }

    public void InitSchema(IDbConnection db)
    {
        BackgroundJobSchema.UpgradeMainDb(db);
    }
    
    public void InitMonthDbSchema(IDbConnection db)
    {
        BackgroundJobSchema.UpgradeArchiveDb(db);
    }

    public IServiceProvider Services => AppHost!.App.ApplicationServices;
    
    private string GetDbDir(string monthDb = "")
    {
        return Path.IsPathRooted(DbDir) 
            ? DbDir.CombineWith(monthDb)
            : AppHost.HostingEnvironment.ContentRootPath.CombineWith(DbDir, monthDb);
    }

    /// <summary>
    /// Deletes a monthly Jobs database. Each month is its own SQLite file, so dropping an expired
    /// archive is deleting that file.
    /// </summary>
    public void DeleteMonthDb(DateTime createdDate)
    {
        var monthDb = DbMonthFile(createdDate);
        var dbPath = GetDbDir(monthDb);
        lock (this)
        {
            OrmLiteConnectionFactory.NamedConnections.Remove(monthDb);
        }
        foreach (var suffix in new[] { "", "-journal", "-wal", "-shm" })
        {
            if (File.Exists(dbPath + suffix))
                File.Delete(dbPath + suffix);
        }
    }

    public List<DateTime> GetTableMonths(IDbConnection db)
    {
        var dir = Path.IsPathRooted(DbDir) ?
            new DirectoryInfo(DbDir)
            : new DirectoryInfo(HostContext.AppHost.GetHostingEnvironment().ContentRootPath.CombineWith(DbDir));
        
        if (!dir.Exists)
            return new List<DateTime>();
        
        var monthDbs = dir.GetFiles()
            .Where(x => x.Name.Contains('_'));

        return monthDbs.Select(x => 
                DateTime.TryParse(x.Name.RightPart('_').LeftPart('.') + "-01", out var date) ? date : (DateTime?)null)
            .Where(x => x != null)
            .Select(x => x!.Value)
            .OrderDescending()
            .ToList();
    }
}
