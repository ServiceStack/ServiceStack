using Microsoft.Extensions.DependencyInjection;
using System.Data;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;

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
    public bool AutoInitSchema { get; set; } = true;
    public bool EnableAdmin { get; set; } = false;
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
    }

    public void Configure(IServiceCollection services)
    {
        services.AddSingleton(this);
        services.AddSingleton<IBackgroundJobs,BackgroundJobs>();

        if (EnableAdmin)
        {
            // Background Jobs Admin UI requires AutoQuery functionality
            ServiceStackHost.GlobalAfterConfigureServices.Add(c =>
            {
                if (!c.Exists<IAutoQueryDb>())
                {
                    AutoQueryFeature ??= new() { MaxLimit = 1000 };
                    c.AddSingleton<IAutoQueryDb>(AutoQueryFeature.CreateAutoQueryDb());
                }
            });
        }
    }

    public void Register(IAppHost appHost)
    {
        CommandsFeature ??= appHost.GetPlugin<CommandsFeature>()
            ?? throw new Exception($"{nameof(CommandsFeature)} is required to use {nameof(BackgroundsJobFeature)}");
        Jobs ??= appHost.TryResolve<IBackgroundJobs>() 
            ?? throw new Exception($"{nameof(IBackgroundJobs)} is not registered");
        DbFactory ??= appHost.TryResolve<IDbConnectionFactory>() 
            ?? new OrmLiteConnectionFactory("Data Source=:memory:", SqliteDialect.Provider);

        var dateConverter = SqliteDialect.Provider.GetDateTimeConverter();
        if (dateConverter.DateStyle == DateTimeKind.Unspecified)
            dateConverter.DateStyle = DateTimeKind.Utc;

        AppHost ??= (IAppHostNetCore)appHost;
        var fullDirPath = AppHost.HostingEnvironment.ContentRootPath.CombineWith(DbDir).AssertDir();

        DbFactory.RegisterConnection(DbFile, fullDirPath.CombineWith(DbFile), SqliteDialect.Provider);

        if (AutoInitSchema)
        {
            InitSchema();
            InitMonthDbSchema(DateTime.UtcNow);
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

    public IDbConnection DefaultResolveAppDb(IDbConnectionFactory dbFactory) =>
        dbFactory.OpenDbConnection(DbFile);

    public IDbConnection DefaultResolveMonthDb(IDbConnectionFactory dbFactory, DateTime createdDate)
    {
        var monthDb = DbMonthFile(createdDate);
        if (!OrmLiteConnectionFactory.NamedConnections.ContainsKey(monthDb))
        {
            var dataSource = AppHost.HostingEnvironment.ContentRootPath.CombineWith(DbDir, monthDb);
            dbFactory.RegisterConnection(monthDb, $"DataSource={dataSource};Cache=Shared", SqliteDialect.Provider);
        }
        return dbFactory.OpenDbConnection(monthDb);
    }

    public IDbConnection OpenJobsDb() => ResolveAppDb(DbFactory);
    public IDbConnection OpenJobsMonthDb(DateTime createdDate) => ResolveMonthDb(DbFactory, createdDate);

    public void InitSchema()
    {
        using var db = OpenJobsDb();
        db.CreateTableIfNotExists<BackgroundJob>();
        db.CreateTableIfNotExists<JobSummary>();
        db.CreateTableIfNotExists<ScheduledTask>();
    }
    public void InitMonthDbSchema(DateTime createdDate)
    {
        using var db = OpenJobsMonthDb(createdDate);
        db.CreateTableIfNotExists<CompletedJob>();
        db.CreateTableIfNotExists<FailedJob>();
    }

    public IServiceProvider Services => AppHost!.App.ApplicationServices;
}
