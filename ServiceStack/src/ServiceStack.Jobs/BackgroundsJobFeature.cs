using Microsoft.Extensions.DependencyInjection;
using System.Data;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;

namespace ServiceStack.Jobs;

public class BackgroundsJobFeature : IPlugin, Model.IHasStringId, IConfigureServices, IRequiresSchema, IPreInitPlugin
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
    public int DefaultTimeoutSecs { get; set; } = 10 * 60; // 10 mins
    public TimeSpan DefaultTimeout
    {
        get => TimeSpan.FromSeconds(DefaultTimeoutSecs);
        set => DefaultTimeoutSecs = (int)value.TotalSeconds;
    }
    public Func<BackgroundJob,Exception,bool> ShouldRetry { get; set; } = (_,ex) => ex is not TaskCanceledException;

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

        if (EnableAdmin)
        {
            services.RegisterService<AdminJobServices>();
            AutoQueryFeature ??= new() { MaxLimit = 1000 };
            AutoQueryFeature.RegisterAutoQueryDbIfNotExists();
        }
    }
    
    public void Register(IAppHost appHost)
    {
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

        AppHost ??= (IAppHostNetCore)appHost;
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

    public void DefaultConfigureDb(IDbConnection db) => db.WithName(GetType().Name);

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
        db.CreateTableIfNotExists<BackgroundJob>();
        db.CreateTableIfNotExists<JobSummary>();
        db.CreateTableIfNotExists<ScheduledTask>();
    }
    
    public void InitMonthDbSchema(IDbConnection db)
    {
        db.CreateTableIfNotExists<CompletedJob>();
        db.CreateTableIfNotExists<FailedJob>();
    }

    public IServiceProvider Services => AppHost!.App.ApplicationServices;
    
    private string GetDbDir(string monthDb = "")
    {
        return Path.IsPathRooted(DbDir) 
            ? DbDir.CombineWith(monthDb)
            : AppHost.HostingEnvironment.ContentRootPath.CombineWith(DbDir, monthDb);
    }
}
