using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Admin;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using ServiceStack.HtmlModules;
using ServiceStack.Model;

namespace ServiceStack;

[Flags]
public enum AdminUiFeature
{
    None           = 0,
    Users          = 1 << 0,
    Roles          = 1 << 1,
    Validation     = 1 << 2,
    Logging        = 1 << 3,
    Analytics      = 1 << 4,
    Profiling      = 1 << 5,
    Redis          = 1 << 6,
    Database       = 1 << 7,
    Commands       = 1 << 8,
    ApiKeys        = 1 << 9,
    BackgroundJobs = 1 << 10,
    All = Users | Roles | Validation | Logging | Analytics | Profiling | Redis | Database | Commands | ApiKeys | BackgroundJobs,
}

public class AnalyticsConfig
{
    public int BatchSize { get; set; } = 1000;
    public int[] DurationRanges { get; set; } = [10, 50, 100, 500, 1000, 2000, 5000, 30000];
    public int SummaryLimit { get; set; } = 100;
    public int DetailLimit { get; set; } = 10;
}

public class AnalyticsInfo
{
    public List<string> Months { get; set; } = [];
    public Dictionary<string,string> Tabs { get; set; } = [];
}

public interface IRequireAnalytics
{
    long GetTotal(DateTime month);
    List<RequestLogEntry> QueryLogs(RequestLogs request);
    void ClearAnalyticsCaches(DateTime month);
    AnalyticsInfo GetAnalyticInfo(AnalyticsConfig config);
    AnalyticsReports GetAnalyticsReports(AnalyticsConfig config, DateTime month);
    AnalyticsReports GetApiAnalytics(AnalyticsConfig config, DateTime month, string op);
    AnalyticsReports GetUserAnalytics(AnalyticsConfig config, DateTime month, string userId);
    AnalyticsReports GetApiKeyAnalytics(AnalyticsConfig config, DateTime month, string apiKey);
    AnalyticsReports GetIpAnalytics(AnalyticsConfig config, DateTime month, string ip);
}

public class UiFeature : IPlugin, IConfigureServices, IPreInitPlugin, IPostInitPlugin, IHasStringId
{
    public string Id => Plugins.Ui;

    public UiInfo Info { get; set; } = new()
    {
        HideTags = [TagNames.Auth, TagNames.Admin],
        AlwaysHideTags = [TagNames.Admin],
        BrandIcon = Svg.ImageUri(Svg.GetDataUri(Svg.Logos.ServiceStack, "#000000")),
        UserIcon = Svg.ImageUri(JwtClaimTypes.DefaultProfileUrl),
        Theme = new ThemeInfo
        {
            Form = "shadow overflow-hidden sm:rounded-md bg-white",
            ModelIcon = Svg.ImageSvg(Svg.Create(Svg.Body.Table)),
        },
        DefaultFormats = new ApiFormat
        {
            // Defaults to browsers navigator.languages
            // Locale = Thread.CurrentThread.CurrentCulture.Name,
            AssumeUtc = true,
            Date = new Intl(IntlFormat.DateTime) {
                Date = DateStyle.Medium,
            }.ToFormat(),
        },
        Locode = new()
        {
            Css = new ApiCss
            {
                Form = "max-w-screen-2xl",
                Fieldset = "grid grid-cols-12 gap-6",
                Field = "col-span-12 lg:col-span-6 xl:col-span-4",
            },
            Tags = new AppTags
            {
                Default = "Tables",
                Other = "other",
            },
            MaxFieldLength = 150,
            MaxNestedFields = 2,
            MaxNestedFieldLength = 30,
        },
        Explorer = new()
        {
            Css = new ApiCss
            {
                Form = "max-w-screen-md",
                Fieldset = "grid grid-cols-12 gap-6", 
                Field = "col-span-12 sm:col-span-6",
            },
            Tags = new AppTags
            {
                Default = "APIs",
                Other = "other",
            },
            JsConfig = "eccn,edv",
        },
        Admin = new()
        {
            Css = new ApiCss
            {
                Form = "max-w-screen-lg",
                Fieldset = "grid grid-cols-12 gap-6", 
                Field = "col-span-12",
            },
        },
        AdminLinks = new(),
        AdminLinksOrder = [
            "",
            "analytics",
            "users",
            "roles",
            "apikeys",
            "logging",
            "profiling",
            "commands",
            "backgroundjobs",
            "validation",
            "database",
            "redis",
        ],
    };

