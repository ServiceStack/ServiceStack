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

public class DatabaseJobFeature : IPlugin, Model.IHasStringId, IConfigureServices, IRequiresSchema, IPreInitPlugin, IBackgroundJobsOptions
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
    public int LeaseDurationSecs { get; set; } = 60;
    /// <summary>Max Jobs read per tick when looking for Jobs to claim</summary>
    public int ClaimBatchSize { get; set; } = 100;
    /// <summary>
    /// Max Jobs per queue this node claims ahead of its Workers, on top of the ones it's running.
    /// Keeps one node from hoarding a backlog that other nodes could be running, while leaving
    /// enough queued locally that Workers aren't idle between ticks.
    /// </summary>
    public int MaxPrefetchJobs { get; set; } = 100;
    /// <summary>
    /// Only process Jobs on these queues, e.g. to run GPU work on the nodes that have one.
    /// Null (default) processes every queue.
    /// </summary>
    public List<string>? Queues { get; set; }
    public string ServerId { get; set; } = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
    public int DefaultTimeoutSecs { get; set; } = 10 * 60; // 10 mins
    public TimeSpan DefaultTimeout
    {
        get => TimeSpan.FromSeconds(DefaultTimeoutSecs);
        set => DefaultTimeoutSecs = (int)value.TotalSeconds;
    }
    public Func<BackgroundJob,Exception,bool> ShouldRetry { get; set; } = (_,ex) => ex is not OperationCanceledException;

    DatabaseJobFeature Resolve(IServiceProvider services)
    {
        Init(services);
        return this;
    }
    
    public void Configure(IServiceCollection services)
    {
        services.AddSingleton(this);
        services.AddHostedService<JobsShutdownHostedService>();
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
