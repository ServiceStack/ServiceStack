#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.Jobs;
using ServiceStack.OrmLite;

namespace ServiceStack;

public class DatabaseJobFeature : IPlugin, Model.IHasStringId, IConfigureServices, IRequiresSchema, IPreInitPlugin
{
    public string Id => Plugins.BackgroundJobs;
    /// <summary>
    /// Limit API access to users in role
    /// </summary>
    public string AccessRole { get; set; } = RoleNames.Admin;
    public IDbConnectionFactory DbFactory { get; set; } = null!;
    public string? NamedConnection { get; set; }
    public DbJobsProvider DbProvider { get; set; } = null!;
    public IOrmLiteDialectProvider Dialect => DbProvider.Dialect;
    public Action<IOrmLiteDialectProvider>? ConfigureDialect { get; set; }
    public bool AutoInitSchema { get; set; } = true;
    public bool EnableAdmin { get; set; } = true;
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

    DatabaseJobFeature Resolve(IServiceProvider services)
    {
        Init(services);
        return this;
    }
    
    public void Configure(IServiceCollection services)
    {
        services.AddSingleton(this);
        services.AddSingleton<IBackgroundJobs>(c => new DbJobs(
            c.GetRequiredService<ILogger<DbJobs>>(),
            Resolve(c),
            c,
            c.GetRequiredService<IServiceScopeFactory>()
        ));

        if (EnableAdmin)
        {
            services.RegisterService<DbJobsAdminServices>();
            AutoQueryFeature ??= new() { MaxLimit = 1000 };
            AutoQueryFeature.RegisterAutoQueryDbIfNotExists();
        }
    }

    protected void Init(IServiceProvider services)
    {
        DbFactory ??= services.GetService<IDbConnectionFactory>() 
            ?? throw new Exception($"{nameof(IDbConnectionFactory)} is not registered");
        DbProvider ??= DbJobsProvider.Create(DbFactory, NamedConnection);
        var dateConverter = Dialect.GetDateTimeConverter();
        if (dateConverter.DateStyle == DateTimeKind.Unspecified)
            dateConverter.DateStyle = DateTimeKind.Utc;
    }
    
    public void Register(IAppHost appHost)
    {
        var services = appHost.GetApplicationServices();
        Init(services);
        
        CommandsFeature ??= appHost.GetPlugin<CommandsFeature>()
            ?? throw new Exception($"{nameof(CommandsFeature)} is required to use {nameof(DatabaseJobFeature)}");
        Jobs ??= services.GetService<IBackgroundJobs>() 
            ?? throw new Exception($"{nameof(IBackgroundJobs)} is not registered");

        ConfigureDialect?.Invoke(DbProvider.Dialect);

        AppHost ??= (IAppHostNetCore)appHost;

        if (AutoInitSchema)
        {
            InitSchema();
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
    
    public IDbConnection OpenDb() => DbProvider.OpenDb();
    public IDbConnection OpenMonthDb(DateTime createdDate) => DbProvider.OpenMonthDb(createdDate);
    
    public List<DateTime> GetTableMonths(IDbConnection db) => DbProvider.GetTableMonths(db);
    public string DateFormat(string quotedColumn, string format) => DbProvider.SqlDateFormat(quotedColumn, format);
    public void InitSchema() => DbProvider.InitSchema();

    public IServiceProvider Services => AppHost!.App.ApplicationServices;
}
#endif