    public List<HtmlModule> HtmlModules { get; } = new();
    
    public HtmlModule AdminHtmlModule { get; set; } = new("/modules/admin-ui", "/admin-ui") {
        DynamicPageQueryStrings = { nameof(MetadataApp.IncludeTypes) }
    };
    public AdminUiFeature AdminUi { get; set; } = AdminUiFeature.All;

    /// <summary>
    /// Links to make available to users in different roles (e.g. in built-in UIs) 
    /// </summary>
    public Dictionary<string, List<LinkInfo>> RoleLinks { get; set; } = new();

    public LinkInfo DashboardLink { get; set; } = new()
    {
        Id = "",
        Label = "Dashboard",
        Icon = Svg.ImageSvg(Svg.Create(Svg.Body.Home)),
    };

    public List<IHtmlModulesHandler> Handlers { get; set; } = new()
    {
        new SharedFolder("shared", "/modules/shared", ".html"),
        new SharedFolder("shared/js", "/modules/shared/js", ".js"),
        new SharedFolder("plugins", "/modules/shared/plugins", ".js"),
        new SharedFolder("components", "/js/components", ".mjs")
        {
            Header = FilesTransformer.ModuleHeader,
            Footer = FilesTransformer.ModuleFooter,
        },
    };

    public HtmlModulesFeature Module { get; } = new HtmlModulesFeature {
            IgnoreIfError = true,
        }
        .Configure((appHost,module) => 
            module.VirtualFiles = appHost.VirtualFileSources);
    
    public Action<IAppHost> OnConfigure { get; set; }

    /// <summary>
    /// Only Attributes used in built-in UIs are returned in /metadata/app.json  
    /// </summary>
    public List<string> PreserveAttributesNamed { get; set; } =
    [
        nameof(ComputedAttribute)
    ];

    // Defaults to browsers navigator.languages
    //Locale = Thread.CurrentThread.CurrentCulture.Name,

    public void AddAdminLink(AdminUiFeature feature, LinkInfo link)
    {
        if (!AdminUi.HasFlag(feature)) 
            return;
        
        if (!RoleLinks.TryGetValue(RoleNames.Admin, out var roleLinks))
            roleLinks = RoleLinks[RoleNames.Admin] = new();
        roleLinks.Add(link.ToAdminRoleLink());

        Info.AdminLinks.Add(link);
    }

    public void Configure(IServiceCollection services)
    {
        services.RegisterService(typeof(AdminDashboardService));
    }

    public void BeforePluginsLoaded(IAppHost appHost)
    {
        if (AdminHtmlModule != null && AdminUi != AdminUiFeature.None)
        {
            HtmlModules.Add(AdminHtmlModule);
            AdminHtmlModule.OnConfigure.Add((_, module) => {
                module.LineTransformers = FilesTransformer.HtmlModuleLineTransformers.ToList();
            });
            
            AddAdminLink(AdminUiFeature.None, DashboardLink);
        }
    }

    public void Register(IAppHost appHost)
    {
    }

    public void AfterPluginsLoaded(IAppHost appHost)
    {
        if (HtmlModules.Count > 0)
        {
            Info.Modules = HtmlModules.Map(x => x.BasePath);
            OnConfigure?.Invoke(appHost);
            Module.Modules.AddRange(HtmlModules);
            Module.Handlers.AddRange(Handlers);
            Module.Register(appHost);
        }
    }
}

public static class UiFeatureUtils
{
    public static LinkInfo ToAdminRoleLink(this LinkInfo link) => new() {
        Id = link.Id,
        Label = link.Label,
        Icon = link.Icon,
        Href = "../admin-ui" + (string.IsNullOrEmpty(link.Id) ? "" : "/" + link.Id),
        Show = link.Show,
    };
}
